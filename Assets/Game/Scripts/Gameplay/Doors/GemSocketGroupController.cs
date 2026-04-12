using System.Collections;
using Game.Systems.Endings;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class GemSocketGroupController : MonoBehaviour
{
    [Header("Sockets")]
    public GemSocket2D redSocket;
    public GemSocket2D greenSocket;
    public GemSocket2D blueSocket;

    [Header("Completion")]
    [Tooltip("Optional GlobalState key set to true when all sockets are inserted.")]
    public string completedGlobalKey;

    [Header("Dialogue (Optional)")]
    public DialogueAsset onCompletedDialogue;

    [Header("Ending Sequence")]
    [Tooltip("When true, use the sequence below instead of only opening onCompletedDialogue.")]
    public bool playCompletionSequence = true;
    [Tooltip("Freeze player input during ending sequence.")]
    public bool lockInputDuringSequence = true;
    [Tooltip("Pause world simulation during ending sequence.")]
    public bool pauseWorldDuringSequence = true;

    [Header("Ending Sequence / Music + Trophy")]
    public AudioClip completionMusic;
    [Min(0f)] public float completionMusicCrossfade = 0.6f;
    public bool completionMusicLoop = true;
    [Tooltip("Optional trophy object/sprite to show when sequence starts.")]
    public GameObject trophyVisual;
    [Tooltip("Hide trophyVisual on Awake until the completion sequence starts.")]
    public bool hideTrophyUntilSequence = true;

    [Header("Ending Sequence / Dialogue Rhythm")]
    [Tooltip("Use fixed rhythm auto-play dialogue (ignores player input while playing).")]
    public bool useFixedRhythmDialogue = true;
    [Min(0f)] public float fixedRhythmSecondsPerLine = 1.2f;
    [Min(0f)] public float preDialogueDelay = 0.2f;
    [Min(0f)] public float postDialogueDelay = 0.3f;

    [Header("Ending Sequence / Ending Unlock")]
    [Tooltip("Use fixed ids like ending.up / ending.right / ending.down / ending.left.")]
    public string unlockEndingId;

    [Header("Ending Sequence / End Scene + Return")]
    public string endSceneName = "END";
    public string endSpawnId = "";
    [Min(0f)] public float endSceneFadeOut = 0.4f;
    [Min(0f)] public float endSceneFadeIn = 0.2f;
    [FormerlySerializedAs("quitAfterEndScene")]
    public bool returnToMainMenuAfterEndScene = true;
    [FormerlySerializedAs("quitDelaySeconds")]
    [Min(0f)] public float returnDelaySeconds = 2.0f;
    public string mainMenuSceneName = "MainMenu";
    public string mainMenuSpawnId = "";
    [Min(0f)] public float mainMenuFadeOut = 0.4f;
    [Min(0f)] public float mainMenuFadeIn = 0.2f;

    private bool completed;
    private bool sequenceStarted;

    private void Awake()
    {
        if (!string.IsNullOrEmpty(completedGlobalKey) && GameRoot.I != null)
            completed = GameRoot.I.Global.GetBool(completedGlobalKey);

        if (hideTrophyUntilSequence && trophyVisual != null)
            trophyVisual.SetActive(false);

        BindSocket(redSocket);
        BindSocket(greenSocket);
        BindSocket(blueSocket);

        if (!completed)
            TryComplete(openDialogue: false);
    }

    private void OnDestroy()
    {
        UnbindSocket(redSocket);
        UnbindSocket(greenSocket);
        UnbindSocket(blueSocket);
    }

    private void OnSocketInserted(GemSocket2D _)
    {
        TryComplete(openDialogue: true);
    }

    private void BindSocket(GemSocket2D socket)
    {
        if (socket == null) return;
        socket.OnInserted += OnSocketInserted;
    }

    private void UnbindSocket(GemSocket2D socket)
    {
        if (socket == null) return;
        socket.OnInserted -= OnSocketInserted;
    }

    private void TryComplete(bool openDialogue)
    {
        if (completed) return;
        if (redSocket == null || greenSocket == null || blueSocket == null) return;
        if (!redSocket.IsInserted || !greenSocket.IsInserted || !blueSocket.IsInserted) return;

        completed = true;
        if (!string.IsNullOrEmpty(completedGlobalKey))
            GameRoot.I?.Global?.SetBool(completedGlobalKey, true);

        if (!openDialogue) return;

        if (playCompletionSequence)
        {
            BeginCompletionSequence();
            return;
        }

        OpenCompletedDialogue();
    }

    private void BeginCompletionSequence()
    {
        if (sequenceStarted) return;
        sequenceStarted = true;

        MonoBehaviour runner = GameRoot.I != null ? (MonoBehaviour)GameRoot.I : this;
        runner.StartCoroutine(BeginCompletionSequenceWhenDialogueClosed());
    }

    private IEnumerator BeginCompletionSequenceWhenDialogueClosed()
    {
        var root = GameRoot.I;
        if (root != null && root.Dialogue != null)
        {
            // Give insert-side logic one frame to open its own dialogue first.
            yield return null;

            while (root.Dialogue.HasActiveSession || root.Dialogue.IsOpen)
                yield return null;

            // Require one extra idle frame to avoid same-frame/session handoff races.
            yield return null;
            while (root.Dialogue.HasActiveSession || root.Dialogue.IsOpen)
                yield return null;
        }

        yield return CompletionSequenceRoutine();
    }

    private IEnumerator CompletionSequenceRoutine()
    {
        var root = GameRoot.I;
        if (root == null)
        {
            OpenCompletedDialogue();
            yield break;
        }

        bool pausedBySequence = false;
        bool lockedBySequence = false;
        bool handedOffToTransition = false;

        try
        {
            if (lockInputDuringSequence)
            {
                root.SetInputLocked(true);
                root.SetMoveLocked(true);
                lockedBySequence = true;
            }

            if (pauseWorldDuringSequence && root.Pause != null)
            {
                root.Pause.PushPause("GemSocketEndingSequence");
                pausedBySequence = true;
            }

            if (trophyVisual != null)
                trophyVisual.SetActive(true);

            if (completionMusic != null && root.Audio != null)
                root.Audio.PlayMusic(completionMusic, completionMusicCrossfade, completionMusicLoop);

            if (preDialogueDelay > 0f)
                yield return WaitRealtime(preDialogueDelay);

            if (useFixedRhythmDialogue)
            {
                DialogueLine[] lines = BuildFixedRhythmLines(root);
                if (lines != null && lines.Length > 0)
                    yield return PlayFixedRhythmDialogue(root, lines);
                else
                    yield return PlayInteractiveDialogue(root, onCompletedDialogue);
            }
            else
            {
                yield return PlayInteractiveDialogue(root, onCompletedDialogue);
            }

            if (!string.IsNullOrWhiteSpace(unlockEndingId))
            {
                bool newlyUnlocked = EndingCollectionService.Unlock(unlockEndingId.Trim());
                Debug.Log($"[GemSocketGroupController] Unlock ending id='{unlockEndingId}', newlyUnlocked={newlyUnlocked}");
            }

            if (postDialogueDelay > 0f)
                yield return WaitRealtime(postDialogueDelay);

            if (!string.IsNullOrWhiteSpace(endSceneName))
            {
                root.TransitionTo(endSceneName.Trim(), endSpawnId, endSceneFadeOut, endSceneFadeIn);
                while (root != null && root.IsTransitioning)
                    yield return null;
            }

            if (returnToMainMenuAfterEndScene)
            {
                if (lockInputDuringSequence)
                {
                    root.SetInputLocked(true);
                    root.SetMoveLocked(true);
                }

                if (returnDelaySeconds > 0f)
                    yield return WaitRealtime(returnDelaySeconds);

                if (!string.IsNullOrWhiteSpace(mainMenuSceneName))
                {
                    root.TransitionTo(mainMenuSceneName.Trim(), mainMenuSpawnId, mainMenuFadeOut, mainMenuFadeIn);
                    handedOffToTransition = true;
                    while (root != null && root.IsTransitioning)
                        yield return null;
                }
            }
        }
        finally
        {
            if (root != null && pausedBySequence && root.Pause != null)
                root.Pause.PopPause("GemSocketEndingSequence");

            if (root != null && lockedBySequence && !handedOffToTransition)
            {
                root.SetInputLocked(false);
                root.SetMoveLocked(false);
            }
        }
    }

    private void OpenCompletedDialogue()
    {
        if (onCompletedDialogue == null || GameRoot.I == null || GameRoot.I.Dialogue == null) return;
        if (GameRoot.I.Dialogue.IsOpen) return;
        GameRoot.I.Dialogue.Open("_gem_socket_group", onCompletedDialogue);
    }

    private DialogueLine[] BuildFixedRhythmLines(GameRoot root)
    {
        if (onCompletedDialogue == null || root == null || root.Dialogue == null)
            return null;

        if (onCompletedDialogue is GraphDialogueAsset)
        {
            Debug.LogWarning("[GemSocketGroupController] Fixed rhythm dialogue does not support GraphDialogueAsset. Fallback to interactive dialogue.");
            return null;
        }

        DialogueSession session = onCompletedDialogue.BuildSession("_gem_socket_group", root.Dialogue.DialogueState);
        return session != null ? session.lines : null;
    }

    private IEnumerator PlayInteractiveDialogue(GameRoot root, DialogueAsset asset)
    {
        if (asset == null || root == null || root.Dialogue == null) yield break;

        var dialogue = root.Dialogue;
        bool done = false;
        void OnSessionClosed() => done = true;

        dialogue.OnSessionClosed += OnSessionClosed;
        try
        {
            while (dialogue.HasActiveSession)
                yield return null;

            dialogue.Open("_gem_socket_group", asset);

            if (!dialogue.HasActiveSession)
                done = true;

            while (!done)
                yield return null;
        }
        finally
        {
            dialogue.OnSessionClosed -= OnSessionClosed;
        }
    }

    private IEnumerator PlayFixedRhythmDialogue(GameRoot root, DialogueLine[] lines)
    {
        var ui = root != null && root.Dialogue != null ? root.Dialogue.ui : null;
        if (ui == null || lines == null || lines.Length == 0) yield break;

        bool wasOpen = ui.IsOpen;
        bool oldIgnoreInput = ui.IgnoreInput;
        ui.IgnoreInput = true;

        try
        {
            for (int i = 0; i < lines.Length; i++)
            {
                ui.Open(new[] { lines[i] });

                // Keep typewriter animation, but still auto-advance without player input.
                while (ui.IsTyping)
                    yield return null;

                float hold = Mathf.Max(0f, fixedRhythmSecondsPerLine);
                if (hold <= 0f) continue;

                yield return WaitRealtime(hold);
            }
        }
        finally
        {
            ui.IgnoreInput = oldIgnoreInput;
            if (!wasOpen)
                ui.Close();
        }
    }

    private static IEnumerator WaitRealtime(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

}

using System.Collections;
using Game.Core;
using Game.Gameplay.Player;
using Game.Systems.Items.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameRoot : MonoBehaviour
{
    public static GameRoot I { get; private set; }

    [Header("Refs (Boot Scene)")]
    public Game.Systems.Items.Inventory Inventory;
    public SpawnOnLoad playerSpawn;
    public CameraFollow2D cameraFollow;
    public FadeController fade;
    public PlayerInputReader playerInput;
    public HeldItem playerHeldItem;
    public HeldItemVisualController vis;
    public GlitchVolumeTransition glitchVolume;
    public AudioSystem Audio;
    public AudioSource globalSfxSource;
    [SerializeField] private AudioClip glitchTransitionSfx;

    [Header("Systems (Boot Scene children)")]
    [SerializeField] private StoryManager storyManager;
    [SerializeField] private LocalizationService localization;
    [SerializeField] private DialogueSystem dialogue;
    [SerializeField] private PauseManager pause;
    [SerializeField] private TriggerManager triggerManager;

    [Header("Display")]
    [SerializeField] private bool forceBuildResolution = true;
    [SerializeField] private bool applyForcedResolutionInEditor = false;
    [SerializeField] private int forcedScreenWidth = 1920;
    [SerializeField] private int forcedScreenHeight = 1080;
    [SerializeField] private bool reapplyResolutionOnFocus = true;

    [Header("Runtime (auto found)")]
    [SerializeField] private GameObject player;

    public TriggerManager Triggers => triggerManager;
    public PlayerInteractor PlayerInteractor { get; private set; }
    public StoryManager Story => storyManager;
    public LocalizationService Localization => localization;
    public DialogueSystem Dialogue => dialogue;
    public PauseManager Pause => pause;

    public GlobalState Global { get; private set; } = new GlobalState();
    public bool InputLocked { get; private set; }
    public bool IsTransitioning { get; private set; }
    public const string STATE_GLITCH_WORLD = "IsGlitchWorld";
    public const string STATE_WORLD_BOX1_ENTER_COUNT = "world.box1.enter.count";
    public const string STATE_ADMIN_DISABLED = "story.admin.disabled";
    public bool IsGlitchWorld => Global.GetBool(STATE_GLITCH_WORLD);

    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        ApplyForcedResolution();

        // 鉁?鍒濆鍖栫粍浠惰幏鍙?
        if (localization == null) localization = GetComponentInChildren<LocalizationService>(true);
        if (dialogue == null) dialogue = GetComponentInChildren<DialogueSystem>(true);
        if (pause == null) pause = GetComponentInChildren<PauseManager>(true);
        if (storyManager == null) storyManager = GetComponentInChildren<StoryManager>(true);
        if (triggerManager == null) triggerManager = GetComponentInChildren<TriggerManager>(true);
        if (Audio == null) Audio = GetComponentInChildren<AudioSystem>(true);
        if (vis == null) vis = GetComponentInChildren<HeldItemVisualController>(true);
        if (globalSfxSource == null) Debug.LogWarning("[GameRoot] No globalSfxSource Found");

        DontDestroyOnLoad(gameObject);
        RefreshRuntimeRefs();
        var initialScene = SceneManager.GetActiveScene();
        BindRoomChaosForScene(initialScene);
        TrackSpecialSceneCounters(initialScene);
        if (globalSfxSource != null)
        {
            globalSfxSource.ignoreListenerPause = true;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        StartCoroutine(ApplyForcedResolutionNextFrame());
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) return;
        if (!reapplyResolutionOnFocus) return;
        ApplyForcedResolution();
    }

    private void OnDestroy()
    {
        if (I == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            I = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindRoomChaosForScene(scene);
        TrackSpecialSceneCounters(scene);
        RefreshRuntimeRefs();
        if (IsTransitioning) return;
        ApplyLevelCameraSettings();
    }

    private void TrackSpecialSceneCounters(Scene scene)
    {
        if (!scene.IsValid()) return;
        if (Global == null) return;

        if (scene.name == "World_Box1")
            Global.AddInt(STATE_WORLD_BOX1_ENTER_COUNT, 1);
    }

    private void BindRoomChaosForScene(Scene scene)
    {
        if (Global == null) return;

        int defaultLevel = 0;
        string roomId = scene.name;

        RoomChaosContext context = null;
        var contexts = FindObjectsByType<RoomChaosContext>(FindObjectsSortMode.None);
        for (int i = 0; i < contexts.Length; i++)
        {
            if (contexts[i] != null && contexts[i].gameObject.scene == scene)
            {
                context = contexts[i];
                break;
            }
        }

        if (context != null)
        {
            defaultLevel = context.defaultLevel;
            roomId = context.GetEffectiveRoomId(scene);
        }

        RoomChaosService.BindRoom(Global, roomId, defaultLevel);
    }

    public void RefreshRuntimeRefs()
    {
        if (cameraFollow == null) cameraFollow = FindFirstObjectByType<CameraFollow2D>();
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            if (playerInput == null) playerInput = player.GetComponent<PlayerInputReader>();
            if (playerHeldItem == null) playerHeldItem = player.GetComponent<HeldItem>();
            PlayerInteractor = player.GetComponent<PlayerInteractor>();
        }

        if (playerSpawn == null && player != null)
            playerSpawn = player.GetComponent<SpawnOnLoad>();

        if (player != null && PlayerInteractor == null)
            Debug.LogWarning("[GameRoot] PlayerInteractor not found on Player.");
    }

    public void SetInputLocked(bool locked)
    {
        InputLocked = locked;
        if (playerInput != null) playerInput.SetAllGameplayEnabled(!locked);
    }

    public void SetMoveLocked(bool locked)
    {
        if (playerInput != null) playerInput.SetMoveEnabled(!locked);
    }

    public void ResetRuntimeForNewGame()
    {
        if (Global != null)
            Global.ClearAll();

        SceneTransfer.NextSpawnId = null;
        SceneTransfer.HoldUp = false;
        SceneTransfer.HoldDown = false;
        SceneTransfer.HoldLeft = false;
        SceneTransfer.HoldRight = false;

        if (Inventory != null)
            Inventory.ClearAll();
        RuntimeItemPriceOverrides.ClearAll();

        if (playerHeldItem != null)
            playerHeldItem.SetHeld(null);

        if (player != null)
        {
            var stats = player.GetComponent<PlayerStats>();
            if (stats != null)
                stats.ResetForNewGame();
        }

        if (pause != null)
            pause.ForceResume();

        SetInputLocked(false);
        SetMoveLocked(false);
    }

    public void ApplyLevelCameraSettings()
    {
        if (cameraFollow == null) return;
        var settings = FindFirstObjectByType<LevelCameraSettings>();
        if (settings == null) { cameraFollow.SetBounds(null); return; }

        cameraFollow.SetFollowMode(settings.defaultMode);
        cameraFollow.SetBounds(settings.bounds);
        if (settings.snapOnEnter) cameraFollow.SnapToTarget();
    }

    public void TransitionTo(string toScene, string toSpawnId, float fadeOutTime = 0.10f, float fadeInTime = 0.10f)
    {
        if (IsTransitioning) return;
        StartCoroutine(TransitionRoutine(toScene, toSpawnId, fadeOutTime, fadeInTime));
    }

    private IEnumerator TransitionRoutine(string toScene, string toSpawnId, float fadeOutTime, float fadeInTime)
    {
        IsTransitioning = true;
        SetInputLocked(true);
        bool usedFadeOut = false;
        bool usedGlitchOut = false;

        try
        {
            if (Dialogue != null && Dialogue.IsOpen) Dialogue.Close();

            // 鉁?杞満寮€濮嬶細瑙嗗惉鍚屾
            if (IsGlitchWorld)
            {
                if (globalSfxSource != null && glitchTransitionSfx != null)
                    globalSfxSource.PlayOneShot(glitchTransitionSfx);

                if (glitchVolume != null)
                {
                    yield return glitchVolume.GlitchOut();
                    usedGlitchOut = true;
                }
            }
            else
            {
                if (fade != null)
                {
                    yield return fade.FadeOut(fadeOutTime);
                    usedFadeOut = true;
                }
            }

            SceneTransfer.NextSpawnId = toSpawnId;

            if (SceneManager.GetActiveScene().name != toScene)
            {
                var op = SceneManager.LoadSceneAsync(toScene);
                while (!op.isDone) yield return null;
            }

            yield return null;
            RefreshRuntimeRefs();

            if (playerSpawn != null && !string.IsNullOrEmpty(SceneTransfer.NextSpawnId))
                yield return playerSpawn.SpawnTo(SceneTransfer.NextSpawnId);

            if (Time.timeScale > 0f)
                yield return new WaitForFixedUpdate();
            else
                yield return new WaitForSecondsRealtime(Time.fixedDeltaTime);

            ApplyLevelCameraSettings();
            if (cameraFollow != null) cameraFollow.SnapToTarget();

            if (player != null)
            {
                var stats = player.GetComponent<PlayerStats>();
                if (stats != null && stats.IsDead) stats.ReviveToFull();
                I.Triggers.Raise(new DeathEvent());
            }
            

            // 鉁?杞満缁撴潫
            if (IsGlitchWorld)
            {
                if (glitchVolume != null) yield return glitchVolume.GlitchIn();

                // If we faded out before load but now resolved to glitch-in path,
                // ensure fade canvas is cleared so the screen cannot stay black.
                if (usedFadeOut && fade != null)
                    fade.SetAlpha(0f);
            }
            else
            {
                // Symmetric guard: if we glitched out before load but now resolved to
                // non-glitch path, bring glitch intensity back down explicitly.
                if (usedGlitchOut && glitchVolume != null)
                    yield return glitchVolume.GlitchIn();

                if (fade != null) yield return fade.FadeIn(fadeInTime);
            }
        }
        finally
        {
            SceneTransfer.NextSpawnId = null;
            SetInputLocked(false);
            SetMoveLocked(false);
            IsTransitioning = false;
            Triggers.Raise(new SceneEnteredEvent());
        }
    }

    private void OnApplicationQuit() { I = null; }

    private void ApplyForcedResolution()
    {
        if (!forceBuildResolution) return;
        if (!applyForcedResolutionInEditor && Application.isEditor) return;

        int w = Mathf.Max(320, forcedScreenWidth);
        int h = Mathf.Max(180, forcedScreenHeight);
        Screen.fullScreen = false;
        Screen.fullScreenMode = FullScreenMode.Windowed;
        Screen.SetResolution(w, h, FullScreenMode.Windowed);
    }

    private IEnumerator ApplyForcedResolutionNextFrame()
    {
        yield return null;
        ApplyForcedResolution();
    }
}

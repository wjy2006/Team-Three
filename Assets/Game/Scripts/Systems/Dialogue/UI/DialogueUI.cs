using TMPro;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

// ✅ 更新：结构体里增加了 interval 字段
[Serializable]
public struct SpeakerVoice
{
    public string speakerKey;
    public AudioClip voiceClip;
    [Range(0.5f, 2.0f)] public float pitch; 
    public float interval; // ✅ 每个角色独立的频率
}

public class DialogueUI : MonoBehaviour
{
    [Header("Root UI")]
    public GameObject dialogRoot;

    [Header("UI References")]
    public TMP_Text nameText;
    public TMP_Text contentText;

    [Header("Typewriter")]
    public float charsPerSecond = 40f;
    public float punctuationPause = 0.03f;

    [Header("Voice Settings")]
    public AudioSource voiceAudioSource;
    public float voiceInterval = 0.06f; // 这里作为默认全局频率
    public AudioClip defaultVoiceClip;
    [Range(0.5f, 2.0f)] public float defaultPitch = 1.0f;
    public List<SpeakerVoice> speakerVoices = new List<SpeakerVoice>();

    private PlayerInputReader input;
    private DialogueLine[] lines;
    private int index;
    private int openFrame;

    private Coroutine typingCo;
    private string fullContent;
    private bool isTyping;
    private bool skipTypingRequested;

    // ✅ 当前句子的音频属性缓存
    private AudioClip currentVoiceClip;
    private float currentVoicePitch;
    private float currentVoiceInterval; // ✅ 新增：当前频率缓存

    public event Action OnClosed;
    public event Action OnNodeEnd;

    public bool IsOpen { get; private set; }
    public bool IgnoreInput { get; set; }
    public bool IsTyping => isTyping;

    void Awake()
    {
        if (dialogRoot == null) dialogRoot = gameObject;
        dialogRoot.SetActive(false);
        voiceAudioSource.ignoreListenerPause = true;
    }

    void Start()
    {
        if (GameRoot.I != null)
            input = GameRoot.I.playerInput;
    }

    void Update()
    {
        if (!IsOpen) return;
        if (IgnoreInput) return;
        if (Time.frameCount == openFrame) return;
        if (input == null) return;

        input.ConsumeMenuDown();

        if (input.ConsumeContinueDown())
        {
            input.ConsumeInteractDown();
            if (isTyping) return;
            Next();
            return;
        }

        if (input.ConsumeCancelDown())
        {
            input.ConsumeInteractDown();
            if (isTyping) RequestSkipTyping();
            return;
        }
    }

    public void Open(DialogueLine[] newLines)
    {
        if (newLines == null || newLines.Length == 0) return;

        EnsureOpen();

        lines = newLines;
        index = 0;
        Show();
    }

    public void ShowImmediateLine(DialogueLine line)
    {
        EnsureOpen();
        StopTypingIfNeeded();

        var loc = GameRoot.I != null ? GameRoot.I.Localization : null;
        string speaker = line.speakerKey;
        string contentKey = line.textKey;

        nameText.text = loc != null ? loc.Get(speaker) : speaker;
        fullContent = loc != null ? loc.Get(contentKey) : contentKey;
        contentText.text = fullContent;
    }

    private void EnsureOpen()
    {
        if (IsOpen) return;

        if (GameRoot.I != null && GameRoot.I.Pause != null)
            GameRoot.I.Pause.PushPause("Dialogue");

        dialogRoot.SetActive(true);
        IsOpen = true;
        openFrame = Time.frameCount;
    }

    void Next()
    {
        index++;
        if (index >= lines.Length)
        {
            OnNodeEnd?.Invoke();
            return;
        }
        Show();
    }

    void Show()
    {
        StopTypingIfNeeded();

        var loc = GameRoot.I != null ? GameRoot.I.Localization : null;

        string speaker = lines[index].speakerKey;
        string contentKey = lines[index].textKey;

        nameText.text = loc != null ? loc.Get(speaker) : speaker;
        fullContent = loc != null ? loc.Get(contentKey) : contentKey;

        // ✅ 匹配声音逻辑：初始化为默认值
        currentVoiceClip = defaultVoiceClip;
        currentVoicePitch = defaultPitch;
        currentVoiceInterval = voiceInterval; // ✅ 默认使用全局 Interval

        foreach (var v in speakerVoices)
        {
            if (v.speakerKey == speaker)
            {
                currentVoiceClip = v.voiceClip;
                currentVoicePitch = v.pitch;
                currentVoiceInterval = v.interval; // ✅ 匹配到角色特有 Interval
                break;
            }
        }

        contentText.text = "";
        isTyping = true;
        skipTypingRequested = false;
        typingCo = StartCoroutine(TypeLine(fullContent));
    }

    private IEnumerator TypeLine(string text)
    {
        if (charsPerSecond <= 0f) charsPerSecond = 9999f;
        float secPerChar = 1f / charsPerSecond;
        float voiceTimer = 0f;

        float safePitch = Mathf.Max(0.1f, currentVoicePitch);
        voiceAudioSource.pitch = safePitch;

        for (int i = 0; i < text.Length; i++)
        {
            if (skipTypingRequested) break;

            char c = text[i];
            contentText.text += c;

            // ✅ 使用 currentVoiceInterval
            if (!char.IsWhiteSpace(c) && voiceTimer <= 0f && currentVoiceClip != null)
            {
                voiceAudioSource.PlayOneShot(currentVoiceClip);
                voiceTimer = currentVoiceInterval; 
            }

            float extra = 0f;
            if (punctuationPause > 0f && IsPunctuation(text[i]))
                extra = punctuationPause;

            float wait = secPerChar + extra;
            float t = 0f;

            while (t < wait)
            {
                if (skipTypingRequested) break;
                float dt = Time.unscaledDeltaTime;
                t += dt;
                voiceTimer -= dt; 
                yield return null;
            }
        }

        contentText.text = text;
        isTyping = false;
        typingCo = null;
    }

    public void RequestSkipTyping()
    {
        skipTypingRequested = true;
    }

    private void StopTypingIfNeeded()
    {
        if (typingCo != null)
        {
            //StopCoroutine(typingCo);
            typingCo = null;
        }
        isTyping = false;
        skipTypingRequested = false;
    }

    private bool IsPunctuation(char c)
    {
        return c == '。' || c == '！' || c == '？' || c == '，' ||
               c == '、' || c == '：' || c == ';' || c == '；';
    }

    public void Close()
    {
        if (!IsOpen) return;

        StopTypingIfNeeded();
        IsOpen = false;
        dialogRoot.SetActive(false);

        if (GameRoot.I != null && GameRoot.I.Pause != null)
            GameRoot.I.Pause.PopPause("Dialogue");

        OnClosed?.Invoke();
    }
}

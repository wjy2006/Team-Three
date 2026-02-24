using TMPro;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

// ✅ 定义一个简单的结构，用来在面板里配置不同人的声音
[Serializable]
public struct SpeakerVoice
{
    public string speakerKey;
    public AudioClip voiceClip;
    [Range(0.5f, 2.0f)] public float pitch; // 通过音调区分不同角色非常有效
}

public class DialogueUI : MonoBehaviour
{
    [Header("Root UI")]
    public GameObject dialogRoot;

    [Header("UI References")]
    public TMP_Text nameText;
    public TMP_Text contentText;

    [Header("Typewriter")]
    [Tooltip("每秒显示多少个字符。比如 40 = 大约每秒40字母/字符。")]
    public float charsPerSecond = 40f;

    [Tooltip("标点额外停顿（秒），让节奏更像对话。可设 0 关闭。")]
    public float punctuationPause = 0.03f;

    // ✅ 新增：音频配置
    [Header("Voice Settings")]
    public AudioSource voiceAudioSource;
    public float voiceInterval = 0.06f; // Undertale通常是0.06~0.08秒播一次声音
    public AudioClip defaultVoiceClip;
    [Range(0.5f, 2.0f)] public float defaultPitch = 1.0f;
    public List<SpeakerVoice> speakerVoices = new List<SpeakerVoice>();

    private PlayerInputReader input;
    private DialogueLine[] lines;
    private int index;
    private int openFrame;

    private Coroutine typingCo;
    private string fullContent;       // 当前句的完整文本
    private bool isTyping;            // 是否正在逐字输出
    private bool skipTypingRequested; // 是否请求“立刻显示完本句”

    // ✅ 当前句子的音频缓存
    private AudioClip currentVoiceClip;
    private float currentVoicePitch;

    public event Action OnClosed;     // 整个对话UI彻底关闭时触发
    public event Action OnNodeEnd;    // 当前节点的所有句子播完时触发

    public bool IsOpen { get; private set; }

    void Awake()
    {
        if (dialogRoot == null) dialogRoot = gameObject;
        dialogRoot.SetActive(false);
        // 关键：对话可能发生在暂停期间，不能让声音挂掉
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

        if (!IsOpen)
        {
            if (GameRoot.I != null && GameRoot.I.Pause != null)
                GameRoot.I.Pause.PushPause("Dialogue");

            dialogRoot.SetActive(true);
            IsOpen = true;
            openFrame = Time.frameCount;
        }

        lines = newLines;
        index = 0;
        Show();
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

        // ✅ 匹配声音：从列表中找，找不到就用默认值
        currentVoiceClip = defaultVoiceClip;
        currentVoicePitch = defaultPitch;

        foreach (var v in speakerVoices)
        {
            if (v.speakerKey == speaker)
            {
                currentVoiceClip = v.voiceClip;
                currentVoicePitch = v.pitch;
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

        float voiceTimer = 0f; // 音频冷却计时器

        for (int i = 0; i < text.Length; i++)
        {
            if (skipTypingRequested) break;

            char c = text[i];
            contentText.text += c;

            // ✅ 如果这不是空格，且冷却到了，就发声
            if (!char.IsWhiteSpace(c) && voiceTimer <= 0f && currentVoiceClip != null)
            {
                voiceAudioSource.pitch = currentVoicePitch;
                voiceAudioSource.PlayOneShot(currentVoiceClip);
                voiceTimer = voiceInterval; // 重置冷却
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
                voiceTimer -= dt; // 递减音频冷却
                yield return null;
            }
        }

        contentText.text = text;
        isTyping = false;
        typingCo = null;
        skipTypingRequested = false;
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
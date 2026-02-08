using TMPro;
using UnityEngine;
using System;
using System.Collections;

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

    private PlayerInputReader input;
    private DialogueLine[] lines;
    private int index;
    private int openFrame;

    private Coroutine typingCo;
    private string fullContent;       // 当前句的完整文本
    private bool isTyping;            // 是否正在逐字输出
    private bool skipTypingRequested; // 是否请求“立刻显示完本句”

    public event Action OnClosed;     // 整个对话UI彻底关闭时触发
    public event Action OnNodeEnd;   // ✅ 新增：当前节点的所有句子播完时触发

    public bool IsOpen { get; private set; }

    void Awake()
    {
        if (dialogRoot == null) dialogRoot = gameObject;
        dialogRoot.SetActive(false);
    }

    void Start()
    {
        if (GameRoot.I != null)
            input = GameRoot.I.playerInput;
    }

    void Update()
    {
        if (!IsOpen) return;

        // ✅ 打开当帧保护
        if (Time.frameCount == openFrame) return;
        if (input == null) return;

        // ✅ 对话期间：不允许菜单键穿透
        input.ConsumeMenuDown();

        // ✅ Continue 键逻辑
        if (input.ConsumeContinueDown())
        {
            input.ConsumeInteractDown(); // 同帧吞掉交互

            if (isTyping)
            {
                // Undertale风格：没打完时按 Continue 不推进（如果你想按确认键也跳过文字，可以这里调 RequestSkip）
                return;
            }

            Next();
            return;
        }

        // ✅ Cancel 键逻辑
        if (input.ConsumeCancelDown())
        {
            input.ConsumeInteractDown(); // 同帧吞掉交互

            if (isTyping)
            {
                RequestSkipTyping();
            }
            return;
        }
    }

    public void Open(DialogueLine[] newLines)
    {
        if (newLines == null || newLines.Length == 0) return;

        // ✅ 只有从“完全关闭”状态进入时，才执行暂停和开启动画
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
            // ✅ 关键改动：一节话说完后，不直接 Close，而是通知 System
            // System 会决定是给下一段对话（无缝切换）还是真的 Close
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

        // 开始逐字打字
        contentText.text = "";
        isTyping = true;
        skipTypingRequested = false;
        typingCo = StartCoroutine(TypeLine(fullContent));
    }

    private IEnumerator TypeLine(string text)
    {
        if (charsPerSecond <= 0f) charsPerSecond = 9999f;
        float secPerChar = 1f / charsPerSecond;

        for (int i = 0; i < text.Length; i++)
        {
            if (skipTypingRequested) break;

            contentText.text += text[i];

            float extra = 0f;
            if (punctuationPause > 0f && IsPunctuation(text[i]))
                extra = punctuationPause;

            float wait = secPerChar + extra;
            float t = 0f;
            while (t < wait)
            {
                if (skipTypingRequested) break;
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        contentText.text = text; // 确保结束显示完整
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
            StopCoroutine(typingCo);
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

    // ✅ 这个方法现在由 System 真正决定何时调用
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
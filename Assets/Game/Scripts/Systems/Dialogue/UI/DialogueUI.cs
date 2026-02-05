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
    private string fullContent;       // 当前句的完整文本（已本地化）
    private bool isTyping;            // 是否正在逐字输出
    private bool skipTypingRequested; // 是否请求“立刻显示完本句”

    public event Action OnClosed;

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

        // ✅ Continue：只有在“本句已打完”时才推进
        if (input.ConsumeContinueDown())
        {
            input.ConsumeInteractDown(); // 同帧吞掉

            if (isTyping)
            {
                // ✅ Undertale风格：没打完时按 Continue 不推进、也不加速（直接无效）
                return;
            }

            Next();
            return;
        }

        // ✅ Cancel：用于“立刻显示完本句”，而不是关闭对话
        if (input.ConsumeCancelDown())
        {
            input.ConsumeInteractDown(); // 同帧吞掉

            if (isTyping)
            {
                RequestSkipTyping();
            }
            // 如果已经打完，则不做任何事（Ut里也不会用X关闭对话）
            return;
        }
    }

    public void Open(DialogueLine[] newLines)
    {
        if (newLines == null || newLines.Length == 0) return;

        // ✅ 暂停世界
        if (GameRoot.I != null && GameRoot.I.Pause != null)
            GameRoot.I.Pause.PushPause("Dialogue");

        lines = newLines;
        index = 0;
        IsOpen = true;
        openFrame = Time.frameCount;

        dialogRoot.SetActive(true);
        Show();
    }

    void Next()
    {
        index++;
        if (index >= lines.Length)
        {
            Close();
            return;
        }
        Show();
    }

    void Show()
    {
        // 停掉上一句的打字协程
        StopTypingIfNeeded();

        var loc = GameRoot.I != null ? GameRoot.I.Localization : null;

        string speaker = lines[index].speakerKey;
        string contentKey = lines[index].textKey;

        nameText.text = loc != null ? loc.Get(speaker) : speaker;

        fullContent = loc != null ? loc.Get(contentKey) : contentKey;

        // 开始逐字
        contentText.text = "";
        isTyping = true;
        skipTypingRequested = false;
        typingCo = StartCoroutine(TypeLine(fullContent));
    }

    private IEnumerator TypeLine(string text)
    {
        if (charsPerSecond <= 0f) charsPerSecond = 9999f; // 防御：<=0 就当瞬间显示

        float secPerChar = 1f / charsPerSecond;

        for (int i = 0; i < text.Length; i++)
        {
            if (skipTypingRequested)
                break;

            contentText.text += text[i];

            // 标点额外停顿（可选）
            float extra = 0f;
            if (punctuationPause > 0f && IsPunctuation(text[i]))
                extra = punctuationPause;

            // 用 unscaled，避免你以后 timeScale=0 时对话不动
            float wait = secPerChar + extra;
            float t = 0f;
            while (t < wait)
            {
                if (skipTypingRequested) break;
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        // 跳过或输出完：直接显示全句
        contentText.text = text;

        isTyping = false;
        typingCo = null;
        skipTypingRequested = false;
    }

    private void RequestSkipTyping()
    {
        skipTypingRequested = true;
        // 协程会在下一次循环检测到并直接显示全文
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
        // 中英文常见停顿符号
        return c == '。' || c == '！' || c == '？' || c == '，' ||
               c == '、' || c == '：' || c == ';' || c == '；';
    }

    public void Close()
    {
        StopTypingIfNeeded();

        IsOpen = false;
        OnClosed?.Invoke();
        dialogRoot.SetActive(false);

        // ✅ 恢复暂停计数
        if (GameRoot.I != null && GameRoot.I.Pause != null)
            GameRoot.I.Pause.PopPause("Dialogue");
    }
}

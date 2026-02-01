using TMPro;
using UnityEngine;
using System;

public class DialogueUI : MonoBehaviour
{
    [Header("Root UI")]
    public GameObject dialogRoot;

    [Header("UI References")]
    public TMP_Text nameText;
    public TMP_Text contentText;

    private PlayerInputReader input;
    private DialogueLine[] lines;
    private int index;
    private int openFrame;
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

        // ✅ 你原来的“打开当帧保护”
        if (Time.frameCount == openFrame) return;

        if (input == null) return;

        // ✅ 对话期间：不允许菜单键穿透
        // （即便有人在别处 ConsumeMenuDown，也不会拿到）
        input.ConsumeMenuDown();

        // ✅ Continue：推进对话
        if (input.ConsumeContinueDown())
        {
            // ✅ 关键：同一帧把 Interact 也吞掉
            // 防止 Z/Enter 同时触发“结束对话 + Interact”
            input.ConsumeInteractDown();

            Next();
            return;
        }

        // Cancel：关闭对话
        if (input.ConsumeCancelDown())
        {
            // ✅ 同帧也吞一下 Interact（避免 Cancel 和 Interact 混绑时穿透）
            input.ConsumeInteractDown();

            Close();
            return;
        }
    }


    public void Open(DialogueLine[] newLines)
    {
        if (newLines == null || newLines.Length == 0) return;

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
        var loc = GameRoot.I != null ? GameRoot.I.Localization : null;

        string speaker = lines[index].speakerKey;
        string content = lines[index].textKey;

        nameText.text = loc != null ? loc.Get(speaker) : speaker;
        contentText.text = loc != null ? loc.Get(content) : content;
    }

    public void Close()
    {
        IsOpen = false;
        OnClosed?.Invoke();
        dialogRoot.SetActive(false);
    }
}

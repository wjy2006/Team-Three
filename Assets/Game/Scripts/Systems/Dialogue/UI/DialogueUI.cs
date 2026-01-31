using TMPro;
using UnityEngine;

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

        if (Time.frameCount == openFrame) return;

        bool nextInput = false;
        bool cancelInput = false;

        if (input != null)
        {
            nextInput = input.ConsumeInteractDown();
            cancelInput = input.ConsumeCancelDown();
        }

        if (nextInput) Next();
        if (cancelInput) Close();
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

        // 如果找不到 Localization，就退回显示 key，方便排错
        nameText.text = loc != null ? loc.Get(speaker) : speaker;
        contentText.text = loc != null ? loc.Get(content) : content;
    }



    public void Close()
    {
        IsOpen = false;
        dialogRoot.SetActive(false);
    }
}

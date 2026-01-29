using System;
using TMPro;
using UnityEngine;

public class SimpleDialogTest : MonoBehaviour
{
    [Header("Root UI")]
    public GameObject dialogRoot;

    [Header("UI References")]
    public TMP_Text nameText;
    public TMP_Text contentText;

    public bool IsOpen { get; private set; }

    private int index = 0;
    private StoryData story;
    private PlayerInputReader input;
    private int openFrame = -1;

    [Serializable] public class Line { public string name; public string text; }
    [Serializable] public class StoryData { public Line[] lines; }

    void Awake()
    {
        if (dialogRoot == null) dialogRoot = gameObject;
        dialogRoot.SetActive(false);
        IsOpen = false;
    }

    void Start()
    {
        if (GameRoot.I != null) input = GameRoot.I.playerInput;
    }

    void Update()
    {
        if (!IsOpen) return;

        // 帧保护：防止开启对话的那一帧立刻触发下一句
        if (Time.frameCount == openFrame) return;

        bool nextInput = false;
        bool cancelInput = false;

        // 优先使用 GameRoot 的 InputReader
        if (input != null)
        {
            // 只有当 PlayerInteractor 不消耗时，这里才能读到 True
            nextInput = input.ConsumeInteractDown();
            cancelInput = input.ConsumeCancelDown();
        }
        else
        {
            // 备用方案
            nextInput = Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);
            cancelInput = Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Escape);
        }

        if (nextInput)
        {
            // Debug.Log("Dialog: 检测到下一步输入，显示下一句"); // 调试用
            Next();
        }

        if (cancelInput)
        {
            Close();
        }
    }

    public void Open(TextAsset storyJson)
    {
        if (storyJson == null) return;

        story = JsonUtility.FromJson<StoryData>(storyJson.text);
        if (story == null || story.lines == null) return;

        index = 0;
        dialogRoot.SetActive(true);
        IsOpen = true;
        openFrame = Time.frameCount; // 记录开启帧

        ShowLine();
    }

    public void Close()
    {
        IsOpen = false;
        dialogRoot.SetActive(false);
    }

    private void Next()
    {
        index++;
        if (index >= story.lines.Length)
        {
            Close();
            return;
        }
        ShowLine();
    }

    private void ShowLine()
    {
        if (index < story.lines.Length)
        {
            nameText.text = story.lines[index].name;
            contentText.text = story.lines[index].text;
        }
    }
}
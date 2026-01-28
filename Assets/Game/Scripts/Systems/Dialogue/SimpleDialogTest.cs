using System;
using TMPro;
using UnityEngine;

public class SimpleDialogTest : MonoBehaviour
{
    [Header("Root UI (DialogPanel or whole dialog UI root)")]
    public GameObject dialogRoot;

    [Header("UI References")]
    public TMP_Text nameText;
    public TMP_Text contentText;

    public bool IsOpen { get; private set; }

    private int index = 0;
    private StoryData story;

    [Serializable] public class Line { public string name; public string text; }
    [Serializable] public class StoryData { public Line[] lines; }

    void Awake()
    {
        if (dialogRoot == null) dialogRoot = gameObject;
        dialogRoot.SetActive(false);
        IsOpen = false;
    }

    void Update()
    {
        if (!IsOpen) return;

        if (Input.GetKeyDown(KeyCode.Z) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetMouseButtonDown(0))
        {
            Next();
        }

        if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    // ⭐️ 新接口：直接传 TextAsset
    public void Open(TextAsset storyJson)
    {
        if (storyJson == null)
        {
            Debug.LogError("Open 失败：storyJson 为空（没有绑定 TextAsset）");
            return;
        }

        story = JsonUtility.FromJson<StoryData>(storyJson.text);
        if (story == null || story.lines == null || story.lines.Length == 0)
        {
            Debug.LogError("Open 失败：JSON 解析后没有 lines（检查格式）");
            return;
        }

        index = 0;
        dialogRoot.SetActive(true);
        IsOpen = true;
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
        nameText.text = story.lines[index].name;
        contentText.text = story.lines[index].text;
    }
}

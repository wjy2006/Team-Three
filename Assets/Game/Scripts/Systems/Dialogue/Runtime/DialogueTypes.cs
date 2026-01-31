using System;
using UnityEngine;

[Serializable]
public struct DialogueLine
{
    public string speakerKey;  // 可以是名字key，也可以直接写 "Bob"
    public string textKey;     // 必须是 key
}

public class DialogueSession
{
    public DialogueLine[] lines;
    public DialogueSession(DialogueLine[] lines) => this.lines = lines;
}

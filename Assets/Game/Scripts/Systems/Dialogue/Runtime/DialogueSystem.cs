using UnityEngine;

public class DialogueSystem : MonoBehaviour
{
    public DialogueUI ui;

    public DialogueState State { get; private set; } = new DialogueState();

    public bool IsOpen => ui != null && ui.IsOpen;

    // ⭐ 对外统一接口
    public void Open(string npcId, DialogueAsset asset)
    {
        if (IsOpen || asset == null) return;

        DialogueSession session = asset.BuildSession(npcId, State);
        if (session == null || session.lines == null || session.lines.Length == 0)
            return;

        ui.Open(session.lines);
    }

    public void Close()
    {
        if (!IsOpen) return;
        ui.Close();
    }
}

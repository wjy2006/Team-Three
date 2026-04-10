using UnityEngine;

[DisallowMultipleComponent]
public class GemSocketGroupController : MonoBehaviour
{
    [Header("Sockets")]
    public GemSocket2D redSocket;
    public GemSocket2D greenSocket;
    public GemSocket2D blueSocket;

    [Header("Completion")]
    [Tooltip("Optional GlobalState key set to true when all sockets are inserted.")]
    public string completedGlobalKey;

    [Header("Dialogue (Optional)")]
    public DialogueAsset onCompletedDialogue;

    private bool completed;

    private void Awake()
    {
        if (!string.IsNullOrEmpty(completedGlobalKey) && GameRoot.I != null)
            completed = GameRoot.I.Global.GetBool(completedGlobalKey);

        BindSocket(redSocket);
        BindSocket(greenSocket);
        BindSocket(blueSocket);

        if (!completed)
            TryComplete(openDialogue: false);
    }

    private void OnDestroy()
    {
        UnbindSocket(redSocket);
        UnbindSocket(greenSocket);
        UnbindSocket(blueSocket);
    }

    private void OnSocketInserted(GemSocket2D _)
    {
        TryComplete(openDialogue: true);
    }

    private void BindSocket(GemSocket2D socket)
    {
        if (socket == null) return;
        socket.OnInserted += OnSocketInserted;
    }

    private void UnbindSocket(GemSocket2D socket)
    {
        if (socket == null) return;
        socket.OnInserted -= OnSocketInserted;
    }

    private void TryComplete(bool openDialogue)
    {
        if (completed) return;
        if (redSocket == null || greenSocket == null || blueSocket == null) return;
        if (!redSocket.IsInserted || !greenSocket.IsInserted || !blueSocket.IsInserted) return;

        completed = true;
        if (!string.IsNullOrEmpty(completedGlobalKey))
            GameRoot.I?.Global?.SetBool(completedGlobalKey, true);

        if (!openDialogue) return;
        if (onCompletedDialogue == null || GameRoot.I == null || GameRoot.I.Dialogue == null) return;
        if (GameRoot.I.Dialogue.IsOpen) return;
        GameRoot.I.Dialogue.Open("_gem_socket_group", onCompletedDialogue);
    }
}

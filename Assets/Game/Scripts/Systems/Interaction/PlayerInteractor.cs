using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    public TopDownMove2D mover;
    public SimpleDialogTest dialog;
    public PlayerInputReader input;

    private InteractableNPC currentNPC;
    private bool waitRelease = false;

    void Awake()
    {
        if (mover == null) mover = GetComponent<TopDownMove2D>();
        if (input == null && GameRoot.I != null) input = GameRoot.I.playerInput;
        if (dialog == null && GameRoot.I != null) dialog = GameRoot.I.Dialogue;
    }

    void Update()
    {
        if (GameRoot.I != null && GameRoot.I.InputLocked) return;
        if (input == null) return;

        // 1. 优先检查对话框状态
        // 如果对话框开着，这里就把控制权完全让给 SimpleDialogTest，不要调用 ConsumeInteractDown()
        if (dialog != null && dialog.IsOpen)
        {
            if (mover != null) mover.canMove = false;
            // 只要对话开着，我们就不处理任何交互逻辑，直接返回
            return;
        }

        // 2. 对话没开，恢复移动
        if (mover != null) mover.canMove = true;

        // 3. 此时才去读取输入（因为确定没人跟我抢了）
        bool interactDownThisFrame = input.ConsumeInteractDown();

        // 交互键按住的逻辑处理（防连续触发）
        if (waitRelease)
        {
            if (input.InteractHeld) return;
            waitRelease = false;
        }

        // 4. 触发 NPC 交互
        if (currentNPC != null && interactDownThisFrame)
        {
            waitRelease = true;
            currentNPC.Interact();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var npc = other.GetComponent<InteractableNPC>();
        if (npc != null) currentNPC = npc;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var npc = other.GetComponent<InteractableNPC>();
        if (npc == currentNPC) currentNPC = null;
    }
}
using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    public TopDownMove2D mover;
    public SimpleDialogTest dialog;

    private InteractableNPC currentNPC;

    // 新增：需要先松开交互键，才能再次触发
    private bool waitRelease = false;

    void Awake()
    {
        if (mover == null) mover = GetComponent<TopDownMove2D>();
    }

    void Update()
    {
        if (GameRoot.I != null && GameRoot.I.InputLocked) return;
        // 对话开着：禁止移动
        if (dialog != null && dialog.IsOpen)
        {
            if (mover != null) mover.canMove = false;
            waitRelease = true; // 对话期间按过交互键，先锁住
            return;
        }
        else
        {
            if (mover != null) mover.canMove = true;
        }

        // 如果还没松开交互键，就不允许再次触发
        if (waitRelease)
        {
            if (IsInteractKeyHeld()) return;
            waitRelease = false; // 已松开，解除锁
        }

        // 按下交互键：Z / Enter / Space
        if (currentNPC != null && IsInteractKeyDown())
        {
            currentNPC.Interact();
        }
    }

    bool IsInteractKeyDown()
    {
        return Input.GetKeyDown(KeyCode.Z) ||
               Input.GetKeyDown(KeyCode.Return) ||
               Input.GetKeyDown(KeyCode.Space);
    }

    bool IsInteractKeyHeld()
    {
        return Input.GetKey(KeyCode.Z) ||
               Input.GetKey(KeyCode.Return) ||
               Input.GetKey(KeyCode.Space);
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

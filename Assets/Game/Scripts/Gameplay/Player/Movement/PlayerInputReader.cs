using UnityEngine;

public class PlayerInputReader : MonoBehaviour
{
    public Vector2 Move { get; private set; }

    // 本帧按下（边沿触发）
    public bool InteractDown { get; private set; }
    public bool CancelDown { get; private set; }

    // 是否按住
    public bool InteractHeld { get; private set; }
    public bool CancelHeld { get; private set; }

    private PlayerInputActions actions;

    private void Awake()
    {
        actions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        actions.Enable();
        actions.Player.Enable();
    }

    private void OnDisable()
    {
        actions.Disable();
    }

    private void Update()
    {
        // Move（Value Vector2）
        Move = actions.Player.Move.ReadValue<Vector2>();

        // Interact（Button）
        InteractDown = actions.Player.Interact.WasPressedThisFrame();
        InteractHeld = actions.Player.Interact.IsPressed();

        // Cancel（Button）——如果你还没建 Cancel action，这里会编译不过；没建就先删这几行
        CancelDown = actions.Player.Cancel.WasPressedThisFrame();
        CancelHeld = actions.Player.Cancel.IsPressed();
    }

    public bool ConsumeInteractDown()
    {
        if (!InteractDown) return false;
        InteractDown = false;
        return true;
    }

    public bool ConsumeCancelDown()
    {
        if (!CancelDown) return false;
        CancelDown = false;
        return true;
    }

    // 只锁移动（推荐用于对话）
    public void SetMoveEnabled(bool enabled)
    {
        if (enabled)
            actions.Player.Move.Enable();
        else
        {
            actions.Player.Move.Disable();
            Move = Vector2.zero;
        }
    }
}

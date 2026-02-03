using UnityEngine;

public class PlayerInputReader : MonoBehaviour
{
    public Vector2 Move { get; private set; }

    public bool InteractDown { get; private set; }
    public bool CancelDown { get; private set; }

    public bool InteractHeld { get; private set; }
    public bool CancelHeld { get; private set; }

    public bool MenuDown { get; private set; }
    public bool UpDown { get; private set; }
    public bool DownDown { get; private set; }
    public bool ContinueDown { get; private set; }

    public bool LeftDown { get; private set; }
    public bool RightDown { get; private set; }
    public bool ClickDown { get; private set; }
    public Vector2 PointerPos { get; private set; }      // 实时屏幕坐标
    public Vector2 ClickScreenPos { get; private set; }  // 点击瞬间屏幕坐标
    public bool ClickHeld { get; private set; }


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
        Move = actions.Player.Move.ReadValue<Vector2>();

        InteractDown = actions.Player.Interact.WasPressedThisFrame();
        InteractHeld = actions.Player.Interact.IsPressed();

        CancelDown = actions.Player.Cancel.WasPressedThisFrame();
        CancelHeld = actions.Player.Cancel.IsPressed();

        MenuDown = actions.Player.Menu.WasPressedThisFrame();
        UpDown = actions.Player.Up.WasPressedThisFrame();
        DownDown = actions.Player.Down.WasPressedThisFrame();
        ContinueDown = actions.Player.Continue.WasPressedThisFrame();

        LeftDown = actions.Player.Left.WasPressedThisFrame();
        RightDown = actions.Player.Right.WasPressedThisFrame();

        ClickDown = actions.Player.Click.WasPressedThisFrame();
        ClickHeld = actions.Player.Click.IsPressed(); 
        PointerPos = actions.Player.Point.ReadValue<Vector2>();

        if (ClickDown)
        {
            ClickScreenPos = PointerPos;
        }

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

    public bool ConsumeMenuDown()
    {
        if (!MenuDown) return false;
        MenuDown = false;
        return true;
    }

    public bool ConsumeUpDown()
    {
        if (!UpDown) return false;
        UpDown = false;
        return true;
    }

    public bool ConsumeDownDown()
    {
        if (!DownDown) return false;
        DownDown = false;
        return true;
    }

    public bool ConsumeContinueDown()
    {
        if (!ContinueDown) return false;
        ContinueDown = false;
        return true;
    }

    // ✅ 新增 Consume
    public bool ConsumeLeftDown()
    {
        if (!LeftDown) return false;
        LeftDown = false;
        return true;
    }

    public bool ConsumeRightDown()
    {
        if (!RightDown) return false;
        RightDown = false;
        return true;
    }


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
    public bool ConsumeClickDown(out Vector2 clickScreenPos)
    {
        if (!ClickDown)
        {
            clickScreenPos = default;
            return false;
        }
        ClickDown = false;
        clickScreenPos = ClickScreenPos;
        return true;
    }

}

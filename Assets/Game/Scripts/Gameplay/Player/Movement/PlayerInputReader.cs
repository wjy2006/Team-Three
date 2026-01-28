using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    public Vector2 Move { get; private set; }

    private PlayerInputActions actions;

    private void Awake()
    {
        actions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        actions.Enable();

        // Move 是 Value(Vector2)，performed/ canceled 都要接
        actions.Player.Move.performed += OnMove;
        actions.Player.Move.canceled += OnMove;
    }

    private void OnDisable()
    {
        actions.Player.Move.performed -= OnMove;
        actions.Player.Move.canceled -= OnMove;
        actions.Disable();
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        // ctx.ReadValue<Vector2>() 会稳定给你 (x,y)，按住 WD 就一直是 (1,1) normalized
        Move = ctx.ReadValue<Vector2>();
    }

    public void SetInputEnabled(bool enabled)
    {
        if (enabled)
        {
            actions.Player.Enable();
        }
        else
        {
            // 禁用 map 会让 Move 立刻 canceled -> Move 变 0
            actions.Player.Disable();
            Move = Vector2.zero;
        }
    }
}

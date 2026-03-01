using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInputReader))]
public class TopDownMove2D : MonoBehaviour
{
    [Header("Move Settings")]
    public float moveSpeed = 12f;
    public float acceleration = 40f;
    public float deceleration = 50f;
    public bool canMove = true;

    [Header("Physics")]
    public float maxTotalSpeed = 20f;

    private Rigidbody2D rb;
    private PlayerInputReader input;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        input = GetComponent<PlayerInputReader>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    private void FixedUpdate()
    {
        if (!canMove) return;
        if (GameRoot.I != null && GameRoot.I.InputLocked) return;

        // ✅ 判断是否正在“站定防御”
        bool isBracing = input != null && input.CancelHeld;

        // 如果在站定，wishDir 强制为 0，否则读取输入
        Vector2 moveInput = (!isBracing && input != null) ? input.Move : Vector2.zero;
        Vector2 wishDir = moveInput.sqrMagnitude > 0.0001f ? moveInput.normalized : Vector2.zero;

        Vector2 v = rb.linearVelocity;
        Vector2 targetVel = wishDir * moveSpeed;

        // ✅ 站定时使用减速度，或者可以设置一个更强力的“阻尼”让角色瞬间停下
        float rate = (isBracing || wishDir == Vector2.zero) ? deceleration : acceleration;
        
        v = Vector2.MoveTowards(v, targetVel, rate * Time.fixedDeltaTime);

        if (v.magnitude > maxTotalSpeed)
            v = v.normalized * maxTotalSpeed;

        rb.linearVelocity = v;
    }
}
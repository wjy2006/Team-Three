using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInputReader))]
public class TopDownMove2D : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 12f;

    [Tooltip("加速度：越大越跟手")]
    public float acceleration = 40f;

    [Tooltip("减速度：越大越快停")]
    public float deceleration = 50f;

    public bool canMove = true;

    [Header("Physics")]
    [Tooltip("把外力/击退当作额外速度保留，不要被移动覆盖")]
    public float maxTotalSpeed = 20f;

    private Rigidbody2D rb;
    private PlayerInputReader input;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        input = GetComponent<PlayerInputReader>();

        // 推荐的2D顶视角设置
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    private void FixedUpdate()
    {
        if (!canMove) return;
        if (GameRoot.I != null && GameRoot.I.InputLocked) return;

        Vector2 move = input != null ? input.Move : Vector2.zero;
        Vector2 wishDir = move.sqrMagnitude > 0.0001f ? move.normalized : Vector2.zero;

        // 当前速度（包含击退带来的速度）
        Vector2 v = rb.linearVelocity;

        // 我们只控制“自己走路想要的目标速度”
        Vector2 targetVel = wishDir * moveSpeed;

        // 用 MoveTowards 实现“加速/减速”，会更有动能感
        float rate = (wishDir == Vector2.zero) ? deceleration : acceleration;
        v = Vector2.MoveTowards(v, targetVel, rate * Time.fixedDeltaTime);

        // 限速：防止击退+移动叠加无限快（可选）
        if (v.magnitude > maxTotalSpeed)
            v = v.normalized * maxTotalSpeed;

        rb.linearVelocity = v;
    }
}

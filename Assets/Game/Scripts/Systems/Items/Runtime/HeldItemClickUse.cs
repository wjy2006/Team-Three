using UnityEngine;
using Game.Systems.Items;

public class HeldItemClickUse : MonoBehaviour
{
    private PlayerInputReader input;
    private Game.Gameplay.Player.HeldItem held;

    void Awake()
    {
        held = GetComponent<Game.Gameplay.Player.HeldItem>();
    }

    void Update()
    {
        // ✅ 延迟获取 input（避免启动顺序问题）
        if (input == null)
        {
            if (GameRoot.I != null) input = GameRoot.I.playerInput;
            if (input == null) return;
        }

        if (held == null) return;

        if (!input.ConsumeClickDown(out var clickScreenPos)) return;
        var item = held.held;
        if (item == null)
        {
            Debug.Log("Click: 手上是空的");
            return;
        }

        if (item.Effect == null)
        {
            Debug.LogWarning($"Click: 手持物品 {item.DisplayName} 没有绑定 Effect，无法使用");
            return;
        }
        var cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("Main Camera 不存在，或没有 Tag=MainCamera");
            return;
        }

        Vector3 wp3 = cam.ScreenToWorldPoint(new Vector3(clickScreenPos.x, clickScreenPos.y, -cam.transform.position.z));
        Vector2 clickWorldPos = new Vector2(wp3.x, wp3.y);

        Vector2 userPos = transform.position;
        Vector2 dir = clickWorldPos - userPos;
        if (dir.sqrMagnitude < 0.0001f)
        {
            // 点到自己脚下就不射，或给默认方向
            return;
        }
        dir.Normalize();

        // 先验证方向对不对（画一条线/打印）
        Debug.DrawLine(userPos, userPos + dir * 2f, Color.red, 0.2f);
        Debug.Log($"Click screen={clickScreenPos} world={clickWorldPos} dir={dir}");
        var ctx = new ItemUseContext
        {
            user = gameObject,
            aimWorldPos = clickWorldPos,
            aimDir = dir
        };

        bool consume = item.Effect.Apply(ctx);

        if (consume)
        {
            held.held = null; // ✅ 消耗品：用完手上清空
        }
    }
}

using UnityEngine;
using Game.Systems.Items;

public class HeldItemClickUse : MonoBehaviour
{
    private PlayerInputReader input;
    private Game.Gameplay.Player.HeldItem held;

    private float nextFireTime; // ✅ 射速节流：下一次允许开火的时间

    void Awake()
    {
        held = GetComponent<Game.Gameplay.Player.HeldItem>();
    }

    void Update()
    {
        // 延迟获取 input（避免启动顺序问题）
        if (input == null)
        {
            if (GameRoot.I != null) input = GameRoot.I.playerInput;
            if (input == null) return;
        }

        if (held == null) return;

        var item = held.held;
        if (item == null) return;

        // 鼠标世界方向（你之前已经验证完美运行的那套）
        if (!TryGetAim(out var aimWorldPos, out var aimDir))
            return;

        // ====== 1) 如果是武器：走射速/连发 ======
        if (item is WeaponDefinition weapon)
        {
            bool wantsShoot =
                weapon.fireMode == WeaponFireMode.Auto
                    ? input.ClickHeld
                    : input.ClickDown; // SemiAuto：只认按下这一帧

            if (!wantsShoot) return;

            // SemiAuto：消耗 ClickDown（避免同一帧多次触发）
            if (weapon.fireMode == WeaponFireMode.SemiAuto)
            {
                // 你也可以不用 Consume，只要 ClickDown 就不会重复
                // 但这里保持你“Consume”的风格一致
                if (!input.ConsumeClickDown(out _)) return;
            }

            // Auto：射速控制
            if (weapon.fireMode == WeaponFireMode.Auto)
            {
                if (Time.time < nextFireTime) return;

                // fireRate <= 0 视为不允许连发（也可当作特殊枪）
                float rate = Mathf.Max(0.01f, weapon.fireRate);
                nextFireTime = Time.time + (1f / rate);
            }

            // 调用 Effect（注意：这里必须是 WeaponShootEffect）
            if (weapon.Effect == null)
            {
                Debug.LogWarning($"Weapon {weapon.DisplayName} 没有绑定 Effect");
                return;
            }

            var ctx = new ItemUseContext
            {
                user = gameObject,
                item = weapon,
                aimWorldPos = aimWorldPos,
                aimDir = aimDir
            };

            weapon.Effect.Apply(ctx);
            return;
        }

        // ====== 2) 非武器：还是“点一下使用一次” ======
        if (!input.ConsumeClickDown(out _)) return;

        if (item.Effect == null)
        {
            Debug.LogWarning($"Click: 手持物品 {item.DisplayName} 没有绑定 Effect，无法使用");
            return;
        }

        var ctx2 = new ItemUseContext
        {
            user = gameObject,
            item = item,
            aimWorldPos = aimWorldPos,
            aimDir = aimDir
        };

        bool consume = item.Effect.Apply(ctx2);

        if (consume)
        {
            held.held = null;
        }
    }

    private bool TryGetAim(out Vector2 aimWorldPos, out Vector2 aimDir)
    {
        aimWorldPos = default;
        aimDir = Vector2.right;

        var cam = Camera.main;
        if (cam == null) return false;

        Vector2 screen = input.PointerPos;
        Vector3 wp3 = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
        aimWorldPos = new Vector2(wp3.x, wp3.y);

        Vector2 origin = transform.position;
        Vector2 dir = aimWorldPos - origin;

        if (dir.sqrMagnitude < 0.0001f) return false;
        aimDir = dir.normalized;
        return true;
    }
}

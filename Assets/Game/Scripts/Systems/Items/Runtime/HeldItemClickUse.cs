using UnityEngine;
using Game.Systems.Items;

public class HeldItemClickUse : MonoBehaviour
{
    private PlayerInputReader input;
    private Game.Gameplay.Player.HeldItem held;

    private float nextFireTime; // 射速节流：下一次允许开火的时间

    // ✅ 暂停期间如果鼠标按着，恢复后不允许立刻开火，必须先松开
    private bool blockUntilClickReleased;

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

        // ✅ 世界暂停：清掉/屏蔽世界侧“鼠标使用”
        if (GameRoot.I != null && GameRoot.I.Pause != null && GameRoot.I.Pause.IsPaused)
        {
            // 1) 如果这帧有 ClickDown，把它消费掉，避免恢复时补开火
            input.ConsumeClickDown(out _);

            // 2) 如果鼠标一直按着，记录下来：恢复后必须先松开
            if (input.ClickHeld)
                blockUntilClickReleased = true;

            return;
        }

        // ✅ 刚从暂停恢复：如果暂停期间鼠标按着，则必须先松开
        if (blockUntilClickReleased)
        {
            // 只要还按着，就一直不允许开火/使用
            if (input.ClickHeld)
            {
                // 顺手吞掉 ClickDown（例如恢复那帧刚好又被判定一次）
                input.ConsumeClickDown(out _);
                return;
            }
            // 松开了，解除封锁
            blockUntilClickReleased = false;
        }

        if (held == null) return;

        var item = held.held;
        if (item == null) return;

        // 鼠标世界方向
        if (!TryGetAim(out var aimWorldPos, out var aimDir))
            return;

        // ====== 1) 武器：射速/连发 ======
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
                if (!input.ConsumeClickDown(out _)) return;
            }

            // Auto：射速控制（注意：Time.time 在暂停期间不走；这里没问题）
            if (weapon.fireMode == WeaponFireMode.Auto)
            {
                if (Time.time < nextFireTime) return;

                float rate = Mathf.Max(0.01f, weapon.fireRate);
                nextFireTime = Time.time + (1f / rate);
            }

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

        // ====== 2) 非武器：点一下使用一次 ======
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

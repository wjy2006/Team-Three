using UnityEngine;
using Game.Systems.Items;

public class HeldItemClickUse : MonoBehaviour
{
    private PlayerInputReader input;
    private Game.Gameplay.Player.HeldItem held;

    [Header("Audio")]
    public AudioSource audioSource; // ✅ 拖入玩家身上的 AudioSource

    private float nextFireTime;
    private bool blockUntilClickReleased;

    [Header("Recoil Refs")]
    [SerializeField] private Game.Gameplay.Player.HeldItemVisualController visualCtrl;
    [SerializeField] private Game.Gameplay.Combat.KnockbackReceiver kbReceiver;

    // --- 2. 在 Awake 中添加自动获取 ---
    void Awake()
    {
        held = GetComponent<Game.Gameplay.Player.HeldItem>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        // 自动寻找组件
        if (visualCtrl == null) visualCtrl = GetComponent<Game.Gameplay.Player.HeldItemVisualController>();
        if (kbReceiver == null) kbReceiver = GetComponent<Game.Gameplay.Combat.KnockbackReceiver>();
    }

    // ✅ 音效播放助手
    private void PlayUseSfx(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    void Update()
    {
        if (input == null)
        {
            if (GameRoot.I != null) input = GameRoot.I.playerInput;
            if (input == null) return;
        }

        if (GameRoot.I != null && GameRoot.I.Pause != null && GameRoot.I.Pause.IsPaused)
        {
            input.ConsumeClickDown(out _);
            if (input.ClickHeld) blockUntilClickReleased = true;
            return;
        }

        if (blockUntilClickReleased)
        {
            if (input.ClickHeld)
            {
                input.ConsumeClickDown(out _);
                return;
            }
            blockUntilClickReleased = false;
        }

        if (held == null) return;
        var item = held.held;
        if (item == null) return;

        if (!TryGetAim(out var aimWorldPos, out var aimDir)) return;

        // ====== 1) 武器逻辑 ======
        if (item is WeaponDefinition weapon)
        {
            bool justPressedThisFrame = input.ClickDown;
            if (justPressedThisFrame) input.ConsumeClickDown(out _);

            bool wantsShoot = weapon.fireMode == WeaponFireMode.Auto ? input.ClickHeld : justPressedThisFrame;
            if (!wantsShoot) return;

            if (weapon.fireMode == WeaponFireMode.Auto)
            {
                if (Time.time < nextFireTime) return;
                float rate = Mathf.Max(0.01f, weapon.fireRate);
                nextFireTime = Time.time + (1f / rate);
            }

            if (weapon.Effect == null) return;

            // --- ✨ 新增：触发后坐力效果 ---
            // 视觉：枪往后缩
            if (visualCtrl != null) visualCtrl.ApplyVisualRecoil(weapon.visualRecoilStrength);

            // 物理：玩家往后退
            if (kbReceiver != null && weapon.physicalRecoilForce > 0)
            {
                kbReceiver.ApplyKnockback(-aimDir, weapon.physicalRecoilForce);
            }
            // ----------------------------

            var ctx = new ItemUseContext { user = gameObject, item = weapon, aimWorldPos = aimWorldPos, aimDir = aimDir };
            PlayUseSfx(weapon.UseSfx);
            weapon.Effect.Apply(ctx);

            if (justPressedThisFrame && GameRoot.I != null && GameRoot.I.Triggers != null)
            {
                GameRoot.I.Triggers.Raise(new HeldItemUsedEvent(item: weapon));
            }
            return;
        }
        // ====== 2) 非武器逻辑 ======
        if (!input.ConsumeClickDown(out _)) return;

        if (item.Effect == null) return;

        var ctx2 = new ItemUseContext { user = gameObject, item = item, aimWorldPos = aimWorldPos, aimDir = aimDir };

        bool applySuccess = item.Effect.Apply(ctx2);
        if (applySuccess)
        {
            // ✅ 物品使用成功后播放音效
            PlayUseSfx(item.UseSfx);

            if (GameRoot.I != null && GameRoot.I.Triggers != null)
            {
                GameRoot.I.Triggers.Raise(new HeldItemUsedEvent(item: item));
            }

            // 如果是消耗品则清空
            held.held = null;
            GameRoot.I.vis.RefreshNow();
        }
    }

    private bool TryGetAim(out Vector2 aimWorldPos, out Vector2 aimDir)
    {
        aimWorldPos = default; aimDir = Vector2.right;
        var cam = Camera.main;
        if (cam == null) return false;

        Vector2 screen = input.PointerPos;
        Vector3 wp3 = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
        aimWorldPos = (Vector2)wp3;

        Vector2 origin = transform.position;
        Vector2 dir = aimWorldPos - origin;

        if (dir.sqrMagnitude < 0.0001f) return false;
        aimDir = dir.normalized;
        return true;
    }
}
using UnityEngine;
using Game.Systems.Items;

public class HeldItemClickUse : MonoBehaviour
{
    private PlayerInputReader input;
    private Game.Gameplay.Player.HeldItem held;

    [Header("Audio")]
    public AudioSource audioSource;

    private float nextFireTime;
    private bool blockUntilClickReleased;

    [Header("Recoil Refs")]
    [SerializeField] private Game.Gameplay.Player.HeldItemVisualController visualCtrl;
    [SerializeField] private Game.Gameplay.Combat.KnockbackReceiver kbReceiver;

    private void Awake()
    {
        held = GetComponent<Game.Gameplay.Player.HeldItem>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (visualCtrl == null) visualCtrl = GetComponent<Game.Gameplay.Player.HeldItemVisualController>();
        if (kbReceiver == null) kbReceiver = GetComponent<Game.Gameplay.Combat.KnockbackReceiver>();
    }

    private void PlayUseSfx(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    private void Update()
    {
        if (input == null)
        {
            if (GameRoot.I != null) input = GameRoot.I.playerInput;
            if (input == null) return;
        }

        if (GameRoot.I != null && GameRoot.I.Pause != null && GameRoot.I.Pause.IsPaused)
        {
            bool menuOpen = Game.UI.Menu.FixedMenuController.Instance != null &&
                            Game.UI.Menu.FixedMenuController.Instance.menuPanel != null &&
                            Game.UI.Menu.FixedMenuController.Instance.menuPanel.activeInHierarchy;
            bool shopOpen = Game.UI.Shop.ShopController.Instance != null &&
                            Game.UI.Shop.ShopController.Instance.isActiveAndEnabled;

            if (menuOpen || shopOpen)
            {
                if (input.ClickHeld) blockUntilClickReleased = true;
            }
            else
            {
                input.ConsumeClickDown(out _);
                if (input.ClickHeld) blockUntilClickReleased = true;
            }
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

        // 1) Weapon
        if (item is WeaponDefinition weapon)
        {
            bool justPressedThisFrame = input.ClickDown;
            if (justPressedThisFrame) input.ConsumeClickDown(out _);

            bool wantsShoot = weapon.fireMode == WeaponFireMode.Auto ? input.ClickHeld : justPressedThisFrame;
            if (!wantsShoot) return;

            if (weapon.fireRate > 0f)
            {
                if (Time.time < nextFireTime) return;
                nextFireTime = Time.time + (1f / weapon.fireRate);
            }

            if (weapon.Effect == null) return;

            if (visualCtrl != null) visualCtrl.ApplyVisualRecoil(weapon.visualRecoilStrength);
            if (kbReceiver != null && weapon.physicalRecoilForce > 0f)
                kbReceiver.ApplyKnockback(-aimDir, weapon.physicalRecoilForce);

            var ctx = new ItemUseContext
            {
                user = gameObject,
                item = weapon,
                aimWorldPos = aimWorldPos,
                aimDir = aimDir
            };

            PlayUseSfx(weapon.UseSfx);
            weapon.Effect.Apply(ctx);

            if (justPressedThisFrame && GameRoot.I != null && GameRoot.I.Triggers != null)
                GameRoot.I.Triggers.Raise(new HeldItemUsedEvent(item: weapon));

            return;
        }

        // 2) Non-weapon
        if (!input.ConsumeClickDown(out _)) return;

        bool insertedByClick = GemSocketInteractTrigger.TryInsertAnyInRangeForHeldGem(item);
        if (insertedByClick)
        {
            PlayUseSfx(item.UseSfx);

            if (GameRoot.I != null && GameRoot.I.Triggers != null)
                GameRoot.I.Triggers.Raise(new HeldItemUsedEvent(item: item));

            if (GameRoot.I != null && GameRoot.I.vis != null)
                GameRoot.I.vis.RefreshNow();

            return;
        }

        if (item.Type == ItemType.Key)
        {
            bool openedByClick = DoorInteractTrigger.TryOpenAnyInRangeForHeldKey(item);
            if (openedByClick)
            {
                PlayUseSfx(item.UseSfx);

                if (GameRoot.I != null && GameRoot.I.Triggers != null)
                    GameRoot.I.Triggers.Raise(new HeldItemUsedEvent(item: item));

                return;
            }
        }

        if (item.Effect == null) return;

        var ctx2 = new ItemUseContext
        {
            user = gameObject,
            item = item,
            aimWorldPos = aimWorldPos,
            aimDir = aimDir
        };

        bool applySuccess = item.Effect.Apply(ctx2);
        if (!applySuccess) return;

        PlayUseSfx(item.UseSfx);

        if (GameRoot.I != null && GameRoot.I.Triggers != null)
            GameRoot.I.Triggers.Raise(new HeldItemUsedEvent(item: item));

        held.held = null;
        if (GameRoot.I != null && GameRoot.I.vis != null)
            GameRoot.I.vis.RefreshNow();
    }

    private bool TryGetAim(out Vector2 aimWorldPos, out Vector2 aimDir)
    {
        aimWorldPos = default;
        aimDir = Vector2.right;

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

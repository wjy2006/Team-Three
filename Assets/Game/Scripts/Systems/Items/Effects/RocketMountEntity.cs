using UnityEngine;
using Game.Systems.Items;
using Game.Gameplay.Combat;
using Game.Gameplay.Player;
using Game.Core;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Health2D))]
public class RocketMountEntity : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    private Health2D health;

    private RocketRideEffect cfg;
    private RocketRideController controller;

    private GameObject playerGO;
    private Rigidbody2D playerRB;
    private PlayerStats playerStats;
    private ItemDefinition sourceItem;

    private string instanceId;
    private Vector2 aimDir = Vector2.right;
    private bool accelHeld;

    private readonly Collider2D[] overlap = new Collider2D[32];
    private ContactFilter2D filter;

    private bool exploded;
    private Vector2 prevV;

    private bool isBoundToPlayer = false;
    private bool bindRequested = false;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        health = GetComponent<Health2D>();
    }

    public void Attach(
        GameObject playerGO,
        PlayerStats playerStats,
        RocketRideController controller,
        RocketRideEffect effect,
        ItemDefinition sourceItem,
        string instanceId,
        int startHp)
    {
        this.playerGO = playerGO;
        this.playerStats = playerStats;
        this.controller = controller;
        this.cfg = effect;
        this.sourceItem = sourceItem;
        this.instanceId = instanceId;

        if (playerGO != null) playerRB = playerGO.GetComponent<Rigidbody2D>();

        health.maxHp = cfg.rocketHp;
        health.hp = Mathf.Clamp(startHp, 0, cfg.rocketHp);

        health.OnDamaged -= OnDamaged;
        health.OnDamaged += OnDamaged;

        filter = new ContactFilter2D();
        filter.SetLayerMask(cfg.explosionLayer);
        filter.useTriggers = true;

        // 初始对齐：火箭找玩家
        SnapToPlayer();

        // 默认不接管
        isBoundToPlayer = false;
        bindRequested = false;
    }

    public int GetCurrentHp() => health != null ? Mathf.CeilToInt(health.hp) : 0;

    public void SetInput(Vector2 aimDir, bool accelHeld)
    {
        if (aimDir.sqrMagnitude > 0.0001f) this.aimDir = aimDir.normalized;
        this.accelHeld = accelHeld;
    }

    // ✅ 转场 SpawnTo 结束后由 Controller 转发
    public void OnPostSpawn()
    {
        if (exploded || cfg == null) return;

        SnapToPlayer();
        bindRequested = true; // 申请接管（会等转场结束）
    }

    /// <summary>
    /// ✅ 切换火箭用：立即绑定（如果不在转场中）
    /// </summary>
    public void BindNowIfSafe()
    {
        if (exploded || cfg == null) return;

        SnapToPlayer();
        bindRequested = true;

        // 不在转场中则立刻激活
        if (GameRoot.I == null || !GameRoot.I.IsTransitioning)
            ActivateBinding();
    }

    public void SnapToPlayer()
    {
        if (playerGO == null) return;
        Vector2 p = playerGO.transform.position;

        rb.position = p;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        transform.position = p;
    }

    private void FixedUpdate()
    {
        if (exploded || cfg == null) return;

        prevV = rb.linearVelocity;

        // 1) 转向
        float targetDeg = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
        float maxStep = cfg.maxTurnDegPerSec * Time.fixedDeltaTime;
        float newDeg = Mathf.MoveTowardsAngle(rb.rotation, targetDeg, maxStep);
        rb.MoveRotation(newDeg);

        // 2) 加速
        if (accelHeld)
        {
            float rad = rb.rotation * Mathf.Deg2Rad;
            Vector2 forward = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            rb.linearVelocity += forward * (cfg.accel * Time.fixedDeltaTime);

            if (rb.linearVelocity.magnitude > cfg.maxSpeed)
                rb.linearVelocity = rb.linearVelocity.normalized * cfg.maxSpeed;
        }

        // ✅ 转场期间不允许拖玩家（否则 SpawnTo 会被拉回旧场景）
        if (GameRoot.I != null && GameRoot.I.IsTransitioning)
            return;

        // ✅ 收到请求后，在转场结束的第一个物理帧激活
        if (bindRequested && !isBoundToPlayer)
            ActivateBinding();

        // ✅ 拖着玩家走（MovePosition 保证 Trigger）
        if (isBoundToPlayer && playerRB != null && playerGO != null)
        {
            playerRB.MovePosition(rb.position);
            playerGO.transform.position = (Vector3)rb.position + new Vector3(0, 0, playerGO.transform.position.z);
        }
    }

    private void ActivateBinding()
    {
        isBoundToPlayer = true;
        bindRequested = false;

        if (playerRB != null)
        {
            playerRB.bodyType = RigidbodyType2D.Kinematic;
            playerRB.linearVelocity = Vector2.zero;
            playerRB.angularVelocity = 0f;

            playerRB.position = rb.position;
            Physics2D.SyncTransforms();
        }
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (exploded || cfg == null || health == null) return;

        Vector2 dv = rb.linearVelocity - prevV;
        float dvMag = dv.magnitude;
        if (dvMag < cfg.dvThreshold) return;

        float dmg = (dvMag - cfg.dvThreshold) * cfg.damagePerDv;
        if (cfg.maxImpactDamage > 0f) dmg = Mathf.Min(dmg, cfg.maxImpactDamage);

        health.TakeDamage(new DamageInfo
        {
            amount = dmg,
            source = gameObject,
            hitPoint = col.GetContact(0).point,
            direction = dv.normalized,
            kind = "rocket_impact"
        });
    }

    private void OnDamaged(DamageInfo info)
    {
        if (exploded) return;
        if (health.hp <= 0f) Explode();
    }

    private void Explode()
    {
        if (exploded) return;
        exploded = true;

        Vector2 center = rb.position;

        if (cfg.explosionVfxPrefab != null)
        {
            var vfx = Instantiate(cfg.explosionVfxPrefab, center, Quaternion.identity);
            Destroy(vfx, cfg.explosionVfxLife);
        }

        int count = Physics2D.OverlapCircle(center, cfg.explosionRadius, filter, overlap);
        for (int i = 0; i < count; i++)
        {
            if (overlap[i].TryGetComponent<IDamageable>(out var target))
            {
                if (playerGO != null && overlap[i].gameObject == playerGO) continue;
                target.TakeDamage(new DamageInfo { amount = cfg.damageToOthers, source = gameObject });
            }
        }

        if (playerStats != null)
            playerStats.TakeDamage(new DamageInfo { amount = cfg.damageToPlayer, source = gameObject });

        if (controller != null)
            controller.OnRocketFinished(sourceItem, instanceId, consumeHeldItem: true);

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (health != null) health.OnDamaged -= OnDamaged;

        // ✅ 关键：切换火箭时不要把玩家刚体还原成 Dynamic（否则会出现“脱跟空窗期”）
        bool restoringBecauseSwitch = (controller != null && controller.IsSwitchingRockets());

        if (!restoringBecauseSwitch && playerRB != null)
        {
            playerRB.bodyType = RigidbodyType2D.Dynamic;
            playerRB.linearVelocity = Vector2.zero;
            playerRB.angularVelocity = 0f;
        }

        if (!exploded && controller != null)
            controller.OnRocketFinished(sourceItem, instanceId, consumeHeldItem: false);
    }
}
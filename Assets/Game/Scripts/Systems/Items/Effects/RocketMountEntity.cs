using UnityEngine;
using Game.Systems.Items;
using Game.Gameplay.Combat;
using Game.Gameplay.Player;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Health2D))]
public class RocketMountEntity : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;

    private RocketRideEffect cfg;
    private RocketRideController controller;

    private GameObject playerGO;
    private PlayerStats playerStats;
    private ItemDefinition sourceItem;

    private Vector2 aimDir = Vector2.right;
    private bool accelHeld;

    private Health2D health;

    private readonly Collider2D[] overlap = new Collider2D[32];
    private ContactFilter2D filter;

    private bool exploded;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        health = GetComponent<Health2D>();
    }

    public void Attach(GameObject playerGO, PlayerStats playerStats, RocketRideController controller,
        RocketRideEffect effect, ItemDefinition sourceItem)
    {
        this.playerGO = playerGO;
        this.playerStats = playerStats;
        this.controller = controller;
        this.cfg = effect;
        this.sourceItem = sourceItem;

        // ✅ 火箭 200HP（并满足 IHealthView 给 HpBar 读）
        health.maxHp = cfg.rocketHp;
        health.hp = cfg.rocketHp;

        health.OnDamaged -= OnDamaged;
        health.OnDamaged += OnDamaged;

        filter = new ContactFilter2D();
        filter.SetLayerMask(cfg.explosionLayer);
        filter.useTriggers = true;
    }

    private void OnDestroy()
    {
        if (health != null) health.OnDamaged -= OnDamaged;

        // 防止外部 Destroy 导致玩家一直被锁/隐藏
        if (!exploded && controller != null)
            controller.OnRocketFinished(sourceItem, consumeHeldItem: false);
    }

    private void OnDamaged(DamageInfo info)
    {
        if (exploded) return;
        if (health.hp <= 0f) Explode();
    }

    public void SetInput(Vector2 aimDir, bool accelHeld)
    {
        if (aimDir.sqrMagnitude > 0.0001f) this.aimDir = aimDir.normalized;
        this.accelHeld = accelHeld;
    }

    // ✅ 场景切换后由控制器调用：把火箭“对齐到玩家最终出生点”，并清速度
    public void SnapToPlayer()
    {
        if (playerGO == null) return;

        Vector2 p = playerGO.transform.position;
        rb.position = p;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        // 角度你要不要保留都行，这里保留当前
        transform.position = p;
    }

    private void FixedUpdate()
    {
        if (exploded || cfg == null) return;

        // 1) 限制旋转速度
        float targetDeg = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
        float maxStep = cfg.maxTurnDegPerSec * Time.fixedDeltaTime;
        float newDeg = Mathf.MoveTowardsAngle(rb.rotation, targetDeg, maxStep);
        rb.MoveRotation(newDeg);

        // 2) 按住左键才加速
        if (accelHeld)
        {
            float rad = newDeg * Mathf.Deg2Rad;
            Vector2 forward = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            rb.linearVelocity += forward * (cfg.accel * Time.fixedDeltaTime);

            float spd = rb.linearVelocity.magnitude;
            if (spd > cfg.maxSpeed)
                rb.linearVelocity = rb.linearVelocity.normalized * cfg.maxSpeed;
        }

        // 3) ✅ 玩家跟火箭（你指定的）
        if (playerGO != null)
            playerGO.transform.position = rb.position;
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (exploded || cfg == null) return;
        if (rb.linearVelocity.magnitude < cfg.explodeSpeedThreshold) return;
        Explode();
    }

    private void Explode()
    {
        if (exploded) return;
        exploded = true;

        Vector2 center = rb.position;

        // VFX 可调寿命
        if (cfg.explosionVfxPrefab != null)
        {
            var vfx = Instantiate(cfg.explosionVfxPrefab, center, Quaternion.identity);
            var b = vfx.GetComponent<Bullet2D>();
            if (b != null) b.explodeVfxLife = cfg.explosionVfxLife;
            Destroy(vfx, Mathf.Max(0.05f, cfg.explosionVfxLife));
        }

        // AOE：只伤害 IDamageable（敌人等）；玩家不需要 overlap 找，固定扣 19
        int count = Physics2D.OverlapCircle(center, cfg.explosionRadius, filter, overlap);
        for (int i = 0; i < count; i++)
        {
            var c = overlap[i];
            if (c == null) continue;
            if (c.attachedRigidbody == rb) continue;

            if (!c.TryGetComponent<IDamageable>(out var dmgTarget)) continue;

            // 跳过玩家（玩家固定扣 19）
            if (playerGO != null && c.gameObject == playerGO) continue;

            dmgTarget.TakeDamage(new DamageInfo
            {
                amount = cfg.damageToOthers,
                source = gameObject,
                hitPoint = center,
                direction = Vector2.zero,
                knockbackForce = 0f,
                knockbackKind = KnockbackKind.Explosion,
                kind = "rocket_explosion"
            });
        }

        // 玩家固定扣 19
        if (playerStats != null)
        {
            playerStats.TakeDamage(new DamageInfo
            {
                amount = cfg.damageToPlayer,
                source = gameObject,
                hitPoint = center,
                direction = Vector2.zero,
                knockbackForce = 0f,
                knockbackKind = KnockbackKind.Explosion,
                kind = "rocket_self"
            });
        }

        // 通知：结束骑乘 + 从手上消失
        if (controller != null)
            controller.OnRocketFinished(sourceItem, consumeHeldItem: true);

        Destroy(gameObject);
    }
}

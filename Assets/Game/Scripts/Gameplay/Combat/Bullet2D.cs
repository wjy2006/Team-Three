using UnityEngine;

namespace Game.Gameplay.Combat
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Bullet2D : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("开启后，会在控制台打印子弹撞到了什么")]
        public bool enableDebugLog = true;

        [Tooltip("子弹能击中哪些层？建议排除 Player 层")]
        public LayerMask hitLayer;

        [Header("Runtime Ref")]
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private Collider2D col;

        [Header("Explosion VFX")]
        public GameObject explodeVfxPrefab;
        public float explodeVfxLife = 1.0f; // 自动销毁特效

        // ===== Audio SFX =====
        [Header("Audio SFX")]
        public AudioClip explodeSfx; // 爆炸/击中音效

        [Header("Explosion Damage")]
        public bool explodeOnHit = false;          // 普通子弹可以关，导弹打开
        public float explosionRadius = 2.0f;
        public float explosionDamage = 6f;
        public bool explosionIgnoresTriggers = true;

        [Header("Knockback")]
        public float hitKnockbackForce = 4f;         // A：直击击退
        public float explosionKnockbackForce = 8f;    // B：爆炸击退

        [Header("Anti-Clip")]
        public bool explodeIfStuckInside = true;

        [Header("Raycast Gate")]
        [Tooltip("Trigger 模式下，速度大于该阈值才使用 Cast 预判；否则完全靠触发来爆")]
        public float raycastSpeedThreshold = 20f;

        // 复用 Overlap 数组，避免GC
        private readonly Collider2D[] overlap = new Collider2D[32];

        private GameObject owner;
        private float damage;
        private float lifeTime;
        private float spawnTime;

        private Vector2 lastPos;
        private bool useTriggerMode;
        private float armUntil;

        private Vector2 savedVelocity;
        private bool frozen;

        // Cast 复用数组
        private readonly RaycastHit2D[] hits = new RaycastHit2D[8];
        private ContactFilter2D filter;

        private void Awake()
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (col == null) col = GetComponent<Collider2D>();

            useTriggerMode = col.isTrigger;

            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            filter = new ContactFilter2D { useTriggers = true };
            if (hitLayer.value != 0)
            {
                filter.useLayerMask = true;
                filter.layerMask = hitLayer;
            }
            else
            {
                filter.useLayerMask = false;
                if (enableDebugLog) Debug.LogWarning($"[Bullet2D] {name} 未设置 Hit Layer，容易误爆！");
            }
        }

        public void Init(GameObject owner, Vector2 dir, float speed, float damage, float lifeTime)
        {
            this.owner = owner;
            this.damage = damage;
            this.lifeTime = lifeTime;

            spawnTime = Time.time;
            armUntil = Time.time + 0.05f;

            dir = dir.sqrMagnitude < 0.0001f ? Vector2.right : dir.normalized;
            rb.linearVelocity = dir * speed;

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            lastPos = rb.position;
        }

        private void Update()
        {
            if (GameRoot.I != null && GameRoot.I.Pause != null && GameRoot.I.Pause.IsPaused)
            {
                if (!frozen && rb != null)
                {
                    savedVelocity = rb.linearVelocity;
                    rb.linearVelocity = Vector2.zero;
                    rb.simulated = false;
                    frozen = true;
                }
                return;
            }
            else if (frozen && rb != null)
            {
                rb.simulated = true;
                rb.linearVelocity = savedVelocity;
                frozen = false;
            }

            if (lifeTime > 0 && Time.time - spawnTime >= lifeTime)
                Destroy(gameObject);
        }

        private void FixedUpdate()
        {
            if (!useTriggerMode) return;

            // ✅ 严格按你原来的版本：先检查 stuck，stuck 就立刻爆并 Destroy
            if (explodeIfStuckInside && CheckStuckAndExplode()) return;

            // ✅ 速度阈值：只有高速才用 Cast 预判；低速完全靠 OnTriggerEnter2D
            float vSqr = rb != null ? rb.linearVelocity.sqrMagnitude : 0f;
            float thresholdSqr = raycastSpeedThreshold * raycastSpeedThreshold;

            if (vSqr <= thresholdSqr)
            {
                lastPos = rb.position;
                return;
            }

            // ===== 高速 Cast 预判（保留你原逻辑结构）=====
            Vector2 currentPos = rb.position;
            Vector2 delta = currentPos - lastPos;
            float dist = delta.magnitude;

            if (dist > 0.00001f)
            {
                Vector2 dir = delta / dist;
                int count = col.Cast(dir, filter, hits, dist);

                if (count > 0)
                {
                    int best = -1;
                    float bestDist = float.MaxValue;

                    for (int i = 0; i < count; i++)
                    {
                        var h = hits[i];
                        if (h.collider == null) continue;
                        if (Time.time < armUntil && owner != null && h.collider.transform.root == owner.transform.root) continue;
                        if (hitLayer.value != 0 && ((1 << h.collider.gameObject.layer) & hitLayer.value) == 0) continue;

                        if (h.distance < bestDist)
                        {
                            bestDist = h.distance;
                            best = i;
                        }
                    }

                    if (best != -1)
                    {
                        var hit = hits[best];
                        rb.position = hit.point;
                        HandleHit(hit.collider, hit.point);
                        return;
                    }
                }
            }

            lastPos = currentPos;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!useTriggerMode) return;
            if (Time.time < armUntil && owner != null && other.transform.root == owner.transform.root) return;
            if (hitLayer.value != 0 && ((1 << other.gameObject.layer) & hitLayer.value) == 0) return;

            // ✅ 低速：碰到就爆（不预判/不回退）；落点用“当前最近点”
            Vector2 p = rb != null ? rb.position : (Vector2)transform.position;
            Vector2 hitPoint = other.ClosestPoint(p);

            HandleHit(other, hitPoint);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (useTriggerMode) return;

            var other = collision.collider;
            if (other == null) return;
            if (Time.time < armUntil && owner != null && other.transform.root == owner.transform.root) return;
            if (hitLayer.value != 0 && ((1 << other.gameObject.layer) & hitLayer.value) == 0) return;

            // ✅ Collision 模式：只有 damageable 才爆炸！！！
            if (other.TryGetComponent<IDamageable>(out var damageable))
            {
                Vector2 hitPoint = collision.contactCount > 0
                    ? collision.GetContact(0).point
                    : other.ClosestPoint(rb != null ? rb.position : (Vector2)transform.position);

                Vector2 dir = rb != null && rb.linearVelocity.sqrMagnitude > 0.0001f ? rb.linearVelocity.normalized : Vector2.zero;
                var info = MakeDamageInfo(damage, hitPoint, dir, "bullet", hitKnockbackForce, KnockbackKind.Hit);

                damageable.TakeDamage(info);
                Explode(hitPoint);
                Destroy(gameObject);
                return;
            }

            // 非 damageable：不爆炸、不销毁
        }

        private void HandleHit(Collider2D other, Vector2 hitPoint)
        {
            if (other.TryGetComponent<IDamageable>(out var damageable))
            {
                Vector2 dir = rb != null && rb.linearVelocity.sqrMagnitude > 0.0001f ? rb.linearVelocity.normalized : Vector2.zero;
                var info = MakeDamageInfo(damage, hitPoint, dir, "bullet", hitKnockbackForce, KnockbackKind.Hit);

                damageable.TakeDamage(info);
                Explode(hitPoint);
                Destroy(gameObject);
                return;
            }

            if (other.isTrigger) return;

            Explode(hitPoint);
            Destroy(gameObject);
        }

        private void SpawnExplosionVfx(Vector2 hitPoint)
        {
            // ✅ 播放音效
            if (GameRoot.I != null && GameRoot.I.globalSfxSource != null && explodeSfx != null)
            {
                GameRoot.I.globalSfxSource.PlayOneShot(explodeSfx);
            }

            if (explodeVfxPrefab == null) return;

            var vfx = Instantiate(explodeVfxPrefab, hitPoint, Quaternion.identity);
            if (explodeVfxLife > 0f)
                Destroy(vfx, explodeVfxLife);
        }

        private void Explode(Vector2 center)
        {
            // 1) VFX + 音效
            SpawnExplosionVfx(center);

            // 2) AOE 伤害
            if (!explodeOnHit) return;

            int count = Physics2D.OverlapCircle(center, explosionRadius, filter, overlap);

            for (int i = 0; i < count; i++)
            {
                var c = overlap[i];
                if (c == null) continue;
                if (explosionIgnoresTriggers && c.isTrigger) continue;

                if (c.TryGetComponent<IDamageable>(out var dmg))
                {
                    Vector2 toTarget = (Vector2)c.transform.position - center;
                    float dist = toTarget.magnitude;
                    Vector2 dir = dist < 0.0001f ? Vector2.up : (toTarget / dist);
                    float t = Mathf.Clamp01(dist / explosionRadius);
                    float falloff = 1f - 0.5f * t;
                    float kb = explosionKnockbackForce * falloff;

                    var info = MakeDamageInfo(explosionDamage, center, dir, "explosion", kb, KnockbackKind.Explosion);
                    dmg.TakeDamage(info);
                }
            }
        }

        private DamageInfo MakeDamageInfo(float amount, Vector2 hitPoint, Vector2 direction, string kind, float knockbackForce, KnockbackKind kbKind)
        {
            return new DamageInfo
            {
                amount = amount,
                source = owner,
                hitPoint = hitPoint,
                direction = direction,
                knockbackForce = knockbackForce,
                knockbackKind = kbKind,
                kind = kind
            };
        }

        private bool CheckStuckAndExplode()
        {
            if (!useTriggerMode) return false;
            if (Time.time < armUntil) return false;

            int count = col.Overlap(filter, overlap);
            if (count <= 0) return false;

            Collider2D best = null;
            for (int i = 0; i < count; i++)
            {
                var c = overlap[i];
                if (c == null) continue;
                if (owner != null && c.transform.root == owner.transform.root) continue;
                if (c.isTrigger) continue;
                best = c;
                break;
            }

            if (best == null) return false;

            Vector2 center = rb.position;
            Vector2 snapPoint = best.ClosestPoint(center);
            rb.position = snapPoint;

            if (enableDebugLog)
                Debug.Log($"[Bullet2D] {name} stuck inside {best.name}, explode immediately.");

            Explode(snapPoint);
            Destroy(gameObject);
            return true;
        }
    }
}
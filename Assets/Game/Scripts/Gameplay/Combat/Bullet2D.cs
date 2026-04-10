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
        public float explodeVfxLife = 1.0f;

        [Header("Audio SFX")]
        public AudioClip explodeSfx;
        public AudioClip bounceSfx; // 弹墙音效，打到 Damageable 不播

        [Header("Explosion Damage")]
        public bool explodeOnHit = false;
        public float explosionRadius = 2.0f;
        public float explosionDamage = 6f;
        public bool explosionIgnoresTriggers = true;

        [Header("Knockback")]
        public float hitKnockbackForce = 4f;
        public float explosionKnockbackForce = 8f;


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

        // 复用数组
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
                        if (Time.time < armUntil && IsOwnerCollider(h.collider)) continue;
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

                        // ✅ 唯一修改：只有打到 Damageable 才 snap 位置
                        // 打墙时不 snap，慢速子弹不会视觉上"沾"在墙面
                        if (TryGetDamageable(hit.collider, out _))
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
            if (Time.time < armUntil && IsOwnerCollider(other)) return;
            if (hitLayer.value != 0 && ((1 << other.gameObject.layer) & hitLayer.value) == 0) return;

            HandleHit(other, transform.position);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (useTriggerMode) return;

            var other = collision.collider;
            if (other == null) return;
            if (Time.time < armUntil && IsOwnerCollider(other)) return;
            if (hitLayer.value != 0 && ((1 << other.gameObject.layer) & hitLayer.value) == 0) return;

            if (TryGetDamageable(other, out var damageable))
            {
                Vector2 hitPoint = collision.GetContact(0).point;
                Vector2 dir = rb != null && rb.linearVelocity.sqrMagnitude > 0.0001f ? rb.linearVelocity.normalized : Vector2.zero;

                var info = MakeDamageInfo(damage, hitPoint, dir, "bullet", hitKnockbackForce, KnockbackKind.Hit);
                damageable.TakeDamage(info);
                Explode(hitPoint);
                Destroy(gameObject);
                return;
            }

            if (other.isTrigger) return;
            if (GameRoot.I != null && GameRoot.I.globalSfxSource != null && bounceSfx != null)
                GameRoot.I.globalSfxSource.PlayOneShot(bounceSfx);
        }

        private void HandleHit(Collider2D other, Vector2 hitPoint)
        {
            if (TryGetDamageable(other, out var damageable))
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
            SpawnExplosionVfx(center);

            if (!explodeOnHit) return;

            int count = Physics2D.OverlapCircle(center, explosionRadius, filter, overlap);

            for (int i = 0; i < count; i++)
            {
                var c = overlap[i];
                if (c == null) continue;
                if (explosionIgnoresTriggers && c.isTrigger) continue;

                if (TryGetDamageable(c, out var dmg))
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
        private bool IsOwnerCollider(Collider2D c)
        {
            if (owner == null || c == null) return false;
            var ownerTr = owner.transform;
            var targetTr = c.transform;
            return targetTr == ownerTr || targetTr.IsChildOf(ownerTr);
        }

        private static bool TryGetDamageable(Collider2D collider, out IDamageable damageable)
        {
            damageable = null;
            if (collider == null) return false;

            if (collider.TryGetComponent<IDamageable>(out var self))
            {
                damageable = self;
                return true;
            }

            damageable = collider.GetComponentInParent<IDamageable>();
            return damageable != null;
        }
}
}

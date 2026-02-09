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
        private Vector2 savedVelocity;
        private bool frozen;



        private GameObject owner;
        private float damage;
        private float lifeTime;
        private float spawnTime;

        private Vector2 lastPos;
        private bool useTriggerMode;
        private float armUntil;

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

            // ✅ 优化 1: 使用 LayerMask，不再检测 Everything
            // 如果你在 Inspector 没设置 LayerMask (值为0)，为了防止无效，默认还是 Everything，但建议你去设置！
            filter = new ContactFilter2D();
            filter.useTriggers = true;
            if (hitLayer.value != 0)
            {
                filter.useLayerMask = true;
                filter.layerMask = hitLayer;
            }
            else
            {
                filter.useLayerMask = false; // 没设置层级时的回退方案
                if (enableDebugLog) Debug.LogWarning($"[Bullet2D] {name} 未设置 Hit Layer，正在检测所有层级，容易误爆！");
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
            // ✅ 暂停：冻结子弹速度（防止任何脚本/物理残留导致“还在动”）
            if (GameRoot.I != null && GameRoot.I.Pause != null && GameRoot.I.Pause.IsPaused)
            {
                if (!frozen && rb != null)
                {
                    savedVelocity = rb.linearVelocity;
                    rb.linearVelocity = Vector2.zero;
                    rb.simulated = false; // 彻底停物理
                    frozen = true;
                }
                return;
            }
            else if (frozen && rb != null)
            {
                // 恢复
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

            // 即使距离很短，也进行检测，防止贴脸穿模
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

                        if (Time.time < armUntil && owner != null && h.collider.transform.root == owner.transform.root)
                            continue;

                        // 额外的 LayerMask 双重检查 (Cast 有时会漏)
                        if (hitLayer.value != 0 && ((1 << h.collider.gameObject.layer) & hitLayer.value) == 0)
                            continue;

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

        // Trigger 作为一个保底
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!useTriggerMode) return;

            if (Time.time < armUntil && owner != null && other.transform.root == owner.transform.root) return;


            // 必须在检测层级内
            if (hitLayer.value != 0 && ((1 << other.gameObject.layer) & hitLayer.value) == 0) return;

            HandleHit(other, transform.position);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (useTriggerMode) return; // 只有非trigger子弹走这里（管理员的手枪）

            var other = collision.collider;
            if (other == null) return;

            // ✅ 出生保护：在很短时间内忽略“命中自己”，防止枪口在自己碰撞体内导致出生即自爆
            if (Time.time < armUntil && owner != null && other.transform.root == owner.transform.root)
                return;

            // LayerMask 检查（如果你设置了 hitLayer）
            if (hitLayer.value != 0 && ((1 << other.gameObject.layer) & hitLayer.value) == 0)
                return;

            // 命中可受伤对象：扣血 + 销毁（包括 player / 自己，只要过了 arm）
            if (other.TryGetComponent<IDamageable>(out var damageable))
            {
                Vector2 hitPoint = collision.GetContact(0).point;

                var info = new DamageInfo
                {
                    amount = damage,
                    source = owner,
                    hitPoint = hitPoint,
                    direction = rb != null && rb.linearVelocity.sqrMagnitude > 0.0001f ? rb.linearVelocity.normalized : Vector2.zero,
                    kind = "bullet"
                };

                damageable.TakeDamage(info);
                SpawnExplosionVfx(hitPoint);
                Destroy(gameObject);
                return;
            }

            // 撞到触发器：一般不处理（不过 collision 里通常不会是 trigger）
            if (other.isTrigger) return;
        }


        private void HandleHit(Collider2D other, Vector2 hitPoint)
        {
            // 命中可受伤目标
            if (other.TryGetComponent<IDamageable>(out var damageable))
            {

                var info = new DamageInfo
                {
                    amount = damage,
                    source = owner,
                    hitPoint = hitPoint,
                    direction = rb != null && rb.linearVelocity.sqrMagnitude > 0.0001f ? rb.linearVelocity.normalized : Vector2.zero,
                    kind = "bullet"
                };
                damageable.TakeDamage(info);
                Destroy(gameObject);
                return;
            }

            // ✅ 关键 Debug: 如果是 Trigger，我们忽略它（比如空气墙、敌人的检测范围）
            if (other.isTrigger)
            {
                // 如果你想要子弹穿过触发器，这里直接 return，不要销毁
                return;
            }
            SpawnExplosionVfx(hitPoint);
            Destroy(gameObject);
        }
        private void SpawnExplosionVfx(Vector2 hitPoint)
        {
            if (explodeVfxPrefab == null) return;

            var vfx = Instantiate(explodeVfxPrefab, hitPoint, Quaternion.identity);

            if (explodeVfxLife > 0f)
                Destroy(vfx, explodeVfxLife);
        }

    }
}
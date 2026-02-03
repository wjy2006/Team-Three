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
                if(enableDebugLog) Debug.LogWarning($"[Bullet2D] {name} 未设置 Hit Layer，正在检测所有层级，容易误爆！");
            }
        }

        public void Init(GameObject owner, Vector2 dir, float speed, float damage, float lifeTime)
        {
            this.owner = owner;
            this.damage = damage;
            this.lifeTime = lifeTime;

            spawnTime = Time.time;
            armUntil = Time.time + 0.05f; // 稍微加长一点容错

            // ✅ 优化 2: 物理级忽略碰撞 (双重保险)
            // 找到 Owner 身上所有的 Collider，直接告诉物理引擎忽略碰撞
            if (owner != null)
            {
                var ownerColliders = owner.GetComponentsInChildren<Collider2D>();
                foreach (var ownerCol in ownerColliders)
                {
                    Physics2D.IgnoreCollision(col, ownerCol, true);
                }
            }

            dir = dir.sqrMagnitude < 0.0001f ? Vector2.right : dir.normalized;
            rb.velocity = dir * speed;

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            lastPos = rb.position;
        }

        private void Update()
        {
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

                        // ✅ 优化 3: 根节点判定法 (比 IsChildOf 更稳健)
                        // 如果撞到的是 Owner 及其任何子物体/父物体（只要根节点相同）
                        if (owner != null && h.collider.transform.root == owner.transform.root)
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
            
            // 同样的过滤逻辑
            if (owner != null && other.transform.root == owner.transform.root) return;
            
            // 必须在检测层级内
            if (hitLayer.value != 0 && ((1 << other.gameObject.layer) & hitLayer.value) == 0) return;

            HandleHit(other, transform.position);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (useTriggerMode) return;
            // 物理碰撞模式暂未处理，如果需要可以照搬上面的过滤逻辑
        }

        private void HandleHit(Collider2D other, Vector2 hitPoint)
        {
            // 命中可受伤目标
            if (other.TryGetComponent<IDamageable>(out var damageable))
            {
                if (enableDebugLog) Debug.Log($"[Bullet2D] 击中敌人: {other.name}");
                
                var info = new DamageInfo
                {
                    amount = damage,
                    source = owner,
                    hitPoint = hitPoint,
                    direction = rb != null && rb.velocity.sqrMagnitude > 0.0001f ? rb.velocity.normalized : Vector2.zero,
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

            
            Destroy(gameObject);
        }
    }
}
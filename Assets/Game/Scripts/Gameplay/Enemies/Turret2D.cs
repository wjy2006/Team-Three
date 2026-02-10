using UnityEngine;
using Game.Gameplay.Combat;

namespace Game.Gameplay.Combat.Enemies
{
    public class Turret2D : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("不填就会自动找 Tag=Player 的对象")]
        public Transform target;

        [Header("Fire")]
        public Bullet2D bulletPrefab;
        public Transform muzzle;                 // 枪口（不填则用自身 position）
        public float fireInterval = 0.6f;        // 每隔多久打一发
        public float bulletSpeed = 10f;
        public float bulletDamage = 1f;
        public float bulletLifeTime = 3f;

        [Header("Aim")]
        public bool rotateToAim = true;          // 炮台是否旋转朝向玩家
        public float aimLeadTime = 0f;           // 预判：0=不预判（先留着，后面可升级）

        [Header("Activation")]
        public bool onlyFireInRange = true;
        public float fireRange = 8f;
        public LayerMask lineOfSightMask;        // 视线遮挡（可不设，后面再做）

        private float nextFireTime;

        private void Awake()
        {
            if (muzzle == null) muzzle = transform;
        }

        private void Start()
        {
            if (target == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) target = p.transform;
            }
        }

        private void Update()
        {
            if (target == null) return;

            Vector2 from = muzzle.position;
            Vector2 to = target.position;
            Vector2 dir = (to - from);

            // 距离开火限制
            if (onlyFireInRange && dir.sqrMagnitude > fireRange * fireRange)
                return;

            // 可选：视线检测（墙挡住就不打）
            if (lineOfSightMask.value != 0)
            {
                var hit = Physics2D.Raycast(from, dir.normalized, dir.magnitude, lineOfSightMask);
                if (hit.collider != null)
                {
                    // 视线撞到东西了，说明被挡住（你也可以只允许撞到 Player）
                    return;
                }
            }

            // 炮台旋转朝向
            if (rotateToAim && dir.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }

            // 开火计时
            if (Time.time >= nextFireTime)
            {
                Fire(dir.normalized);
                nextFireTime = Time.time + fireInterval;
            }
        }

        private void Fire(Vector2 dir)
        {
            if (bulletPrefab == null) return;

            var bullet = Instantiate(bulletPrefab, muzzle.position, Quaternion.identity);
            bullet.Init(owner: gameObject, dir: dir, speed: bulletSpeed, damage: bulletDamage, lifeTime: bulletLifeTime);

            // 你 Bullet2D 里有 hitLayer，建议在 prefab 上配置
            // bullet.hitLayer = ...
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!onlyFireInRange) return;
            Gizmos.DrawWireSphere(transform.position, fireRange);
        }
#endif
    }
}

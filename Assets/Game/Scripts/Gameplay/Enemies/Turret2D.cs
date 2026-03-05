using UnityEngine;
using Game.Gameplay.Combat;
using Game.Systems.Items;

namespace Game.Gameplay.Combat.Enemies
{
    public class Turret2D : MonoBehaviour
    {
        [Header("Weapon Definition (drive all fire params)")]
        public WeaponDefinition weapon;

        [Header("Target")]
        [Tooltip("不填就会自动找 Tag=Player 的对象")]
        public Transform target;

        [Header("Muzzle")]
        [Tooltip("枪口（可不填；不填则用 WeaponDefinition.firePointLocal 计算）")]
        public Transform muzzle;

        [Header("Aim")]
        public bool rotateToAim = true;
        public float aimLeadTime = 0f;

        [Header("Activation")]
        public bool onlyFireInRange = true;
        public float fireRange = 8f;
        public LayerMask lineOfSightMask;

        [Header("GlobalState Control")]
        [Tooltip("为空则不受GlobalState控制；否则当这个bool为true时禁用开火")]
        public string disableStateKey;

        [Header("Warmup (avoid first-frame wrong target pos)")]
        private readonly int warmupFrames = 3;
        private readonly float warmupSeconds = 0f;
        private float nextFireTime;

        private int warmupLeftFrames;
        private float warmupEndTime;

        private void OnEnable()
        {
            // ✅ 每次启用都重新 warmup（含场景加载后）
            warmupLeftFrames = Mathf.Max(0, warmupFrames);
            warmupEndTime = Time.time + Mathf.Max(0f, warmupSeconds);

            // ✅ 把下一次允许开火的时间推迟到 warmup 结束之后
            // 这样即便 fireRate<=0 或 nextFireTime 默认 0，也不会第一帧开火
            nextFireTime = Mathf.Max(nextFireTime, warmupEndTime);
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
            if (weapon == null) return;
            if (target == null) return;

            // ✅ Warmup gating：前几帧/前几秒只“等待”，避免第一帧拿到错误玩家位置而误射
            if (warmupLeftFrames > 0)
            {
                warmupLeftFrames--;
                return;
            }
            if (Time.time < warmupEndTime)
                return;

            // GlobalState gating
            if (!string.IsNullOrEmpty(disableStateKey))
            {
                if (GameRoot.I == null || GameRoot.I.Global == null) return;
                if (GameRoot.I.Global.GetBool(disableStateKey)) return; // 被关掉，不开火
            }

            Vector2 from = GetMuzzleWorldPos();
            Vector2 to = target.position;
            Vector2 dir = to - from;

            // Range check
            if (onlyFireInRange && dir.sqrMagnitude > fireRange * fireRange)
                return;

            // Line of sight check（mask!=0 才检查）
            if (lineOfSightMask.value != 0 && dir.sqrMagnitude > 0.0001f)
            {
                float dist = Mathf.Sqrt(dir.sqrMagnitude);
                var hit = Physics2D.Raycast(from, dir / dist, dist, lineOfSightMask);
                if (hit.collider != null)
                    return;
            }

            // Rotate turret
            if (rotateToAim && dir.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }

            // Fire rate control (weapon.fireRate <= 0 => unlimited, but we still respect nextFireTime)
            if (Time.time < nextFireTime) return;

            if (weapon.fireRate > 0f)
                nextFireTime = Time.time + (1f / weapon.fireRate);
            else
                nextFireTime = Time.time; // 无上限：不额外节流（但 warmup 已经挡过第一下）

            Fire(dir.normalized);
        }

        private Vector2 GetMuzzleWorldPos()
        {
            if (muzzle != null) return muzzle.position;

            Vector3 world = transform.TransformPoint((Vector3)weapon.firePointLocal);
            return world;
        }

        private void Fire(Vector2 baseDir)
        {
            if (weapon.bulletPrefab == null) return;

            int pellets = Mathf.Max(1, weapon.pellets);
            float spread = Mathf.Max(0f, weapon.spreadDegrees);

            Vector2 spawnPos = GetMuzzleWorldPos();

            for (int i = 0; i < pellets; i++)
            {
                Vector2 dir = ApplySpread(baseDir, spread, pellets, i, weapon.fireMode);
                SpawnBullet(spawnPos, dir);
            }
        }

        private void SpawnBullet(Vector2 spawnPos, Vector2 dir)
        {
            var go = Instantiate(weapon.bulletPrefab, spawnPos, Quaternion.identity);

            var bullet = go.GetComponent<Bullet2D>();
            if (bullet == null)
            {
                Destroy(go);
                return;
            }

            bullet.Init(
                owner: gameObject,
                dir: dir,
                speed: weapon.bulletSpeed,
                damage: weapon.damage,
                lifeTime: weapon.bulletLifeTime
            );
        }

        private Vector2 ApplySpread(Vector2 baseDir, float spreadDegrees, int pellets, int index, WeaponFireMode fireMode)
        {
            if (spreadDegrees <= 0f || pellets <= 1)
                return Rotate(baseDir, weapon.fireAngleOffset);

            float t = pellets == 1 ? 0.5f : (index / (float)(pellets - 1));
            float angle = Mathf.Lerp(-spreadDegrees * 0.5f, spreadDegrees * 0.5f, t);

            angle += weapon.fireAngleOffset;
            return Rotate(baseDir, angle);
        }

        private static Vector2 Rotate(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad);
            float s = Mathf.Sin(rad);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c).normalized;
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
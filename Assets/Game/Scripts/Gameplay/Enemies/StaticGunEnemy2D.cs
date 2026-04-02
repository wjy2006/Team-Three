using UnityEngine;
using Game.Gameplay.Player;
using Game.Systems.Items;
using Game.Gameplay.Combat;

namespace Game.Gameplay.Combat.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HeldItem))]
    [RequireComponent(typeof(EnemyHeldItemVisualController))]
    [RequireComponent(typeof(Health2D))]
    [RequireComponent(typeof(KnockbackReceiver))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class StaticGunEnemy2D : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private HeldItem heldItem;
        [SerializeField] private EnemyHeldItemVisualController heldVisual;
        [SerializeField] private Transform target;
        [Tooltip("Aim direction is solved from this origin. Default: enemy transform.")]
        [SerializeField] private Transform aimOrigin;
        [Tooltip("Optional muzzle override. If empty, visual fire point is used.")]
        [SerializeField] private Transform muzzle;
        [SerializeField] private AudioSource fireAudioSource;

        [Header("Aim")]
        [SerializeField] private bool rotateBodyToAim = false;
        [SerializeField] private float aimDirectionSmoothing = 18f;
        [SerializeField] private float minAimDistance = 0.08f;

        [Header("Aim Prediction")]
        [SerializeField] private bool useLeadAim = true;
        [SerializeField, Range(0f, 2f)] private float leadFactor = 1.0f;
        [SerializeField] private float maxLeadTime = 0.35f;
        [Tooltip("Set >= 0 to force a fixed lead time. Set < 0 to use distance/bulletSpeed.")]
        [SerializeField] private float fixedLeadTime = -1f;
        [Tooltip("Extra lead scale by targetSpeed/bulletSpeed.")]
        [SerializeField] private float speedLeadGain = 0.45f;
        [Tooltip("Additional random aim jitter (degrees) used at fire moment.")]
        [SerializeField] private float fireAimJitterDegrees = 0f;

        [Header("Activation")]
        [SerializeField] private bool onlyFireInRange = true;
        [SerializeField] private float fireRange = 8f;
        [SerializeField] private LayerMask lineOfSightMask = 0;

        [Header("Warmup")]
        [SerializeField] private int warmupFrames = 2;
        [SerializeField] private float warmupSeconds = 0f;

        [Header("Bullet Hit Layer (Optional Override)")]
        [SerializeField] private bool overrideBulletHitLayer = false;
        [SerializeField] private LayerMask bulletHitLayer;
        [SerializeField] private bool allowSelfHit = true;

        [Header("Spawn Stability")]
        [Tooltip("Push bullet spawn forward when muzzle enters self collider.")]
        [SerializeField] private float muzzleExitStep = 0.04f;
        [SerializeField] private int muzzleExitMaxSteps = 16;

        private float nextFireTime;
        private int warmupLeftFrames;
        private float warmupEndTime;

        private Rigidbody2D targetRb;
        private bool hasLastTargetPos;
        private Vector2 lastTargetPos;
        private Vector2 smoothedAimDir = Vector2.right;
        private Collider2D[] selfColliders;

        private void Reset()
        {
            heldItem = GetComponent<HeldItem>();
            heldVisual = GetComponent<EnemyHeldItemVisualController>();
            if (aimOrigin == null) aimOrigin = transform;

            var hp = GetComponent<Health2D>();
            if (hp != null)
            {
                hp.maxHp = 20f;
                hp.hp = 20f;
            }
        }

        private void Awake()
        {
            if (heldItem == null) heldItem = GetComponent<HeldItem>();
            if (heldVisual == null) heldVisual = GetComponent<EnemyHeldItemVisualController>();
            if (aimOrigin == null) aimOrigin = transform;

            if (fireAudioSource != null)
                fireAudioSource.ignoreListenerPause = true;

            selfColliders = GetComponentsInChildren<Collider2D>();
        }

        private void OnEnable()
        {
            warmupLeftFrames = Mathf.Max(0, warmupFrames);
            warmupEndTime = Time.time + Mathf.Max(0f, warmupSeconds);
            nextFireTime = Mathf.Max(nextFireTime, warmupEndTime);
            hasLastTargetPos = false;

            if (heldVisual != null)
                heldVisual.RefreshNow();
        }

        private void Start()
        {
            TryResolveTarget();
        }

        private void Update()
        {
            if (GameRoot.I != null && GameRoot.I.Pause != null && GameRoot.I.Pause.IsPaused)
                return;

            TryResolveTarget();
            if (target == null) return;

            WeaponDefinition weapon = heldItem != null ? heldItem.held as WeaponDefinition : null;
            if (weapon == null || weapon.bulletPrefab == null) return;

            if (warmupLeftFrames > 0)
            {
                warmupLeftFrames--;
                return;
            }
            if (Time.time < warmupEndTime) return;

            Vector2 originPos = aimOrigin != null ? (Vector2)aimOrigin.position : (Vector2)transform.position;
            Vector2 predictedTargetPos = GetPredictedTargetPos(originPos, weapon);
            Vector2 toTarget = predictedTargetPos - originPos;
            float distSqr = toTarget.sqrMagnitude;
            if (distSqr <= 0.0001f) return;

            if (onlyFireInRange && distSqr > fireRange * fireRange)
                return;

            float minDistSqr = minAimDistance * minAimDistance;
            Vector2 desiredAimDir = toTarget.sqrMagnitude <= minDistSqr
                ? (smoothedAimDir.sqrMagnitude > 0.0001f ? smoothedAimDir : Vector2.right)
                : toTarget.normalized;

            float smoothT = 1f - Mathf.Exp(-Mathf.Max(0.01f, aimDirectionSmoothing) * Time.deltaTime);
            smoothedAimDir = Vector2.Lerp(smoothedAimDir, desiredAimDir, smoothT);
            if (smoothedAimDir.sqrMagnitude <= 0.0001f)
                smoothedAimDir = desiredAimDir;
            else
                smoothedAimDir.Normalize();

            if (heldVisual != null)
            {
                heldVisual.SetAimDirection(smoothedAimDir);
                heldVisual.RefreshNow();
            }

            if (rotateBodyToAim)
            {
                float angle = Mathf.Atan2(smoothedAimDir.y, smoothedAimDir.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            Vector2 spawnPos = GetMuzzleWorldPos(weapon);

            if (lineOfSightMask.value != 0)
            {
                Vector2 losToTarget = predictedTargetPos - spawnPos;
                float losDist = losToTarget.magnitude;
                if (losDist > 0.0001f)
                {
                    Vector2 dirForRay = losToTarget / losDist;
                    var hit = Physics2D.Raycast(spawnPos, dirForRay, losDist, lineOfSightMask);
                    if (hit.collider != null)
                        return;
                }
            }

            if (Time.time < nextFireTime) return;
            if (weapon.fireRate > 0f) nextFireTime = Time.time + (1f / weapon.fireRate);
            else nextFireTime = Time.time;

            Vector2 fireAimDir = BuildFireAimDirection(smoothedAimDir);
            FireWeapon(weapon, spawnPos, fireAimDir);
        }

        private Vector2 GetPredictedTargetPos(Vector2 originPos, WeaponDefinition weapon)
        {
            Vector2 currentTargetPos = target.position;
            Vector2 targetVelocity = GetTargetVelocity(currentTargetPos);

            if (!useLeadAim)
                return currentTargetPos;

            float leadTime;
            if (fixedLeadTime >= 0f)
            {
                leadTime = fixedLeadTime;
            }
            else
            {
                float bulletSpeed = Mathf.Max(0.01f, weapon.bulletSpeed);
                float dist = Vector2.Distance(originPos, currentTargetPos);
                leadTime = dist / bulletSpeed;
            }

            float speedFactor = 1f + Mathf.Max(0f, speedLeadGain) * (targetVelocity.magnitude / Mathf.Max(0.01f, weapon.bulletSpeed));
            leadTime *= Mathf.Max(0f, leadFactor) * speedFactor;
            leadTime = Mathf.Clamp(leadTime, 0f, Mathf.Max(0f, maxLeadTime));
            return currentTargetPos + targetVelocity * leadTime;
        }

        private Vector2 BuildFireAimDirection(Vector2 fallbackDir)
        {
            Vector2 dir = GetMuzzleForwardDir(fallbackDir);
            return ApplyFireJitter(dir);
        }

        private Vector2 ApplyFireJitter(Vector2 dir)
        {
            float jitter = Mathf.Max(0f, fireAimJitterDegrees);
            if (jitter <= 0f) return dir.normalized;
            return Rotate(dir.normalized, Random.Range(-jitter, jitter));
        }

        private Vector2 GetMuzzleForwardDir(Vector2 fallbackDir)
        {
            if (muzzle != null)
            {
                Vector2 d = muzzle.right;
                if (d.sqrMagnitude > 0.0001f) return d.normalized;
            }

            if (heldVisual != null)
            {
                Vector2 d = heldVisual.GetFirePointForwardDir();
                if (d.sqrMagnitude > 0.0001f) return d.normalized;
            }

            if (fallbackDir.sqrMagnitude > 0.0001f) return fallbackDir.normalized;
            return Vector2.right;
        }

        private Vector2 GetTargetVelocity(Vector2 currentTargetPos)
        {
            if (targetRb == null && target != null)
                targetRb = target.GetComponent<Rigidbody2D>();

            if (targetRb != null)
                return targetRb.linearVelocity;

            if (!hasLastTargetPos)
            {
                hasLastTargetPos = true;
                lastTargetPos = currentTargetPos;
                return Vector2.zero;
            }

            float dt = Mathf.Max(0.0001f, Time.deltaTime);
            Vector2 velocity = (currentTargetPos - lastTargetPos) / dt;
            lastTargetPos = currentTargetPos;
            return velocity;
        }

        private void FireWeapon(WeaponDefinition weapon, Vector2 spawnPos, Vector2 baseDir)
        {
            int pellets = Mathf.Max(1, weapon.pellets);
            float spread = Mathf.Max(0f, weapon.spreadDegrees);

            for (int i = 0; i < pellets; i++)
            {
                Vector2 shotDir = ApplyRandomSpread(baseDir, spread, weapon.fireAngleOffset);
                SpawnBullet(weapon, spawnPos, shotDir);
            }

            if (fireAudioSource != null && weapon.UseSfx != null)
                fireAudioSource.PlayOneShot(weapon.UseSfx);
        }

        private void SpawnBullet(WeaponDefinition weapon, Vector2 spawnPos, Vector2 dir)
        {
            Vector2 safeSpawnPos = ResolveSafeSpawnPos(spawnPos, dir);
            var go = Instantiate(weapon.bulletPrefab, safeSpawnPos, Quaternion.identity);
            if (go == null) return;

            if (go.TryGetComponent<Bullet2D>(out var bullet))
            {
                if (overrideBulletHitLayer)
                    bullet.hitLayer = bulletHitLayer;

                bullet.Init(
                    owner: allowSelfHit ? null : gameObject,
                    dir: dir,
                    speed: weapon.bulletSpeed,
                    damage: weapon.damage,
                    lifeTime: weapon.bulletLifeTime
                );
                return;
            }

            if (go.TryGetComponent<Rigidbody2D>(out var rb))
                rb.linearVelocity = dir * weapon.bulletSpeed;
        }

        private Vector2 ResolveSafeSpawnPos(Vector2 spawnPos, Vector2 shotDir)
        {
            if (selfColliders == null || selfColliders.Length == 0) return spawnPos;

            Vector2 dir = shotDir.sqrMagnitude > 0.0001f ? shotDir.normalized : Vector2.right;
            float step = Mathf.Max(0.005f, muzzleExitStep);
            int maxSteps = Mathf.Clamp(muzzleExitMaxSteps, 1, 64);

            Vector2 pos = spawnPos;
            for (int i = 0; i < maxSteps; i++)
            {
                if (!IsInsideAnySolidSelfCollider(pos))
                    return pos;
                pos += dir * step;
            }

            if (IsInsideAnySolidSelfCollider(pos))
            {
                Vector2 origin = aimOrigin != null ? (Vector2)aimOrigin.position : (Vector2)transform.position;
                float furthest = 0f;
                for (int i = 0; i < selfColliders.Length; i++)
                {
                    var c = selfColliders[i];
                    if (c == null || !c.enabled || !c.gameObject.activeInHierarchy || c.isTrigger) continue;
                    Bounds b = c.bounds;
                    Vector2 centerOffset = (Vector2)b.center - origin;
                    float support = Vector2.Dot(centerOffset, dir) +
                                    Mathf.Abs(dir.x) * b.extents.x +
                                    Mathf.Abs(dir.y) * b.extents.y;
                    if (support > furthest) furthest = support;
                }
                pos = origin + dir * (furthest + step);
            }

            return pos;
        }

        private bool IsInsideAnySolidSelfCollider(Vector2 point)
        {
            if (selfColliders == null) return false;

            for (int i = 0; i < selfColliders.Length; i++)
            {
                var c = selfColliders[i];
                if (c == null || !c.enabled || !c.gameObject.activeInHierarchy || c.isTrigger) continue;
                if (c.OverlapPoint(point))
                    return true;
            }
            return false;
        }

        private Vector2 GetMuzzleWorldPos(WeaponDefinition weapon)
        {
            if (muzzle != null) return muzzle.position;
            if (heldVisual != null) return heldVisual.GetFirePointWorldPos();
            return transform.TransformPoint((Vector3)weapon.firePointLocal);
        }

        private void TryResolveTarget()
        {
            if (target != null)
            {
                if (targetRb == null)
                    targetRb = target.GetComponent<Rigidbody2D>();
                return;
            }

            var p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) return;

            target = p.transform;
            targetRb = p.GetComponent<Rigidbody2D>();
            hasLastTargetPos = false;
        }

        private static Vector2 ApplyRandomSpread(Vector2 baseDir, float spreadDegrees, float angleOffset)
        {
            if (spreadDegrees <= 0f)
                return Rotate(baseDir, angleOffset);

            float randomOffset = Random.Range(-spreadDegrees, spreadDegrees);
            float angle = randomOffset + angleOffset;
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
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, fireRange);
        }
#endif
    }
}

using UnityEngine;
using Game.Gameplay.Player;
using Game.Systems.Items;
using Game.Gameplay.Combat;

namespace Game.Gameplay.Combat.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HeldItem))]
    [RequireComponent(typeof(EnemyHeldItemVisualController))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class AdminBossSweepShooter2D : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private HeldItem heldItem;
        [SerializeField] private EnemyHeldItemVisualController heldVisual;
        [SerializeField] private Transform aimOrigin;
        [SerializeField] private Transform muzzle;
        [SerializeField] private AudioSource fireAudioSource;

        [Header("Sweep Fire")]
        [SerializeField] private bool clockwise = false;
        [SerializeField] private float shotStepDegrees = 6f;
        [SerializeField] private bool randomizeStartAngle = true;
        [SerializeField] private float startAngleDegrees = 0f;
        [SerializeField, Min(1)] private int shotsPerFrame = 1;

        [Header("Warmup")]
        [SerializeField] private int warmupFrames = 0;

        [Header("Audio")]
        [SerializeField] private bool playWeaponSfx = true;
        [SerializeField, Min(0f)] private float minSfxInterval = 0.04f;

        [Header("Bullet Hit Layer (Optional Override)")]
        [SerializeField] private bool overrideBulletHitLayer = false;
        [SerializeField] private LayerMask bulletHitLayer;
        [SerializeField] private bool allowSelfHit = true;

        [Header("Spawn Stability")]
        [SerializeField] private float muzzleExitStep = 0.04f;
        [SerializeField] private int muzzleExitMaxSteps = 16;

        private int warmupLeftFrames;
        private float currentAngleDeg;
        private float lastSfxTime = -999f;
        private Collider2D[] selfColliders;

        private void Reset()
        {
            heldItem = GetComponent<HeldItem>();
            heldVisual = GetComponent<EnemyHeldItemVisualController>();
            if (aimOrigin == null) aimOrigin = transform;
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
            currentAngleDeg = randomizeStartAngle ? Random.Range(0f, 360f) : startAngleDegrees;

            if (heldVisual != null)
                heldVisual.RefreshNow();
        }

        private void Update()
        {
            if (GameRoot.I != null && GameRoot.I.Pause != null && GameRoot.I.Pause.IsPaused)
                return;

            WeaponDefinition weapon = heldItem != null ? heldItem.held as WeaponDefinition : null;
            if (weapon == null || weapon.bulletPrefab == null) return;

            if (warmupLeftFrames > 0)
            {
                warmupLeftFrames--;
                return;
            }

            int shotCount = Mathf.Max(1, shotsPerFrame);
            for (int i = 0; i < shotCount; i++)
            {
                Vector2 baseDir = AngleToDir(currentAngleDeg);

                if (heldVisual != null)
                {
                    heldVisual.SetAimDirection(baseDir);
                    heldVisual.RefreshNow();
                }

                Vector2 spawnPos = GetMuzzleWorldPos(weapon);
                FireWeapon(weapon, spawnPos, baseDir);

                float signedStep = Mathf.Abs(shotStepDegrees) * (clockwise ? -1f : 1f);
                currentAngleDeg = Mathf.Repeat(currentAngleDeg + signedStep, 360f);
            }
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

            if (!playWeaponSfx) return;
            if (fireAudioSource == null || weapon.UseSfx == null) return;

            float now = Time.time;
            if (now - lastSfxTime < Mathf.Max(0f, minSfxInterval)) return;
            fireAudioSource.PlayOneShot(weapon.UseSfx);
            lastSfxTime = now;
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

        private static Vector2 ApplyRandomSpread(Vector2 baseDir, float spreadDegrees, float angleOffset)
        {
            if (spreadDegrees <= 0f)
                return Rotate(baseDir, angleOffset);

            float randomOffset = Random.Range(-spreadDegrees, spreadDegrees);
            return Rotate(baseDir, randomOffset + angleOffset);
        }

        private static Vector2 Rotate(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad);
            float s = Mathf.Sin(rad);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c).normalized;
        }

        private static Vector2 AngleToDir(float angleDeg)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }
    }
}

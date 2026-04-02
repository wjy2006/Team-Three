using UnityEngine;
using Game.Gameplay.Combat;

namespace Game.Gameplay.Combat.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public class GunEnemyMover2D : MonoBehaviour
    {
        [Header("Move Settings (match TopDownMove2D defaults)")]
        public float moveSpeed = 12f;
        public float acceleration = 40f;
        public float deceleration = 50f;
        public bool canMove = true;

        [Header("Physics")]
        public float maxTotalSpeed = 20f;

        [Header("AI Range")]
        [Tooltip("Too close -> back off.")]
        public float keepOutDistance = 3f;
        [Tooltip("Too far -> chase target.")]
        public float keepInDistance = 6f;
        [Tooltip("Distance hysteresis to avoid rapid state bouncing around thresholds.")]
        public float rangeHysteresis = 0.25f;
        [Tooltip("When very close, disable strafe to avoid hard swing.")]
        public float closeNoStrafeDistance = 1.0f;

        [Header("Engage Distance")]
        [Tooltip("If enabled, enemy will not chase when target is too far.")]
        public bool useEngageDistance = true;
        [Tooltip("Start chasing only when target enters this distance.")]
        public float engageDistance = 11f;
        [Tooltip("Stop chasing when target goes beyond this distance.")]
        public float disengageDistance = 13f;
        [Tooltip("If true, once this enemy is damaged it ignores engage/disengage distance limits and keeps chasing.")]
        public bool forceEngageOnDamaged = true;

        [Header("AI Strafe")]
        [Range(0f, 1f)] public float strafeWeight = 0.65f;
        public float strafeFlipMinSeconds = 0.45f;
        public float strafeFlipMaxSeconds = 1.1f;
        public bool fourDirectionOnly = false;

        [Header("Direction Smoothing")]
        public float directionSmoothing = 14f;

        [Header("Target")]
        public Transform target;
        public string autoTargetTag = "Player";

        private enum RangeState
        {
            TooClose,
            InBand,
            TooFar
        }

        private Rigidbody2D rb;
        private int strafeSign = 1;
        private float nextStrafeFlipTime;
        private Vector2 smoothedWishDir = Vector2.zero;
        private Vector2 lastRadialDir = Vector2.right;
        private RangeState rangeState = RangeState.InBand;
        private bool isEngaged = false;
        private bool forceEngagedByDamage = false;
        private Health2D health;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            health = GetComponent<Health2D>();
            ScheduleNextStrafeFlip();
        }

        private void OnEnable()
        {
            smoothedWishDir = Vector2.zero;
            lastRadialDir = Vector2.right;
            rangeState = RangeState.InBand;
            isEngaged = false;
            forceEngagedByDamage = false;
            ScheduleNextStrafeFlip();

            if (health == null) health = GetComponent<Health2D>();
            if (health != null) health.OnDamaged += OnDamaged;
        }

        private void OnDisable()
        {
            if (health != null) health.OnDamaged -= OnDamaged;
        }

        private void Update()
        {
            if (target == null && !string.IsNullOrEmpty(autoTargetTag))
            {
                var t = GameObject.FindGameObjectWithTag(autoTargetTag);
                if (t != null) target = t.transform;
            }
        }

        private void FixedUpdate()
        {
            if (!canMove) return;
            if (rb == null) return;
            if (GameRoot.I != null && GameRoot.I.Pause != null && GameRoot.I.Pause.IsPaused) return;
            if (GameRoot.I != null && GameRoot.I.InputLocked) return;

            Vector2 desiredWishDir = ComputeWishDirection();

            float smoothT = 1f - Mathf.Exp(-Mathf.Max(0.01f, directionSmoothing) * Time.fixedDeltaTime);
            smoothedWishDir = Vector2.Lerp(smoothedWishDir, desiredWishDir, smoothT);
            if (smoothedWishDir.sqrMagnitude < 0.0001f)
                smoothedWishDir = Vector2.zero;
            else if (smoothedWishDir.sqrMagnitude > 1f)
                smoothedWishDir.Normalize();

            Vector2 v = rb.linearVelocity;
            Vector2 targetVel = smoothedWishDir * moveSpeed;

            float rate = smoothedWishDir == Vector2.zero ? deceleration : acceleration;
            v = Vector2.MoveTowards(v, targetVel, rate * Time.fixedDeltaTime);

            if (v.magnitude > maxTotalSpeed)
                v = v.normalized * maxTotalSpeed;

            rb.linearVelocity = v;
        }

        private Vector2 ComputeWishDirection()
        {
            if (target == null) return Vector2.zero;

            Vector2 toTarget = (Vector2)target.position - rb.position;
            float dist = toTarget.magnitude;

            if (dist > 0.0001f)
                lastRadialDir = toTarget / dist;

            if (useEngageDistance)
            {
                float engage = Mathf.Max(0f, engageDistance);
                float disengage = Mathf.Max(engage, disengageDistance);

                if (!forceEngagedByDamage)
                {
                    if (isEngaged)
                    {
                        if (dist > disengage) isEngaged = false;
                    }
                    else
                    {
                        if (dist <= engage) isEngaged = true;
                    }
                }

                if (!isEngaged && !forceEngagedByDamage)
                    return Vector2.zero;
            }

            Vector2 radialDir = lastRadialDir;

            UpdateRangeState(dist);

            float radialWeight = 0f;
            switch (rangeState)
            {
                case RangeState.TooFar:
                    radialWeight = 1f;
                    break;
                case RangeState.TooClose:
                    radialWeight = -1f;
                    break;
                case RangeState.InBand:
                    radialWeight = 0f;
                    break;
            }

            if (Time.time >= nextStrafeFlipTime)
            {
                strafeSign = -strafeSign;
                ScheduleNextStrafeFlip();
            }

            float localStrafeWeight = dist <= closeNoStrafeDistance ? 0f : strafeWeight;
            Vector2 strafeDir = new Vector2(-radialDir.y, radialDir.x) * strafeSign;
            Vector2 wish = radialDir * radialWeight + strafeDir * localStrafeWeight;

            if (wish.sqrMagnitude <= 0.0001f)
                return Vector2.zero;

            wish.Normalize();

            if (fourDirectionOnly)
                wish = QuantizeTo4Dir(wish);

            return wish;
        }

        private void UpdateRangeState(float dist)
        {
            float h = Mathf.Max(0f, rangeHysteresis);
            float closeEnter = Mathf.Max(0f, keepOutDistance - h);
            float closeExit = keepOutDistance + h;
            float farEnter = keepInDistance + h;
            float farExit = Mathf.Max(keepOutDistance, keepInDistance - h);

            switch (rangeState)
            {
                case RangeState.TooClose:
                    if (dist > closeExit) rangeState = RangeState.InBand;
                    break;
                case RangeState.TooFar:
                    if (dist < farExit) rangeState = RangeState.InBand;
                    break;
                case RangeState.InBand:
                    if (dist < closeEnter) rangeState = RangeState.TooClose;
                    else if (dist > farEnter) rangeState = RangeState.TooFar;
                    break;
            }
        }

        private void ScheduleNextStrafeFlip()
        {
            float min = Mathf.Max(0.01f, strafeFlipMinSeconds);
            float max = Mathf.Max(min, strafeFlipMaxSeconds);
            nextStrafeFlipTime = Time.time + Random.Range(min, max);
        }

        private static Vector2 QuantizeTo4Dir(Vector2 v)
        {
            if (Mathf.Abs(v.x) >= Mathf.Abs(v.y))
                return new Vector2(Mathf.Sign(v.x), 0f);
            return new Vector2(0f, Mathf.Sign(v.y));
        }

        private void OnValidate()
        {
            if (keepOutDistance < 0f) keepOutDistance = 0f;
            if (keepInDistance < keepOutDistance)
                keepInDistance = keepOutDistance;
            if (closeNoStrafeDistance < 0f)
                closeNoStrafeDistance = 0f;
            if (engageDistance < 0f)
                engageDistance = 0f;
            if (disengageDistance < engageDistance)
                disengageDistance = engageDistance;
        }

        private void OnDamaged(DamageInfo info)
        {
            if (!forceEngageOnDamaged) return;
            forceEngagedByDamage = true;
            isEngaged = true;
        }
    }
}

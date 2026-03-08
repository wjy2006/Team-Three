using UnityEngine;
using Game.Systems.Items;
using Game.Gameplay.Combat;
using Game.Gameplay.Player;
using Game.Core;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Health2D))]
[RequireComponent(typeof(AudioSource))]
public class RocketMountEntity : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private RocketThrusterVfxController thrusterVfx;
    [SerializeField] private AudioSource flightLoopSource;
    private Health2D health;

    private RocketRideEffect cfg;
    private RocketRideController controller;

    private GameObject playerGO;
    private Rigidbody2D playerRB;
    private PlayerStats playerStats;
    private ItemDefinition sourceItem;

    private string instanceId;
    private Vector2 aimDir = Vector2.right;
    private bool accelHeld;

    private readonly Collider2D[] overlap = new Collider2D[32];
    private ContactFilter2D filter;

    private bool exploded;
    private Vector2 prevV;

    private bool isBoundToPlayer;
    private bool bindRequested;
    private bool isFlightLoopPlaying;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (thrusterVfx == null) thrusterVfx = GetComponentInChildren<RocketThrusterVfxController>(true);
        if (flightLoopSource == null) flightLoopSource = GetComponent<AudioSource>();

        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        health = GetComponent<Health2D>();

        if (flightLoopSource != null)
        {
            flightLoopSource.playOnAwake = false;
            flightLoopSource.loop = false;
            flightLoopSource.spatialBlend = 0f;
            flightLoopSource.ignoreListenerPause = true;
        }
    }

    public void Attach(
        GameObject playerGO,
        PlayerStats playerStats,
        RocketRideController controller,
        RocketRideEffect effect,
        ItemDefinition sourceItem,
        string instanceId,
        int startHp)
    {
        this.playerGO = playerGO;
        this.playerStats = playerStats;
        this.controller = controller;
        this.cfg = effect;
        this.sourceItem = sourceItem;
        this.instanceId = instanceId;

        if (playerGO != null) playerRB = playerGO.GetComponent<Rigidbody2D>();

        health.maxHp = cfg.rocketHp;
        health.hp = Mathf.Clamp(startHp, 0, cfg.rocketHp);

        health.OnDamaged -= OnDamaged;
        health.OnDamaged += OnDamaged;

        filter = new ContactFilter2D();
        filter.SetLayerMask(cfg.explosionLayer);
        filter.useTriggers = true;

        SnapToPlayer();

        isBoundToPlayer = false;
        bindRequested = false;
        isFlightLoopPlaying = false;
    }

    public int GetCurrentHp() => health != null ? Mathf.CeilToInt(health.hp) : 0;

    public void SetInput(Vector2 aimDir, bool accelHeld)
    {
        if (aimDir.sqrMagnitude > 0.0001f) this.aimDir = aimDir.normalized;

        this.accelHeld = accelHeld;

        if (thrusterVfx != null)
            thrusterVfx.SetThrustActive(accelHeld);

        if (accelHeld)
            StartFlightLoopSfx();
        else
            StopFlightLoopSfx();
    }

    public void OnPostSpawn()
    {
        if (exploded || cfg == null) return;

        SnapToPlayer();
        bindRequested = true;
    }

    public void BindNowIfSafe()
    {
        if (exploded || cfg == null) return;

        SnapToPlayer();
        bindRequested = true;

        if (GameRoot.I == null || !GameRoot.I.IsTransitioning)
            ActivateBinding();
    }

    public void SnapToPlayer()
    {
        if (playerGO == null) return;

        Vector2 p = playerGO.transform.position;
        rb.position = p;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        transform.position = p;
    }

    private void FixedUpdate()
    {
        if (exploded || cfg == null) return;

        prevV = rb.linearVelocity;

        float targetDeg = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
        float maxStep = cfg.maxTurnDegPerSec * Time.fixedDeltaTime;
        float newDeg = Mathf.MoveTowardsAngle(rb.rotation, targetDeg, maxStep);
        rb.MoveRotation(newDeg);

        if (accelHeld)
        {
            float rad = rb.rotation * Mathf.Deg2Rad;
            Vector2 forward = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            rb.linearVelocity += forward * (cfg.accel * Time.fixedDeltaTime);

            if (rb.linearVelocity.magnitude > cfg.maxSpeed)
                rb.linearVelocity = rb.linearVelocity.normalized * cfg.maxSpeed;
        }

        if (GameRoot.I != null && GameRoot.I.IsTransitioning)
            return;

        if (bindRequested && !isBoundToPlayer)
            ActivateBinding();

        if (isBoundToPlayer && playerRB != null && playerGO != null)
        {
            playerRB.MovePosition(rb.position);
            playerGO.transform.position = (Vector3)rb.position + new Vector3(0f, 0f, playerGO.transform.position.z);
        }
    }

    private void ActivateBinding()
    {
        isBoundToPlayer = true;
        bindRequested = false;

        if (playerRB == null) return;

        playerRB.bodyType = RigidbodyType2D.Kinematic;
        playerRB.linearVelocity = Vector2.zero;
        playerRB.angularVelocity = 0f;
        playerRB.position = rb.position;
        Physics2D.SyncTransforms();
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (exploded || cfg == null || health == null) return;

        Vector2 dv = rb.linearVelocity - prevV;
        float dvMag = dv.magnitude;
        if (dvMag < cfg.dvThreshold) return;

        float dmg = (dvMag - cfg.dvThreshold) * cfg.damagePerDv;
        if (cfg.maxImpactDamage > 0f) dmg = Mathf.Min(dmg, cfg.maxImpactDamage);

        health.TakeDamage(new DamageInfo
        {
            amount = dmg,
            source = gameObject,
            hitPoint = col.GetContact(0).point,
            direction = dv.normalized,
            kind = "rocket_impact"
        });
    }

    private void OnDamaged(DamageInfo info)
    {
        if (exploded) return;
        if (health.hp <= 0f) Explode();
    }

    private void Explode()
    {
        if (exploded) return;
        exploded = true;

        if (thrusterVfx != null)
            thrusterVfx.ForceStopAll();

        StopFlightLoopSfx();
        PlayExplosionSfx();

        Vector2 center = rb.position;

        if (cfg.explosionVfxPrefab != null)
        {
            var vfx = Instantiate(cfg.explosionVfxPrefab, center, Quaternion.identity);
            Destroy(vfx, cfg.explosionVfxLife);
        }

        int count = Physics2D.OverlapCircle(center, cfg.explosionRadius, filter, overlap);
        for (int i = 0; i < count; i++)
        {
            if (overlap[i].TryGetComponent<IDamageable>(out var target))
            {
                if (playerGO != null && overlap[i].gameObject == playerGO) continue;
                target.TakeDamage(new DamageInfo { amount = cfg.damageToOthers, source = gameObject });
            }
        }

        if (playerStats != null)
            playerStats.TakeDamage(new DamageInfo { amount = cfg.damageToPlayer, source = gameObject });

        if (controller != null)
            controller.OnRocketFinished(sourceItem, instanceId, consumeHeldItem: true);

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (health != null)
            health.OnDamaged -= OnDamaged;

        if (thrusterVfx != null)
            thrusterVfx.ForceStopAll();

        StopFlightLoopSfx();

        bool restoringBecauseSwitch = controller != null && controller.IsSwitchingRockets();

        if (!restoringBecauseSwitch && playerRB != null)
        {
            playerRB.bodyType = RigidbodyType2D.Dynamic;
            playerRB.linearVelocity = Vector2.zero;
            playerRB.angularVelocity = 0f;
        }

        if (!exploded && controller != null)
            controller.OnRocketFinished(sourceItem, instanceId, consumeHeldItem: false);
    }

    private void StartFlightLoopSfx()
    {
        if (isFlightLoopPlaying || cfg == null || cfg.flyingLoopSfx == null || flightLoopSource == null) return;

        flightLoopSource.clip = cfg.flyingLoopSfx;
        flightLoopSource.loop = true;
        flightLoopSource.Play();
        isFlightLoopPlaying = true;
    }

    private void StopFlightLoopSfx()
    {
        if (!isFlightLoopPlaying && (flightLoopSource == null || flightLoopSource.clip != cfg.flyingLoopSfx)) return;

        if (flightLoopSource != null && flightLoopSource.isPlaying && flightLoopSource.clip == cfg.flyingLoopSfx)
            flightLoopSource.Stop();

        if (flightLoopSource != null && flightLoopSource.clip == cfg.flyingLoopSfx)
        {
            flightLoopSource.clip = null;
            flightLoopSource.loop = false;
        }

        isFlightLoopPlaying = false;
    }

    private void PlayExplosionSfx()
    {
        if (cfg == null || cfg.explosionSfx == null) return;

        AudioSource source = GameRoot.I != null ? GameRoot.I.globalSfxSource : null;
        if (source != null)
            source.PlayOneShot(cfg.explosionSfx);
    }
}

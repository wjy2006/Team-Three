using System;
using System.Collections.Generic;
using Game.Gameplay.Combat;
using UnityEngine;

[DisallowMultipleComponent]
public class DamageSwitchTimedWall : MonoBehaviour, IDamageable
{
    [Header("Timing")]
    [Min(0f)] public float wallDownSeconds = 5f;
    public bool useUnscaledTime = false;

    [Header("Global")]
    [Tooltip("Global key storing the wall-open-until timestamp in milliseconds.")]
    public string openUntilGlobalKey;

    [Header("Wall Targets")]
    [Tooltip("Preferred: assign wall roots here. They will be set inactive while the switch is active.")]
    public GameObject[] wallObjects;

    [Header("Damage Filter")]
    [Tooltip("When enabled, only damage with amount > 0 can trigger the switch.")]
    public bool requirePositiveDamage = false;
    [Tooltip("When enabled, only direct bullet hits trigger the switch. Explosion/splash damage is ignored.")]
    public bool directBulletOnly = true;

    [Header("Events")]
    [Tooltip("Raise DamagedEvent when the switch is hit.")]
    public bool raiseDamagedEvent = false;

    private readonly List<GameObjectState> objectStates = new List<GameObjectState>();
    private bool wallOpenedBySwitch;
    private int localFallbackUntilMs;
    private string resolvedAutoKey;

    private struct GameObjectState
    {
        public GameObject target;
        public bool activeSelf;
    }

    private void Awake()
    {
        CacheInitialStates();
        SyncFromTimerState();
    }

    private void OnEnable()
    {
        SyncFromTimerState();
    }

    private void Update()
    {
        SyncFromTimerState();
    }

    private void OnDisable()
    {
        RestoreInitialStates();
        wallOpenedBySwitch = false;
    }

    public void TakeDamage(DamageInfo info)
    {
        if (requirePositiveDamage && info.amount <= 0f) return;
        if (directBulletOnly && !IsDirectBulletHit(info)) return;

        if (raiseDamagedEvent && GameRoot.I != null && GameRoot.I.Triggers != null)
            GameRoot.I.Triggers.Raise(new DamagedEvent(gameObject, info));

        ArmWallOpenTimer();
        SyncFromTimerState();
    }

    private void ArmWallOpenTimer()
    {
        int nowMs = GetNowMs();
        int durationMs = Mathf.Max(0, Mathf.RoundToInt(Mathf.Max(0f, wallDownSeconds) * 1000f));
        int targetUntil = nowMs + durationMs;

        var global = GameRoot.I != null ? GameRoot.I.Global : null;
        string key = ResolveGlobalKey();
        if (global != null && !string.IsNullOrEmpty(key))
        {
            int currentUntil = global.GetInt(key);
            if (targetUntil < currentUntil) targetUntil = currentUntil;
            global.SetInt(key, targetUntil);
            return;
        }

        if (targetUntil < localFallbackUntilMs) targetUntil = localFallbackUntilMs;
        localFallbackUntilMs = targetUntil;
    }

    private void SyncFromTimerState()
    {
        int nowMs = GetNowMs();
        var global = GameRoot.I != null ? GameRoot.I.Global : null;
        string key = ResolveGlobalKey();

        int untilMs = 0;
        bool usingGlobal = global != null && !string.IsNullOrEmpty(key);
        if (usingGlobal)
        {
            untilMs = global.GetInt(key);
        }
        else
        {
            untilMs = localFallbackUntilMs;
        }

        bool shouldOpen = nowMs < untilMs;
        if (shouldOpen == wallOpenedBySwitch) return;

        if (shouldOpen)
        {
            SetWallActive(false);
            wallOpenedBySwitch = true;
            return;
        }

        RestoreInitialStates();
        wallOpenedBySwitch = false;

        if (usingGlobal && untilMs != 0)
            global.SetInt(key, 0);
        else if (!usingGlobal)
            localFallbackUntilMs = 0;
    }

    private void CacheInitialStates()
    {
        objectStates.Clear();

        var objectSet = new HashSet<GameObject>();
        if (wallObjects != null)
        {
            for (int i = 0; i < wallObjects.Length; i++)
            {
                var target = wallObjects[i];
                if (target == null || !objectSet.Add(target)) continue;

                objectStates.Add(new GameObjectState
                {
                    target = target,
                    activeSelf = target.activeSelf
                });
            }
        }
    }

    private void SetWallActive(bool active)
    {
        for (int i = 0; i < objectStates.Count; i++)
        {
            var target = objectStates[i].target;
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }
    }

    private void RestoreInitialStates()
    {
        for (int i = 0; i < objectStates.Count; i++)
        {
            var state = objectStates[i];
            if (state.target != null && state.target.activeSelf != state.activeSelf)
                state.target.SetActive(state.activeSelf);
        }
    }

    private static bool IsDirectBulletHit(DamageInfo info)
    {
        return string.Equals(info.kind, "bullet", StringComparison.OrdinalIgnoreCase);
    }

    private int GetNowMs()
    {
        float now = useUnscaledTime ? Time.unscaledTime : Time.time;
        return Mathf.Max(0, Mathf.RoundToInt(now * 1000f));
    }

    private string ResolveGlobalKey()
    {
        if (!string.IsNullOrWhiteSpace(openUntilGlobalKey))
            return openUntilGlobalKey;

        if (!string.IsNullOrEmpty(resolvedAutoKey))
            return resolvedAutoKey;

        string sceneName = gameObject.scene.IsValid() ? gameObject.scene.name : "UnknownScene";
        string path = BuildHierarchyPath(transform);
        resolvedAutoKey = $"DamageSwitchTimedWall/{sceneName}/{path}/OpenUntilMs";
        return resolvedAutoKey;
    }

    private static string BuildHierarchyPath(Transform node)
    {
        if (node == null) return "Unknown";
        if (node.parent == null) return node.name;
        return BuildHierarchyPath(node.parent) + "/" + node.name;
    }
}

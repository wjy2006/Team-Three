using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInputReader))]
[RequireComponent(typeof(SpawnOnLoad))]
public class UnstuckRespawn : MonoBehaviour
{
    [Header("Availability")]
    [FormerlySerializedAs("enabledSceneNames")]
    [Tooltip("Only these scenes can use unstuck. Match Scene.name exactly.")]
    public string[] sceneWhitelist;
    [Tooltip("If whitelist is empty: allow in all scenes.")]
    public bool allowWhenListEmpty = false;

    [Header("Spawn")]
    [SerializeField] private string fallbackSpawnId = "default";
    [SerializeField] private bool useNearestSpawnPointWhenUnknown = true;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.1f;
    [SerializeField, Min(0f)] private float fadeInDuration = 0.1f;
    [SerializeField, Min(0f)] private float cooldownSeconds = 0.3f;

    private PlayerInputReader input;
    private SpawnOnLoad spawner;
    private bool running;
    private float nextAllowedTime;

    private void Awake()
    {
        input = GetComponent<PlayerInputReader>();
        spawner = GetComponent<SpawnOnLoad>();
    }

    private void Update()
    {
        if (running || input == null || spawner == null)
            return;

        if (!input.ConsumeUnstuckDown())
            return;

        if (Time.unscaledTime < nextAllowedTime)
            return;

        if (!IsCurrentSceneEnabled())
            return;

        var root = GameRoot.I;
        if (root == null || root.IsTransitioning || root.InputLocked)
            return;

        string spawnId = ResolveTargetSpawnId();
        if (string.IsNullOrEmpty(spawnId))
        {
            Debug.LogWarning("[UnstuckRespawn] No valid spawnId for respawn.");
            return;
        }

        StartCoroutine(RespawnRoutine(spawnId));
    }

    private IEnumerator RespawnRoutine(string spawnId)
    {
        running = true;
        nextAllowedTime = Time.unscaledTime + cooldownSeconds;

        var root = GameRoot.I;
        if (root != null)
            root.SetInputLocked(true);

        try
        {
            if (root != null && root.fade != null)
                yield return root.fade.FadeOut(fadeOutDuration);

            yield return spawner.SpawnTo(spawnId);

            if (root != null && root.cameraFollow != null)
                root.cameraFollow.SnapToTarget();

            if (root != null && root.fade != null)
                yield return root.fade.FadeIn(fadeInDuration);
        }
        finally
        {
            if (root != null)
                root.SetInputLocked(false);
            running = false;
        }
    }

    private bool IsCurrentSceneEnabled()
    {
        if (sceneWhitelist == null || sceneWhitelist.Length == 0)
            return allowWhenListEmpty;

        string current = SceneManager.GetActiveScene().name;
        for (int i = 0; i < sceneWhitelist.Length; i++)
        {
            if (string.Equals(sceneWhitelist[i], current, System.StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private string ResolveTargetSpawnId()
    {
        if (!string.IsNullOrEmpty(spawner.CurrentSpawnId))
            return spawner.CurrentSpawnId;

        if (!string.IsNullOrEmpty(fallbackSpawnId))
            return fallbackSpawnId;

        if (!useNearestSpawnPointWhenUnknown)
            return null;

        var points = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
        if (points == null || points.Length == 0)
            return null;

        Vector2 self = transform.position;
        SpawnPoint best = null;
        float bestDist = float.MaxValue;
        for (int i = 0; i < points.Length; i++)
        {
            var p = points[i];
            if (p == null || string.IsNullOrEmpty(p.spawnId))
                continue;

            float dist = ((Vector2)p.transform.position - self).sqrMagnitude;
            if (dist < bestDist)
            {
                bestDist = dist;
                best = p;
            }
        }

        return best != null ? best.spawnId : null;
    }
}


using Game.Gameplay.Combat;
using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(menuName = "Story/Conditions/Combat/All Enemies Defeated In Scene")]
public class AllEnemiesDefeatedInSceneCondition : StoryCondition
{
    [Header("Enemy Filter")]
    [Tooltip("Only objects on this layer are treated as enemies.")]
    public string enemyLayerName = "Damagable";
    [SerializeField] private bool debugLogs = true;

    public override bool Evaluate(GameEvent evt)
    {
        if (evt is not HealthDeathEvent death) return false;
        if (death.target == null)
        {
            if (debugLogs) Debug.Log("[AllEnemiesDefeatedInSceneCondition] HealthDeathEvent target is null.");
            return false;
        }

        int enemyLayer = LayerMask.NameToLayer(enemyLayerName);
        if (enemyLayer < 0)
        {
            enemyLayer = death.target.layer;
            if (debugLogs)
            {
                Debug.LogWarning(
                    $"[AllEnemiesDefeatedInSceneCondition] Layer '{enemyLayerName}' not found. " +
                    $"Fallback to target layer '{LayerMask.LayerToName(enemyLayer)}'.");
            }
        }
        if (death.target.layer != enemyLayer)
        {
            if (debugLogs)
            {
                Debug.Log($"[AllEnemiesDefeatedInSceneCondition] Ignored death: target={death.target.name}, targetLayer={LayerMask.LayerToName(death.target.layer)}, expectedLayer={enemyLayerName}.");
            }
            return false;
        }

        Scene scene = death.target.scene;
        if (!scene.IsValid())
        {
            if (debugLogs) Debug.Log("[AllEnemiesDefeatedInSceneCondition] Death target scene invalid.");
            return false;
        }

        var allHealth = Object.FindObjectsByType<Health2D>(FindObjectsSortMode.None);
        int enemyTotal = 0;
        int enemyAlive = 0;
        string firstAliveName = null;

        for (int i = 0; i < allHealth.Length; i++)
        {
            var h = allHealth[i];
            if (h == null) continue;

            GameObject go = h.gameObject;
            if (go.layer != enemyLayer) continue;
            if (go.scene != scene) continue;

            enemyTotal++;
            if (h.Current > 0f)
            {
                enemyAlive++;
                if (firstAliveName == null)
                    firstAliveName = go.name;
            }
        }

        bool cleared = enemyAlive == 0;
        if (debugLogs)
        {
            Debug.Log(
                $"[AllEnemiesDefeatedInSceneCondition] scene={scene.name}, enemyTotal={enemyTotal}, enemyAlive={enemyAlive}, cleared={cleared}" +
                (firstAliveName != null ? $", firstAlive={firstAliveName}" : string.Empty));
        }

        return cleared;
    }
}

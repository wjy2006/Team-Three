using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class SpawnOnLoad : MonoBehaviour
{
    private Rigidbody2D rb;
    private Collider2D col;

    [SerializeField] private string currentSpawnId;
    public string CurrentSpawnId => currentSpawnId;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    public IEnumerator SpawnTo(string spawnId)
    {
        if (string.IsNullOrEmpty(spawnId))
            yield break;

        if (col != null)
            col.enabled = false;

        var points = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
        SpawnPoint target = null;
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] != null && points[i].spawnId == spawnId)
            {
                target = points[i];
                break;
            }
        }

        if (target == null)
        {
            Debug.LogError($"SpawnPoint not found: {spawnId}");
            if (col != null)
                col.enabled = true;
            yield break;
        }

        var worldPos = target.transform.position;
        worldPos.z = transform.position.z;

        rb.position = (Vector2)worldPos;
        rb.linearVelocity = Vector2.zero;
        currentSpawnId = spawnId;

        Physics2D.SyncTransforms();
        if (col != null)
            col.enabled = true;

        yield return null;

        SendMessage("OnPostSpawn", SendMessageOptions.DontRequireReceiver);
    }
}


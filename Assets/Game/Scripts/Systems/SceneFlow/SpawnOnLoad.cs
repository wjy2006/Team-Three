using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class SpawnOnLoad : MonoBehaviour
{
    private Rigidbody2D rb;
    private Collider2D col;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    public IEnumerator SpawnTo(string spawnId)
    {
        if (string.IsNullOrEmpty(spawnId)) yield break;
        if (col != null) col.enabled = false;
        var points = FindObjectsOfType<SpawnPoint>();
        SpawnPoint target = null;
        foreach (var p in points)
            if (p.spawnId == spawnId) { target = p; break; }

        if (target == null)
        {
            Debug.LogError($"没找到 SpawnPoint: {spawnId}");
            yield break;
        }

        var wp = target.transform.position;
        wp.z = transform.position.z;
        

        rb.position = (Vector2)wp;
        rb.velocity = Vector2.zero;
        Physics2D.SyncTransforms();
        if (col != null) col.enabled = true;
        
        yield return null;
    }
}

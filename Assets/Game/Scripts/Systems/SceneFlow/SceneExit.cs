using UnityEngine;

public class SceneExit : MonoBehaviour
{
    public string toScene;
    public string toSpawnId;

    private bool isLoading;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isLoading) return;
        if (!other.CompareTag("Player")) return;

        // ⭐ 核心：过渡中任何出口都不响应
        if (GameRoot.I != null && GameRoot.I.IsTransitioning) return;

        isLoading = true;
        GameRoot.I.TransitionTo(toScene, toSpawnId, 0.15f);
    }

}

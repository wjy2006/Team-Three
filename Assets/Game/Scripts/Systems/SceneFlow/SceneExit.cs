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

        isLoading = true;
        GameRoot.I.TransitionTo(toScene, toSpawnId, 0.15f);
    }
}

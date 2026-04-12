using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BootLoader : MonoBehaviour
{
    private const string PlayerHouseSceneName = "Room_Lab_PlayerHouse";
    private static string pendingStartScene;
    private static string pendingStartSpawnId;

    public static void RequestStartGame(string sceneName, string spawnId)
    {
        pendingStartScene = sceneName;
        pendingStartSpawnId = spawnId;
    }

    private void Start()
    {
        if (!string.IsNullOrWhiteSpace(pendingStartScene))
        {
            string targetScene = pendingStartScene;
            string targetSpawn = pendingStartSpawnId;
            pendingStartScene = null;
            pendingStartSpawnId = null;
            bool enteringPlayerHouse =
                string.Equals(targetScene, PlayerHouseSceneName, StringComparison.Ordinal);

            if (GameRoot.I != null)
            {
                GameRoot.I.ResetRuntimeForNewGame();
                ResetPlayerToOriginIfPresent();
                if (enteringPlayerHouse && GameRoot.I.fade != null)
                    GameRoot.I.fade.SetAlpha(1f);

                float fadeOut = enteringPlayerHouse ? 0f : 0.10f;
                GameRoot.I.TransitionTo(targetScene, targetSpawn, fadeOut, 0.10f);
            }
            else
            {
                SceneTransfer.NextSpawnId = targetSpawn;
                SceneManager.LoadScene(targetScene);
            }

            return;
        }

        SceneManager.LoadScene("MainMenu");
    }

    private static void ResetPlayerToOriginIfPresent()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        player.transform.position = new Vector3(0f, 0f, -4f);

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }
}

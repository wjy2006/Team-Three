using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CameraFollow2D))]
public class CameraSceneBinder : MonoBehaviour
{
    private CameraFollow2D follow;

    void Awake()
    {
        follow = GetComponent<CameraFollow2D>();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 1) 找到关卡场景里的 LevelCameraSettings（如果没有就不改）
        var settings = FindAnyObjectByType<LevelCameraSettings>();
        if (settings != null)
        {
            follow.followMode = settings.defaultMode;
            follow.bounds = settings.bounds;

            if (settings.snapOnEnter && follow.target != null)
            {
                // 让相机别从上一个场景“滑过来”，直接到新场景初始位置
                var p = follow.target.position + follow.offset;
                transform.position = new Vector3(p.x, p.y, follow.offset.z);
            }
        }
        else
        {
            // 没有配置就用默认：不改 mode，不改 bounds
            // （你也可以在这里给个默认 bounds = null）
            follow.bounds = null;
        }
    }
}

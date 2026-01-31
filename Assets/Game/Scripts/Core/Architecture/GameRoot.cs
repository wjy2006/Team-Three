using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameRoot : MonoBehaviour
{
    public static GameRoot I { get; private set; }

    [Header("Refs (Boot Scene)")]
    public SpawnOnLoad playerSpawn;
    public CameraFollow2D cameraFollow;
    public FadeController fade;
    public PlayerInputReader playerInput;
    public PlayerInteractor PlayerInteractor { get; private set; }
    public LocalizationService Localization { get; private set; }
    public DialogueSystem Dialogue { get; private set; }
    public GlobalState Global { get; private set; } = new GlobalState();


    [SerializeField] private GameObject player;

    public bool InputLocked { get; private set; }

    public bool IsTransitioning { get; private set; }


    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        Dialogue = GetComponentInChildren<DialogueSystem>(true);

        if (cameraFollow == null)
            cameraFollow = FindFirstObjectByType<CameraFollow2D>();

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
            PlayerInteractor = player.GetComponent<PlayerInteractor>();

        if (PlayerInteractor == null)
            Debug.LogWarning("[GameRoot] PlayerInteractor not found on Player.");
        SceneManager.sceneLoaded += OnSceneLoaded;
        Localization = GetComponentInChildren<LocalizationService>(true);
        Dialogue = GetComponentInChildren<DialogueSystem>(true);
    }

    private void OnDestroy()
    {
        if (I == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 如果正在过场切换，TransitionRoutine 里会自己做相机设置和 Snap，这里不重复
        if (IsTransitioning) return;
        ApplyLevelCameraSettings();
    }

    public void SetInputLocked(bool locked) => InputLocked = locked;

    public void SetMoveLocked(bool locked)
    {
        // locked=true => 关移动；locked=false => 开移动
        if (playerInput != null)
            playerInput.SetMoveEnabled(!locked);
    }

    // ========== 相机设置 ==========
    public void ApplyLevelCameraSettings()
    {
        if (cameraFollow == null)
        {
            Debug.LogWarning("GameRoot: cameraFollow 未绑定，无法应用关卡相机设置");
            return;
        }

        var settings = FindFirstObjectByType<LevelCameraSettings>();
        if (settings == null)
        {
            cameraFollow.SetBounds(null);
            return;
        }

        cameraFollow.SetFollowMode(settings.defaultMode);
        cameraFollow.SetBounds(settings.bounds);

        if (settings.snapOnEnter)
            cameraFollow.SnapToTarget(); // 直接用 Snap 方法
    }



    // ========== 对外切场景入口 ==========
    public void TransitionTo(string toScene, string toSpawnId, float fadeOutTime = 0.12f, float fadeInTime = 0.10f)
    {
        if (IsTransitioning) return;
        StartCoroutine(TransitionRoutine(toScene, toSpawnId, fadeOutTime, fadeInTime));
    }

    private IEnumerator TransitionRoutine(string toScene, string toSpawnId, float fadeOutTime, float fadeInTime)
    {
        IsTransitioning = true;
        SetInputLocked(true);
        SetMoveLocked(true);

        try
        {
            if (Dialogue != null && Dialogue.IsOpen) Dialogue.Close();

            if (fade != null) yield return fade.FadeOut(fadeOutTime);

            SceneTransfer.NextSpawnId = toSpawnId;

            bool alreadyInScene = SceneManager.GetActiveScene().name == toScene;

            if (!alreadyInScene)
            {
                var op = SceneManager.LoadSceneAsync(toScene);
                while (!op.isDone) yield return null;
            }

            yield return null;

            if (playerSpawn != null)
                yield return playerSpawn.SpawnTo(SceneTransfer.NextSpawnId);

            yield return new WaitForFixedUpdate();

            ApplyLevelCameraSettings();
            cameraFollow.SnapToTarget();

            if (fade != null) yield return fade.FadeIn(fadeInTime);
        }
        finally
        {
            SceneTransfer.NextSpawnId = null;
            SetInputLocked(false);
            SetMoveLocked(false);
            IsTransitioning = false;
        }


    }



}

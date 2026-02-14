using System.Collections;
using Game.Core;
using Game.Gameplay.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameRoot : MonoBehaviour
{
    public static GameRoot I { get; private set; }

    [Header("Refs (Boot Scene)")]
    public Game.Systems.Items.Inventory Inventory;
    public SpawnOnLoad playerSpawn;
    public CameraFollow2D cameraFollow;
    public FadeController fade;
    public PlayerInputReader playerInput;
    public HeldItem playerHeldItem;

    [Header("Systems (Boot Scene children)")]
    [SerializeField] private StoryManager storyManager;
    [SerializeField] private LocalizationService localization;
    [SerializeField] private DialogueSystem dialogue;
    [SerializeField] private PauseManager pause;
    [SerializeField] private TriggerManager triggerManager;
    [Header("Runtime (auto found)")]
    [SerializeField] private GameObject player;

    public TriggerManager Triggers => triggerManager;


    public PlayerInteractor PlayerInteractor { get; private set; }

    // 对外暴露（统一口径）
    public StoryManager Story => storyManager;
    public LocalizationService Localization => localization;
    public DialogueSystem Dialogue => dialogue;
    public PauseManager Pause => pause;

    public GlobalState Global { get; private set; } = new GlobalState();

    public bool InputLocked { get; private set; }
    public bool IsTransitioning { get; private set; }


    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;

        // ✅ Boot 内系统：优先用 Inspector 绑定；没绑就从子物体里找
        if (localization == null) localization = GetComponentInChildren<LocalizationService>(true);
        if (dialogue == null) dialogue = GetComponentInChildren<DialogueSystem>(true);
        if (pause == null) pause = GetComponentInChildren<PauseManager>(true);
        if (storyManager == null) storyManager = GetComponentInChildren<StoryManager>(true);
        if (triggerManager == null) triggerManager = GetComponentInChildren<TriggerManager>(true);
        DontDestroyOnLoad(gameObject);
        // 玩家/相机等运行时对象：第一次抓取
        RefreshRuntimeRefs();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (I == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            I = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 场景加载后，刷新一次关卡侧引用（相机/关卡设置/玩家）
        RefreshRuntimeRefs();

        // 如果正在过场切换，TransitionRoutine 里会自己做相机设置和 Snap，这里不重复
        if (IsTransitioning) return;

        ApplyLevelCameraSettings();
    }

    /// <summary>
    /// 刷新会随场景变化的引用：Player、PlayerInteractor、CameraFollow、HeldItem 等
    /// </summary>
    public void RefreshRuntimeRefs()
    {
        // 相机跟随脚本通常在关卡场景相机上（也可能在全局相机上）
        if (cameraFollow == null)
            cameraFollow = FindFirstObjectByType<CameraFollow2D>();

        // Player 通常是 DontDestroyOnLoad；如果不是，也要能在新场景里找到
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            if (playerInput == null)
                playerInput = player.GetComponent<PlayerInputReader>();

            if (playerHeldItem == null)
                playerHeldItem = player.GetComponent<HeldItem>();

            PlayerInteractor = player.GetComponent<PlayerInteractor>();
        }

        // SpawnOnLoad 可能挂在玩家身上
        if (playerSpawn == null && player != null)
            playerSpawn = player.GetComponent<SpawnOnLoad>();

        if (player != null && PlayerInteractor == null)
            Debug.LogWarning("[GameRoot] PlayerInteractor not found on Player.");
    }

    // ========= Input Lock =========
    public void SetInputLocked(bool locked)
    {
        InputLocked = locked;

        // ✅ 如果你 PlayerInputReader 有总开关，建议在这里同步（推荐你实现）
        if (playerInput != null)
            playerInput.SetAllGameplayEnabled(!locked);
    }

    public void SetMoveLocked(bool locked)
    {
        if (playerInput != null)
            playerInput.SetMoveEnabled(!locked);
    }

    // ========== 相机设置 ==========
    public void ApplyLevelCameraSettings()
    {
        if (cameraFollow == null)
        {
            Debug.LogWarning("[GameRoot] cameraFollow 未找到，无法应用关卡相机设置");
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
            cameraFollow.SnapToTarget();
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

        // 过场期间：锁输入/锁移动
        SetInputLocked(true);
        //SetMoveLocked(true);

        try
        {
            if (Dialogue != null && Dialogue.IsOpen)
                Dialogue.Close();

            if (fade != null) yield return fade.FadeOut(fadeOutTime);

            SceneTransfer.NextSpawnId = toSpawnId;

            bool alreadyInScene = SceneManager.GetActiveScene().name == toScene;
            if (!alreadyInScene)
            {
                var op = SceneManager.LoadSceneAsync(toScene);
                while (!op.isDone) yield return null;
            }

            // 等一帧让新场景 Awake/Start 走完
            yield return null;

            // 新场景加载后刷新一次运行时引用（相机/玩家等）
            RefreshRuntimeRefs();

            if (playerSpawn != null && !string.IsNullOrEmpty(SceneTransfer.NextSpawnId))
                yield return playerSpawn.SpawnTo(SceneTransfer.NextSpawnId);

            // 等物理稳定
            yield return new WaitForFixedUpdate();

            ApplyLevelCameraSettings();
            if (cameraFollow != null) cameraFollow.SnapToTarget();
            if (player != null)
            {
                var stats = player.GetComponent<PlayerStats>();
                if (stats != null && stats.IsDead)
                {
                    stats.ReviveToFull();
                }
                I.Triggers.Raise(new DeathEvent());
            }

            if (fade != null) yield return fade.FadeIn(fadeInTime);
        }
        finally
        {
            SceneTransfer.NextSpawnId = null;

            SetInputLocked(false);
            SetMoveLocked(false);
            IsTransitioning = false;
            Triggers.Raise(new SceneEnteredEvent());
        }
    }
    private void OnApplicationQuit()
    {
        I = null;
    }
}

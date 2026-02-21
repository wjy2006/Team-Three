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
    public GlitchVolumeTransition glitchVolume; 

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
    public StoryManager Story => storyManager;
    public LocalizationService Localization => localization;
    public DialogueSystem Dialogue => dialogue;
    public PauseManager Pause => pause;

    public GlobalState Global { get; private set; } = new GlobalState();
    public bool InputLocked { get; private set; }
    public bool IsTransitioning { get; private set; }
    public const string STATE_GLITCH_WORLD = "IsGlitchWorld"; 
    public bool IsGlitchWorld => Global.GetBool(STATE_GLITCH_WORLD);

    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;

        if (localization == null) localization = GetComponentInChildren<LocalizationService>(true);
        if (dialogue == null) dialogue = GetComponentInChildren<DialogueSystem>(true);
        if (pause == null) pause = GetComponentInChildren<PauseManager>(true);
        if (storyManager == null) storyManager = GetComponentInChildren<StoryManager>(true);
        if (triggerManager == null) triggerManager = GetComponentInChildren<TriggerManager>(true);
        
        DontDestroyOnLoad(gameObject);
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
        RefreshRuntimeRefs();
        if (IsTransitioning) return;
        ApplyLevelCameraSettings();
    }

    public void RefreshRuntimeRefs()
    {
        if (cameraFollow == null) cameraFollow = FindFirstObjectByType<CameraFollow2D>();
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            if (playerInput == null) playerInput = player.GetComponent<PlayerInputReader>();
            if (playerHeldItem == null) playerHeldItem = player.GetComponent<HeldItem>();
            PlayerInteractor = player.GetComponent<PlayerInteractor>();
        }

        if (playerSpawn == null && player != null)
            playerSpawn = player.GetComponent<SpawnOnLoad>();

        if (player != null && PlayerInteractor == null)
            Debug.LogWarning("[GameRoot] PlayerInteractor not found on Player.");
    }

    public void SetInputLocked(bool locked)
    {
        InputLocked = locked;
        if (playerInput != null) playerInput.SetAllGameplayEnabled(!locked);
    }

    public void SetMoveLocked(bool locked)
    {
        if (playerInput != null) playerInput.SetMoveEnabled(!locked);
    }

    public void ApplyLevelCameraSettings()
    {
        if (cameraFollow == null) return;
        var settings = FindFirstObjectByType<LevelCameraSettings>();
        if (settings == null) { cameraFollow.SetBounds(null); return; }

        cameraFollow.SetFollowMode(settings.defaultMode);
        cameraFollow.SetBounds(settings.bounds);
        if (settings.snapOnEnter) cameraFollow.SnapToTarget();
    }

    public void TransitionTo(string toScene, string toSpawnId, float fadeOutTime = 0.10f, float fadeInTime = 0.10f)
    {
        if (IsTransitioning) return;
        StartCoroutine(TransitionRoutine(toScene, toSpawnId, fadeOutTime, fadeInTime));
    }

    private IEnumerator TransitionRoutine(string toScene, string toSpawnId, float fadeOutTime, float fadeInTime)
    {
        IsTransitioning = true;
        SetInputLocked(true);

        try
        {
            if (Dialogue != null && Dialogue.IsOpen) Dialogue.Close();

            // ✅ 根据状态决定切场前的视觉表现
            if (IsGlitchWorld)
            {
                if (glitchVolume != null) yield return glitchVolume.GlitchOut();
            }
            else
            {
                if (fade != null) yield return fade.FadeOut(fadeOutTime);
            }

            SceneTransfer.NextSpawnId = toSpawnId;

            // 加载场景逻辑
            if (SceneManager.GetActiveScene().name != toScene)
            {
                var op = SceneManager.LoadSceneAsync(toScene);
                while (!op.isDone) yield return null;
            }

            yield return null; 
            RefreshRuntimeRefs();

            if (playerSpawn != null && !string.IsNullOrEmpty(SceneTransfer.NextSpawnId))
                yield return playerSpawn.SpawnTo(SceneTransfer.NextSpawnId);

            if (Time.timeScale > 0f)
                yield return new WaitForFixedUpdate();
            else
                yield return new WaitForSecondsRealtime(Time.fixedDeltaTime);

            ApplyLevelCameraSettings();
            if (cameraFollow != null) cameraFollow.SnapToTarget();

            // 复活逻辑
            if (player != null)
            {
                var stats = player.GetComponent<PlayerStats>();
                if (stats != null && stats.IsDead) stats.ReviveToFull();
                I.Triggers.Raise(new DeathEvent());
            }

            // ✅ 根据状态决定切场后的视觉表现
            if (IsGlitchWorld)
            {
                if (glitchVolume != null) yield return glitchVolume.GlitchIn();
            }
            else
            {
                if (fade != null) yield return fade.FadeIn(fadeInTime);
            }
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

    private void OnApplicationQuit() { I = null; }
}
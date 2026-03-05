using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Systems.Items;
using Game.Gameplay.Player;
using Game.Systems.Items.Runtime;

[RequireComponent(typeof(HeldItem))]
[RequireComponent(typeof(TopDownMove2D))]
[RequireComponent(typeof(RuntimeItemStateStore))]
[RequireComponent(typeof(PlayerStats))]
public class RocketRideController : MonoBehaviour
{
    private HeldItem held;
    private TopDownMove2D move;
    private PlayerInputReader input;
    private PlayerStats stats;
    private RuntimeItemStateStore stateStore;

    private RocketMountEntity rocket;
    private RocketRideEffect cfg;

    private string ridingInstanceId;
    private ItemDefinition ridingItemDef;
    private SpriteRenderer[] cachedRenderers;

    // ✅ 切换/重建时排队（避免 Destroy 延迟导致双火箭）
    private bool switchPending;
    private ItemInstance pendingInst;
    private ItemDefinition pendingItemDef;
    private RocketRideEffect pendingEffect;

    // ✅ 供 RocketMountEntity.OnDestroy 判断：切换中不要还原玩家刚体
    public bool IsSwitchingRockets() => switchPending;

    [Header("Delay bind in these scenes (e.g. Shop)")]
    [Tooltip("这些场景里：第一次拿到火箭不会立刻接管玩家，而是等 SpawnOnLoad 的 OnPostSpawn（避免卡 Exit Trigger）。\n正常关卡不填，普通物品切火箭会立刻接管。")]
    [SerializeField] private string[] delayBindSceneNames;

    private void Awake()
    {
        held = GetComponent<HeldItem>();
        move = GetComponent<TopDownMove2D>();
        stats = GetComponent<PlayerStats>();

        stateStore = GetComponent<RuntimeItemStateStore>();
        if (stateStore == null) stateStore = gameObject.AddComponent<RuntimeItemStateStore>();
    }

    /// <summary>
    /// SpawnOnLoad.SpawnTo() 完成后会在 Player 上 SendMessage("OnPostSpawn")
    /// 火箭是独立物体收不到，必须由 Controller 转发
    /// </summary>
    private void OnPostSpawn()
    {
        if (rocket != null)
            rocket.OnPostSpawn();
    }

    private void Update()
    {
        // 0) 输入引用
        if (input == null)
        {
            if (GameRoot.I != null) input = GameRoot.I.playerInput;
            if (input == null) return;
        }

        // ✅ 等旧火箭销毁期间不要生成新火箭
        if (switchPending)
        {
            if (rocket != null)
            {
                bool accelHeld = input.ClickHeld;
                Vector2 aimDir = GetAimDir(rocket.transform.position);
                rocket.SetInput(aimDir, accelHeld);
            }
            return;
        }

        // 1) 当前手持实例与效果
        ItemInstance inst = held != null ? held.heldInstance : null;
        ItemDefinition itemDef = inst != null ? inst.Definition : (held != null ? held.held : null);
        RocketRideEffect effect = itemDef != null ? itemDef.Effect as RocketRideEffect : null;

        // 2) 不拿火箭：销毁并恢复
        if (effect == null)
        {
            if (rocket != null)
            {
                if (!string.IsNullOrEmpty(ridingInstanceId))
                    stateStore.SetInt(ridingInstanceId, rocket.GetCurrentHp());

                rocket.gameObject.SetActive(false);
                Destroy(rocket.gameObject);
                rocket = null;
            }

            cfg = null;
            ridingItemDef = null;
            ridingInstanceId = null;

            RestorePlayer();
            return;
        }

        // 3) 开始/切换骑乘
        if (rocket == null)
        {
            // ✅ 关键修复：普通物品切火箭（非商店）要立刻绑定；商店场景才延后绑定
            bool bindImmediately = ShouldBindImmediately();
            StartRide(inst, itemDef, effect, bindImmediately);
        }
        else
        {
            string curId = inst != null ? inst.InstanceId : null;

            if (!string.Equals(curId, ridingInstanceId))
            {
                QueueSwitchTo(inst, itemDef, effect);
            }
        }

        // 4) 输入驱动
        if (rocket != null)
        {
            bool accelHeld = input.ClickHeld;
            Vector2 aimDir = GetAimDir(rocket.transform.position);
            rocket.SetInput(aimDir, accelHeld);
        }
    }

    private bool ShouldBindImmediately()
    {
        // 转场期间绝对不绑
        if (GameRoot.I != null && GameRoot.I.IsTransitioning)
            return false;

        // 商店/指定场景：第一次拿火箭不立刻绑，等 SpawnOnLoad 的 OnPostSpawn
        string sceneName = SceneManager.GetActiveScene().name;
        if (delayBindSceneNames != null)
        {
            for (int i = 0; i < delayBindSceneNames.Length; i++)
            {
                if (!string.IsNullOrEmpty(delayBindSceneNames[i]) && delayBindSceneNames[i] == sceneName)
                    return false;
            }
        }

        // 其它正常场景：立刻绑定（否则普通物品切火箭永远等不到 OnPostSpawn）
        return true;
    }

    private void QueueSwitchTo(ItemInstance nextInst, ItemDefinition nextDef, RocketRideEffect nextEffect)
    {
        if (rocket != null && !string.IsNullOrEmpty(ridingInstanceId))
            stateStore.SetInt(ridingInstanceId, rocket.GetCurrentHp());

        switchPending = true;
        pendingInst = nextInst;
        pendingItemDef = nextDef;
        pendingEffect = nextEffect;

        rocket.gameObject.SetActive(false);
        Destroy(rocket.gameObject);

        rocket = null;
        cfg = null;
        ridingItemDef = null;
        ridingInstanceId = null;
    }

    private Vector2 GetAimDir(Vector2 origin)
    {
        var cam = Camera.main;
        if (cam == null) return Vector2.right;

        Vector2 screenPos = input.PointerPos;
        Vector3 worldPoint = cam.ScreenToWorldPoint(new Vector3(
            screenPos.x,
            screenPos.y,
            Mathf.Abs(cam.transform.position.z)
        ));
        Vector2 mouseWorld = (Vector2)worldPoint;

        Vector2 dir = mouseWorld - origin;
        if (dir.sqrMagnitude < 0.0001f) return Vector2.right;
        return dir.normalized;
    }

    private void StartRide(ItemInstance inst, ItemDefinition itemDef, RocketRideEffect effect, bool bindImmediately)
    {
        cfg = effect;
        ridingItemDef = itemDef;
        ridingInstanceId = inst != null ? inst.InstanceId : null;

        if (cfg == null || cfg.rocketPrefab == null)
        {
            Debug.LogError("[RocketRide] RocketRideEffect 或 rocketPrefab 为空");
            return;
        }

        var go = Instantiate(cfg.rocketPrefab, transform.position, Quaternion.identity);
        rocket = go.GetComponent<RocketMountEntity>();
        if (rocket == null)
        {
            Debug.LogError("[RocketRide] rocketPrefab 上缺少 RocketMountEntity");
            Destroy(go);
            return;
        }

        int startHp = cfg.rocketHp;
        if (!string.IsNullOrEmpty(ridingInstanceId))
            startHp = stateStore.GetInt(ridingInstanceId, cfg.rocketHp);

        rocket.Attach(
            playerGO: gameObject,
            playerStats: stats,
            controller: this,
            effect: cfg,
            sourceItem: itemDef,
            instanceId: ridingInstanceId,
            startHp: startHp
        );

        if (GameRoot.I != null) GameRoot.I.SetMoveLocked(true);
        if (move != null) move.canMove = false;
        ShowPlayer(false);

        // ✅ 非商店场景：立刻接管（解决“普通物品切火箭不跟随”）
        if (bindImmediately && rocket != null)
        {
            rocket.BindNowIfSafe(); // 你之前那版 RocketMountEntity 里已经有
        }
        // 商店场景：不立刻接管，等 SpawnOnLoad 的 OnPostSpawn
    }

    public void OnRocketFinished(ItemDefinition sourceItem, string instanceId, bool consumeHeldItem)
    {
        if (!string.IsNullOrEmpty(instanceId))
        {
            if (consumeHeldItem) stateStore.Remove(instanceId);
            else if (rocket != null) stateStore.SetInt(instanceId, rocket.GetCurrentHp());
        }

        if (consumeHeldItem)
        {
            switchPending = false;
            pendingInst = null;
            pendingItemDef = null;
            pendingEffect = null;

            rocket = null;
            cfg = null;
            ridingItemDef = null;
            ridingInstanceId = null;

            RestorePlayer();

            if (held != null)
                held.SetHeld(null);

            return;
        }

        rocket = null;
        cfg = null;
        ridingItemDef = null;
        ridingInstanceId = null;

        if (switchPending)
        {
            var ni = pendingInst;
            var nd = pendingItemDef;
            var ne = pendingEffect;

            switchPending = false;
            pendingInst = null;
            pendingItemDef = null;
            pendingEffect = null;

            // 切换出来的新火箭：立即绑定（不依赖 OnPostSpawn）
            StartRide(ni, nd, ne, bindImmediately: true);
        }
        else
        {
            RestorePlayer();
        }
    }

    private void RestorePlayer()
    {
        if (GameRoot.I != null) GameRoot.I.SetMoveLocked(false);
        if (move != null) move.canMove = true;
        ShowPlayer(true);
    }

    private void ShowPlayer(bool show)
    {
        if (cachedRenderers == null || cachedRenderers.Length == 0)
            cachedRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        for (int i = 0; i < cachedRenderers.Length; i++)
            cachedRenderers[i].enabled = show;
    }
}
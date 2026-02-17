using System.Collections;
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

    private string ridingInstanceId;     // 当前骑乘绑定的实例ID
    private ItemDefinition ridingItemDef; // 当前骑乘的物品定义（方便比对/回调）
    private SpriteRenderer[] cachedRenderers;

    private void Awake()
    {
        held = GetComponent<HeldItem>();
        move = GetComponent<TopDownMove2D>();
        stats = GetComponent<PlayerStats>();

        stateStore = GetComponent<RuntimeItemStateStore>();
        if (stateStore == null) stateStore = gameObject.AddComponent<RuntimeItemStateStore>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (rocket == null) return;
        StartCoroutine(SnapRocketNextFrame());
    }

    private IEnumerator SnapRocketNextFrame()
    {
        yield return null; // 让 SpawnOnLoad 先把玩家放到最终位置
        if (rocket == null) yield break;
        rocket.SnapToPlayer();
    }

    // SpawnOnLoad 里 SendMessage("OnPostSpawn") 会自动调用这个（同一GO上）
    private void OnPostSpawn()
    {
        if (rocket != null)
            rocket.SnapToPlayer();
    }

    private void Update()
    {
        if (input == null)
        {
            if (GameRoot.I != null) input = GameRoot.I.playerInput;
            if (input == null) return;
        }

        // ✅ 一律以 heldInstance 为准（有实例就用实例；没有就退化成 definition 模式）
        ItemInstance inst = held != null ? held.heldInstance : null;
        ItemDefinition itemDef = inst != null ? inst.Definition : (held != null ? held.held : null);
        RocketRideEffect effect = itemDef != null ? itemDef.Effect as RocketRideEffect : null;

        // 1) 不再手持火箭：如果正在骑乘，收起并写回 hp
        if (effect == null)
        {
            if (rocket != null)
            {
                // ✅ 收起写回 HP（只有有实例ID才写回）
                if (!string.IsNullOrEmpty(ridingInstanceId))
                    stateStore.SetInt(ridingInstanceId, rocket.GetCurrentHp());

                Destroy(rocket.gameObject); // RocketMountEntity.OnDestroy 会恢复锁定/显示（双保险）
                rocket = null;
            }

            RestorePlayer();
            return;
        }

        // 2) 手持火箭：没有火箭就生成（✅ 不需要点击）
        if (rocket == null)
        {
            StartRide(inst, itemDef, effect);
            if (rocket == null) return;
        }
        else
        {
            // ✅ 如果手持换成“另一份火箭实例”，就先收起旧的再生成新的
            string curId = inst != null ? inst.InstanceId : null;
            if (!string.Equals(curId, ridingInstanceId))
            {
                if (!string.IsNullOrEmpty(ridingInstanceId))
                    stateStore.SetInt(ridingInstanceId, rocket.GetCurrentHp());

                Destroy(rocket.gameObject);
                rocket = null;

                StartRide(inst, itemDef, effect);
                if (rocket == null) return;
            }
        }

        // 3) 输入喂给火箭：按住左键才加速
        bool accelHeld = input.ClickHeld;
        Vector2 aimDir = GetAimDir();
        rocket.SetInput(aimDir, accelHeld);
    }

    private void StartRide(ItemInstance inst, ItemDefinition itemDef, RocketRideEffect effect)
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

        // ✅ 从 stateStore 读“这份实例”的剩余 hp（没有记录则用满血）
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

        // ✅ ban 方向键移动
        if (GameRoot.I != null) GameRoot.I.SetMoveLocked(true);
        if (move != null) move.canMove = false;

        // ✅ 只隐藏渲染，不关 collider（Trigger 还能用）
        ShowPlayer(false);
    }

    /// <summary>
    /// RocketMountEntity 在爆炸/销毁时调用
    /// </summary>
    public void OnRocketFinished(ItemDefinition sourceItem, string instanceId, bool consumeHeldItem)
    {
        // ✅ 先处理状态（这时 rocket/cfg 可能还有效）
        if (!string.IsNullOrEmpty(instanceId))
        {
            if (consumeHeldItem)
            {
                stateStore.Remove(instanceId);
            }
            else
            {
                // 正常结束（比如外部 Destroy），尽量写回当前 hp
                if (rocket != null)
                    stateStore.SetInt(instanceId, rocket.GetCurrentHp());
            }
        }

        // ✅ 清引用（在处理状态之后）
        rocket = null;
        cfg = null;
        ridingItemDef = null;
        ridingInstanceId = null;

        RestorePlayer();

        // ✅ 爆炸消耗：从手里彻底消失（同时清实例）
        if (consumeHeldItem && held != null)
        {
            held.SetHeld(null);
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

    private Vector2 GetAimDir()
    {
        var cam = Camera.main;
        if (cam == null) return Vector2.right;

        Vector2 screen = input.PointerPos;
        Vector3 wp3 = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
        Vector2 mouseWorld = new Vector2(wp3.x, wp3.y);

        Vector2 origin = transform.position;
        Vector2 dir = mouseWorld - origin;
        if (dir.sqrMagnitude < 0.0001f) return Vector2.right;
        return dir.normalized;
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Systems.Items;
using Game.Gameplay.Player;

[RequireComponent(typeof(HeldItem))]
[RequireComponent(typeof(TopDownMove2D))]
public class RocketRideController : MonoBehaviour
{
    private HeldItem held;
    private TopDownMove2D move;
    private PlayerInputReader input;
    private PlayerStats stats;

    private RocketMountEntity rocket;
    private RocketRideEffect cfg;
    private ItemDefinition rocketItem;

    private SpriteRenderer[] cachedRenderers;

    private void Awake()
    {
        held = GetComponent<HeldItem>();
        move = GetComponent<TopDownMove2D>();
        stats = GetComponent<PlayerStats>();
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
        // ✅ 让 SpawnOnLoad / 场景出生点逻辑先把玩家放到最终位置
        yield return null;

        if (rocket == null) yield break;

        // ✅ 特判：场景切换后火箭要跟到玩家（并清速度，避免瞬移/漂移）
        rocket.SnapToPlayer();
    }

    private void Update()
    {
        if (input == null)
        {
            if (GameRoot.I != null) input = GameRoot.I.playerInput;
            if (input == null) return;
        }

        var item = held != null ? held.held : null;
        var effect = (item != null) ? item.Effect as RocketRideEffect : null;

        // 1) 不再手持火箭：火箭必须消失
        if (effect == null)
        {
            if (rocket != null)
            {
                Destroy(rocket.gameObject); // OnDestroy 会通知恢复移动/显示
                rocket = null;
            }

            // 双保险恢复
            if (GameRoot.I != null) GameRoot.I.SetMoveLocked(false);
            if (move != null) move.canMove = true;
            ShowPlayer(true);

            return;
        }

        // 2) 手持火箭：没有火箭就生成（✅ 不需要点击）
        if (rocket == null)
        {
            StartRide(item, effect);
            if (rocket == null) return;
        }

        // 3) 输入喂给火箭：按住左键才加速
        bool accelHeld = input.ClickHeld;
        Vector2 aimDir = GetAimDir();
        rocket.SetInput(aimDir, accelHeld);
    }

    private void StartRide(ItemDefinition item, RocketRideEffect effect)
    {
        rocketItem = item;
        cfg = effect;

        if (cfg.rocketPrefab == null)
        {
            Debug.LogError("[RocketRide] RocketRideEffect.rocketPrefab 为空");
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

        rocket.Attach(
            playerGO: gameObject,
            playerStats: stats,
            controller: this,
            effect: cfg,
            sourceItem: rocketItem
        );

        // ✅ ban 方向键移动
        if (GameRoot.I != null) GameRoot.I.SetMoveLocked(true);
        if (move != null) move.canMove = false;

        // ✅ 只隐藏渲染，不关 collider（Trigger 还能用）
        ShowPlayer(false);
    }

    public void OnRocketFinished(ItemDefinition sourceItem, bool consumeHeldItem)
    {
        // 先恢复移动和显示
        if (GameRoot.I != null) GameRoot.I.SetMoveLocked(false);
        if (move != null) move.canMove = true;
        ShowPlayer(true);

        rocket = null;
        cfg = null;
        rocketItem = null;

        // ✅ 爆炸后从手上消失（真正清空 HeldItem）
        if (consumeHeldItem && held != null && held.held == sourceItem)
            held.held = null;
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
    private void OnPostSpawn()
    {
        if (rocket != null)
            rocket.SnapToPlayer();
    }

}

using UnityEngine;
using Game.Systems.Items;

[RequireComponent(typeof(Collider2D))]
public class KeyDoor2D : MonoBehaviour
{
    [Header("Blocking Collider")]
    [Tooltip("挡路的 Collider。为空就用本物体上的 Collider2D")]
    public Collider2D blockingCollider;

    [Header("Visual (Renderers)")]
    [Tooltip("要一起隐藏的渲染器。不填就自动抓取本物体及其子物体上的所有 Renderer")]
    public Renderer[] renderersToHide;

    [Header("One Key One Door")]
    [Tooltip("唯一对应的钥匙（ItemDefinition）")]
    public ItemDefinition requiredKey;

    [Tooltip("开门后是否消耗钥匙（从背包移除一份）")]
    public bool consumeKey = true;

    [Header("Persist (Optional)")]
    [Tooltip("填了就跨场景记住开门：GlobalState bool key")]
    public string openedGlobalKey;

    private bool isOpen;

    private void Awake()
    {
        if (blockingCollider == null)
            blockingCollider = GetComponent<Collider2D>();

        if (renderersToHide == null || renderersToHide.Length == 0)
            renderersToHide = GetComponentsInChildren<Renderer>(true);

        // 从全局状态恢复
        if (!string.IsNullOrEmpty(openedGlobalKey) && GameRoot.I != null)
            isOpen = GameRoot.I.Global.GetBool(openedGlobalKey);

        ApplyState();
    }

    public bool CanOpen()
    {
        if (isOpen) return false;

        // 没配钥匙 => 当作随便开（也可以改成 return false）
        if (requiredKey == null) return true;

        // ✅ 一一对应：必须拥有这把钥匙（背包 或 手持）
        return HasKey(requiredKey);
    }

    private bool HasKey(ItemDefinition key)
    {
        if (key == null) return true;

        // 1) 背包
        var inv = GameRoot.I != null ? GameRoot.I.Inventory : null;
        if (inv != null && inv.Contains(key))
            return true;

        // 2) 手持
        var held = GameRoot.I != null ? GameRoot.I.playerHeldItem : null;
        return held != null && held.held == key;
    }

    public bool TryOpen()
    {
        if (!CanOpen()) return false;

        if (consumeKey && requiredKey != null)
            if (!TryConsumeKey(requiredKey)) return false;

        Open();
        return true;
    }

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;
        ApplyState();

        if (!string.IsNullOrEmpty(openedGlobalKey))
            GameRoot.I.Global?.SetBool(openedGlobalKey, true);
    }

    private void ApplyState()
    {
        // 开门 = 关 collider
        if (blockingCollider != null)
            blockingCollider.enabled = !isOpen;

        // 开门 = 关 renderer（隐藏门）
        if (renderersToHide != null)
        {
            bool visible = !isOpen;
            for (int i = 0; i < renderersToHide.Length; i++)
            {
                if (renderersToHide[i] != null)
                    renderersToHide[i].enabled = visible;
            }
        }
    }

    private bool TryConsumeKey(ItemDefinition key)
    {
        // 优先消耗手持
        var held = GameRoot.I != null ? GameRoot.I.playerHeldItem : null;
        if (held != null && held.held == key)
        {
            held.SetHeld(null);
            held.held = null; // 兼容旧代码：确保立刻清空
            return true;
        }

        // 否则消耗背包
        var inv = GameRoot.I != null ? GameRoot.I.Inventory : null;
        if (inv == null) return false;
        return inv.RemoveOne(key);
    }

    public bool IsOpen => isOpen;
}
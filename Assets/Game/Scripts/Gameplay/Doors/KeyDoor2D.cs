using UnityEngine;
using Game.Systems.Items;

[RequireComponent(typeof(Collider2D))]
public class KeyDoor2D : MonoBehaviour
{
    [Header("Blocking Collider")]
    [Tooltip("挡路的 Collider。为空就用本物体上的 Collider2D")]
    public Collider2D blockingCollider;

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

        // ✅ 一一对应：必须拥有这把钥匙
        var inv = GameRoot.I != null ? GameRoot.I.Inventory : null;
        return inv != null && inv.Contains(requiredKey);
    }

    public bool TryOpen()
    {
        if (!CanOpen()) return false;

        // ✅ 消耗钥匙：从背包移除一份（可选）
        if (consumeKey && requiredKey != null)
        {
            var inv = GameRoot.I != null ? GameRoot.I.Inventory : null;
            if (inv == null) return false;

            if (!inv.RemoveOne(requiredKey))
                return false; // 理论上不会发生（因为 CanOpen 已检查）
        }

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
        if (blockingCollider != null)
            blockingCollider.enabled = !isOpen; // 开门=关 collider
    }

    public bool IsOpen => isOpen;
}

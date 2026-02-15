using Game.Systems.Items;
using UnityEngine;

[System.Serializable]
public class BuySlot
{
    public ItemDefinition item;

    [Header("Limit (default unlimited)")]
    public bool limited = false;
    [Min(1)] public int maxCount = 1;

    [Tooltip("存到 GlobalState 的计数 key（建议每个shop唯一，比如 shop.town.potion）")]
    public string boughtCountGlobalKey;

    [Header("Sold Out Display")]
    [Tooltip("卖光后，商品名显示这个 key（比如“卖光了”）。为空则显示“——”】【不会硬编码loc key】")]
    public string soldOutNameKey;

    [Tooltip("卖光后，购买时在 Hint 显示的 key（可空，空则用通用 soldOutHintKey）")]
    public string soldOutHintKey;
}

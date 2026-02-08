using UnityEngine;
using Game.Systems.Items;

[CreateAssetMenu(menuName = "Story/Conditions/Item/Used Held Item")]
public class UsedItemCondition : StoryCondition
{
    [Header("Item (物品)")]
    public ItemDefinition itemMustBe; // 为空 = 不限制

    public override bool Evaluate(GameEvent evt)
    {
        if (evt is not HeldItemUsedEvent e) return false;

        if (itemMustBe != null && e.item != itemMustBe) return false;

        return true;
    }
}

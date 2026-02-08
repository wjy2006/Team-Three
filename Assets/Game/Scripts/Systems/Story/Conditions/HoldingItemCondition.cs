using Game.Systems.Items;
using UnityEngine;

[CreateAssetMenu(menuName = "Story/Conditions/HoldingItem")]
public class HoldingItemCondition : StoryCondition
{
    public ItemDefinition item;

    public override bool Evaluate(GameEvent evt)
    {
        var held = GameRoot.I.playerHeldItem;
        return held != null && held.held == item;
    }
}


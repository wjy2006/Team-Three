using System;
using System.Collections;
using UnityEngine;
using Game.Systems.Items;
using Game.Systems.Items.Runtime;

[Serializable]
public class SetItemBuyPriceStep : StoryStep
{
    public ItemDefinition item;
    [Min(0)] public int buyPrice = 0;

    public override IEnumerator Play(StoryContext ctx)
    {
        if (item == null) yield break;
        RuntimeItemPriceOverrides.SetBuyPrice(item, buyPrice);
    }
}

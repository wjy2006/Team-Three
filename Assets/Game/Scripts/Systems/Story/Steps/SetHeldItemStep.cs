using System;
using System.Collections;
using Game.Gameplay.Combat.Enemies;
using Game.Gameplay.Player;
using Game.Systems.Items;
using Game.Systems.Items.Runtime;
using UnityEngine;

[Serializable]
public class SetHeldItemStep : StoryStep
{
    public HeldItem target;
    public ItemDefinition item;
    public bool clear;

    public override IEnumerator Play(StoryContext ctx)
    {
        if (target == null) yield break;

        if (clear || item == null)
        {
            target.held = null;
            target.heldInstance = null;
            RefreshVisuals(ctx, target);
            yield break;
        }

        target.held = item;
        target.heldInstance = new ItemInstance(item);
        RefreshVisuals(ctx, target);

        yield break;
    }

    private static void RefreshVisuals(StoryContext ctx, HeldItem held)
    {
        if (held == null) return;

        var enemyVis = held.GetComponent<EnemyHeldItemVisualController>();
        if (enemyVis != null) enemyVis.RefreshNow();

        if (ctx?.Root != null && ctx.Root.playerHeldItem == held && ctx.Root.vis != null)
            ctx.Root.vis.RefreshNow();
    }
}

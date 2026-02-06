using Game.Systems.Items;
using UnityEngine;

public abstract class StoryCondition : ScriptableObject
{
    public abstract bool Evaluate(GameRoot root);
}

[CreateAssetMenu(menuName="Game/Story/Conditions/Has Flag")]
public class HasFlagCondition : StoryCondition
{
    public string flag;
    public override bool Evaluate(GameRoot root) => root.Blackboard.HasFlag(flag);
}

[CreateAssetMenu(menuName="Game/Story/Conditions/Held Item Is")]
public class HeldItemIsCondition : StoryCondition
{
    public ItemDefinition item;
    public override bool Evaluate(GameRoot root)
        => root?.playerHeldItem != null && root.playerHeldItem.held == item;
}

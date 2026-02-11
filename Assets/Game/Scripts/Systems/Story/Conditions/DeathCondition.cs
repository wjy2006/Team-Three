using UnityEngine;

[CreateAssetMenu(menuName = "Story/Conditions/Death")]
public class DeathCondition : StoryCondition
{
    public override bool Evaluate(GameEvent evt)
    {
        return evt is DeathEvent;
    }
}

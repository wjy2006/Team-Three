using UnityEngine;

[CreateAssetMenu(menuName = "Story/Conditions/Trigger/In Trigger")]
public class InTriggerCondition : StoryCondition
{
    public string triggerIdMustBe;

    public override bool Evaluate(GameEvent evt)
    {
        if (evt is not EnterTriggerEvent e) return false;
        if (!string.IsNullOrEmpty(triggerIdMustBe) &&
            e.triggerId != triggerIdMustBe)
            return false;

        return true;
    }

}

using UnityEngine;

[CreateAssetMenu(menuName = "Story/Conditions/Combat/Npc Dodge")]
public class NpcDodgeCondition : StoryCondition
{
    public string targetNpcId;

    public override bool Evaluate(GameEvent evt)
    {
        if (evt is not NpcDodgeEvent e) return false;
        
        // 如果指定了 ID，则检查是否匹配
        if (!string.IsNullOrEmpty(targetNpcId) && e.npcId != targetNpcId)
            return false;

        return true;
    }
}
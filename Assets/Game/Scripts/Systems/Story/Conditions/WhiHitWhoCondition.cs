using UnityEngine;

[CreateAssetMenu(menuName = "Story/Conditions/Damaged/Who Hit Who")]
public class WhoHitWhoCondition : StoryCondition
{
    [Header("Target (被打者)")]
    public GameObject targetMustBe;   // 为空 = 不限制

    [Header("Source (攻击者)")]
    public GameObject sourceMustBe;   // 为空 = 不限制

    public override bool Evaluate(GameEvent evt)
    {
        if (evt is not DamagedEvent e) return false;

        if (targetMustBe != null && e.target != targetMustBe) return false;
        if (sourceMustBe != null && e.source != sourceMustBe) return false;

        return true;
    }
}

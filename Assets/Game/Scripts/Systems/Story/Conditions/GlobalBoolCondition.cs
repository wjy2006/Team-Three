using UnityEngine;

[CreateAssetMenu(menuName = "Story/Conditions/GlobalBool")]
public class GlobalBoolCondition : StoryCondition
{
    public string key;
    public bool expectedValue = true;

    public override bool Evaluate(GameEvent evt)
    {
        if (GameRoot.I == null) return false;
        return GameRoot.I.Global.GetBool(key) == expectedValue;
    }
}

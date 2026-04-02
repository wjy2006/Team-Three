using UnityEngine;

public enum ActorType
{
    Any,            // 不限
    Player,         // 玩家 (通过 GameRoot 或 Tag 判断)
    ByTag,          // 按标签匹配
    ByName,         // 按名字匹配
}

[CreateAssetMenu(menuName = "Story/Conditions/Damaged/Who Hit Who")]
public class WhoHitWhoCondition : StoryCondition
{
    [Header("Target (被打者)")]
    public ActorType targetType = ActorType.Any;
    public string targetId; // 当类型为 Tag 或 Name 时使用

    [Header("Source (攻击者)")]
    public ActorType sourceType = ActorType.Any;
    public string sourceId; // 当类型为 Tag 或 Name 时使用

    public override bool Evaluate(GameEvent evt)
    {
        if (evt is not DamagedEvent e) return false;

        if (!CheckMatch(e.target, targetType, targetId)) return false;
        if (!CheckMatch(e.source, sourceType, sourceId)) return false;

        return true;
    }

    private bool CheckMatch(GameObject obj, ActorType type, string id)
    {
        if (type == ActorType.Any) return true;
        if (obj == null) return false;

        return type switch
        {
            ActorType.Player => obj.CompareTag("Player"), // 或者判断 obj == GameRoot.I.Player
            ActorType.ByTag => obj.CompareTag(id),
            ActorType.ByName => obj.name == id,
            _ => false
        };
    }
}
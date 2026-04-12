using Game.Gameplay.Combat;
using Game.Systems.Items;
using UnityEngine;

public abstract class GameEvent
{
}

public class ItemUsedEvent : GameEvent
{
    public ItemDefinition item;
    public ItemUsedEvent(ItemDefinition item)
    {
        this.item = item;
    }
}


public sealed class DamagedEvent : GameEvent
{
    public readonly GameObject target; // 被打的角色（就是挂 HP 的那个）
    public readonly GameObject source; // 打人的角色（可能为 null）
    public readonly DamageInfo info;

    public DamagedEvent(GameObject target, DamageInfo info)
    {
        this.target = target;
        this.info = info;
        source = info.source;
    }
}

public sealed class HealthDeathEvent : GameEvent
{
    public readonly GameObject target;
    public readonly GameObject source;
    public readonly DamageInfo info;

    public HealthDeathEvent(GameObject target, DamageInfo info)
    {
        this.target = target;
        this.info = info;
        source = info.source;
    }
}

public class InteractEvent : GameEvent
{
    public GameObject target;
    public InteractEvent(GameObject target)
    {
        this.target = target;
    }
}
public sealed class HeldItemUsedEvent : GameEvent
{
    public readonly ItemDefinition item; 

    public HeldItemUsedEvent(ItemDefinition item)
    {
        this.item = item;
    }
}
public sealed class HeldItemChangedEvent : GameEvent
{
    // 空事件，不带数据
}
public sealed class SceneEnteredEvent : GameEvent
{
    // 空事件，仅表示：当前场景刚刚进入
}
public class EnterTriggerEvent : GameEvent
{
    public GameObject player;
    public string triggerId;

    public EnterTriggerEvent(GameObject player, string id)
    {
        this.player = player;
        this.triggerId = id;
    }
}
public class DeathEvent : GameEvent
{
    
}

public sealed class NpcDodgeEvent : GameEvent
{
    public readonly string npcId;
    public readonly GameObject npcObject;

    public NpcDodgeEvent(string npcId, GameObject npcObject)
    {
        this.npcId = npcId;
        this.npcObject = npcObject;
    }
}

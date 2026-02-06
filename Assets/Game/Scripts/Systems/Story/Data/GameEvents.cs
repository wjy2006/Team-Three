using Game.Systems.Items;
using UnityEngine;

public static class GameEvents
{
    public static System.Action<GameObject, float> OnDamaged;      // (who, amount)
    public static System.Action<string> OnSceneEntered;            // sceneName
    public static System.Action<ItemDefinition> OnItemUsed;        // item
}

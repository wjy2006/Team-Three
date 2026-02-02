using UnityEngine;

namespace Game.Systems.Items
{
    public abstract class ItemEffect : ScriptableObject
    {
        // 返回 true 表示该物品应该被消耗（从背包移除）
        public abstract bool Apply(GameObject user);
    }
}

using UnityEngine;

namespace Game.Systems.Items
{
    public abstract class ItemEffect : ScriptableObject
    {
        public abstract bool Apply(ItemUseContext ctx);
    }
}

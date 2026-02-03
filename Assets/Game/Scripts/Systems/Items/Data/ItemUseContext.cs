using UnityEngine;

namespace Game.Systems.Items
{
    public struct ItemUseContext
    {
        public GameObject user;
        public Vector2 aimWorldPos;
        public Vector2 aimDir;
        public ItemDefinition item;
    }
    
}

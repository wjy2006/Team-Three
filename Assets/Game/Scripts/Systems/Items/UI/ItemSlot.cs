using System;
using UnityEngine;

namespace Game.Systems.Items
{
    [Serializable]
    public class ItemSlot
    {
        [SerializeField] private ItemDefinition item;

        public ItemDefinition Item => item;
        public bool IsEmpty => item == null;

        public ItemSlot() { item = null; }
        public ItemSlot(ItemDefinition item) { this.item = item; }

        public void Clear() => item = null;
        public void Set(ItemDefinition newItem) => item = newItem;
    }
}

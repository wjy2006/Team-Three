using System.Collections.Generic;
using UnityEngine;

namespace Game.Systems.Items.Runtime
{
    public class InventoryRuntime : MonoBehaviour
    {
        [SerializeField] private List<ItemInstance> items = new();

        public IReadOnlyList<ItemInstance> Items => items;

        public ItemInstance Add(ItemDefinition def)
        {
            var inst = new ItemInstance(def);
            items.Add(inst);
            return inst;
        }

        public bool Remove(ItemInstance inst)
        {
            if (inst == null) return false;
            return items.Remove(inst);
        }

        public ItemInstance FindById(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId)) return null;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].InstanceId == instanceId) return items[i];
            }
            return null;
        }
    }
}

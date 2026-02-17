using System;
using UnityEngine;

namespace Game.Systems.Items.Runtime
{
    [Serializable]
    public class ItemInstance
    {
        [SerializeField] private string instanceId;
        [SerializeField] private ItemDefinition definition;

        public string InstanceId => instanceId;
        public ItemDefinition Definition => definition;

        public ItemInstance(ItemDefinition def)
        {
            instanceId = Guid.NewGuid().ToString("N");
            definition = def;
        }

        // 反序列化/拷贝时用
        public ItemInstance(string id, ItemDefinition def)
        {
            instanceId = id;
            definition = def;
        }
    }
}

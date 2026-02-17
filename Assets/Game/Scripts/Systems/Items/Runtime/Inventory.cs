using System;
using UnityEngine;
using System.Collections.Generic;
using Game.Systems.Items.Runtime;

namespace Game.Systems.Items
{
    public class Inventory : MonoBehaviour
    {
        [SerializeField] private int capacity = 8;
        [SerializeField] private ItemSlot[] slots;

        public int Capacity => capacity;
        public IReadOnlyList<ItemSlot> Slots => slots;

        public event Action OnChanged;

        private void Awake()
        {
            if (capacity < 1) capacity = 1;

            // 初始化 slots（避免你忘了配）
            if (slots == null || slots.Length != capacity)
            {
                slots = new ItemSlot[capacity];
                for (int i = 0; i < capacity; i++)
                    slots[i] = new ItemSlot();
            }
        }

        /// <summary>
        /// 兼容旧用法：传 ItemDefinition 会自动包装成 ItemInstance
        /// </summary>
        public bool TryAdd(ItemDefinition item)
        {
            if (item == null) return false;
            return TryAdd(new ItemInstance(item));
        }

        /// <summary>
        /// ✅ 新用法：直接添加实例（能保留 InstanceId / 状态）
        /// </summary>
        public bool TryAdd(ItemInstance inst)
        {
            if (inst == null || inst.Definition == null) return false;

            int idx = FindFirstEmptyIndex();
            if (idx < 0) return false; // 满了

            slots[idx].Set(inst);
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// ✅ 新：拿到实例（推荐）
        /// </summary>
        public ItemInstance GetAt(int index)
        {
            if (!IsValidIndex(index)) return null;
            return slots[index].Instance;
        }

        /// <summary>
        /// 兼容旧代码：如果还在用 GetAtDefinition / DisplayName
        /// </summary>
        public ItemDefinition GetAtDefinition(int index)
        {
            var inst = GetAt(index);
            return inst != null ? inst.Definition : null;
        }

        public bool RemoveAt(int index)
        {
            if (!IsValidIndex(index)) return false;
            if (slots[index].IsEmpty) return false;

            slots[index].Clear();
            OnChanged?.Invoke();
            return true;
        }

        public bool IsFull() => FindFirstEmptyIndex() < 0;

        private int FindFirstEmptyIndex()
        {
            for (int i = 0; i < slots.Length; i++)
                if (slots[i].IsEmpty) return i;
            return -1;
        }

        /// <summary>
        /// ✅ 新：设置实例（允许 inst 为 null 表示清空）
        /// </summary>
        public bool SetAt(int index, ItemInstance inst)
        {
            if (index < 0 || index >= Capacity) return false;

            if (inst == null || inst.Definition == null)
                slots[index].Clear();
            else
                slots[index].Set(inst);

            OnChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 兼容旧用法：传 definition 会包装成新实例（注意：会生成新 InstanceId）
        /// </summary>
        public bool SetAt(int index, ItemDefinition item)
        {
            if (item == null) return SetAt(index, (ItemInstance)null);
            return SetAt(index, new ItemInstance(item));
        }

        /// <summary>
        /// ✅ Contains：按 definition 判断（背包里只要有同类物品就算）
        /// </summary>
        public bool Contains(ItemDefinition item)
        {
            if (item == null) return false;
            for (int i = 0; i < Capacity; i++)
            {
                var inst = GetAt(i);
                if (inst != null && inst.Definition == item) return true;
            }
            return false;
        }

        /// <summary>
        /// ✅ RemoveOne：移除任意一个该 definition 的实例
        /// </summary>
        public bool RemoveOne(ItemDefinition item)
        {
            if (item == null) return false;
            for (int i = 0; i < Capacity; i++)
            {
                var inst = GetAt(i);
                if (inst != null && inst.Definition == item)
                {
                    RemoveAt(i);
                    // RemoveAt 已经 Invoke 了
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// ✅（可选）移除指定实例：用于你以后做“消耗某一份实例”
        /// </summary>
        public bool RemoveInstance(ItemInstance inst)
        {
            if (inst == null) return false;
            for (int i = 0; i < Capacity; i++)
            {
                var cur = GetAt(i);
                if (cur != null && cur.InstanceId == inst.InstanceId)
                {
                    RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        private bool IsValidIndex(int index) => index >= 0 && index < slots.Length;

        // -----------------------------
        // ✅ 内部 Slot：改成存 ItemInstance
        // -----------------------------
        [Serializable]
        public class ItemSlot
        {
            [SerializeField] private ItemInstance instance;

            public ItemInstance Instance => instance;
            public ItemDefinition Item => instance != null ? instance.Definition : null; // 兼容旧访问

            public bool IsEmpty => instance == null || instance.Definition == null;

            public void Set(ItemInstance inst) => instance = inst;
            public void Clear() => instance = null;
        }
    }
}

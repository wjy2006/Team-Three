using System;
using UnityEngine;
using System.Collections.Generic;

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

        public bool TryAdd(ItemDefinition item)
        {
            if (item == null) return false;

            int idx = FindFirstEmptyIndex();
            if (idx < 0) return false; // 满了

            slots[idx].Set(item);
            OnChanged?.Invoke();
            return true;
        }

        public ItemDefinition GetAt(int index)
        {
            if (!IsValidIndex(index)) return null;
            return slots[index].Item;
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
        public bool SetAt(int index, ItemDefinition item)
        {
            // 允许 item 为 null（表示清空）
            if (index < 0 || index >= Capacity) return false;

            // 直接替换槽位内容（无堆叠）
            if (item == null)
                slots[index].Clear();
            else
                slots[index].Set(item);

            OnChanged?.Invoke();
            return true;
        }

        private bool IsValidIndex(int index) => index >= 0 && index < slots.Length;
    }
}

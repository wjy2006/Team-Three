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

            // 鍒濆鍖?slots锛堥伩鍏嶄綘蹇樹簡閰嶏級
            if (slots == null || slots.Length != capacity)
            {
                slots = new ItemSlot[capacity];
                for (int i = 0; i < capacity; i++)
                    slots[i] = new ItemSlot();
            }
        }

        /// <summary>
        /// 鍏煎鏃х敤娉曪細浼?ItemDefinition 浼氳嚜鍔ㄥ寘瑁呮垚 ItemInstance
        /// </summary>
        public bool TryAdd(ItemDefinition item)
        {
            if (item == null) return false;
            return TryAdd(new ItemInstance(item));
        }

        /// <summary>
        /// 鉁?鏂扮敤娉曪細鐩存帴娣诲姞瀹炰緥锛堣兘淇濈暀 InstanceId / 鐘舵€侊級
        /// </summary>
        public bool TryAdd(ItemInstance inst)
        {
            if (inst == null || inst.Definition == null) return false;

            int idx = FindFirstEmptyIndex();
            if (idx < 0) return false; // 婊′簡

            slots[idx].Set(inst);
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 鉁?鏂帮細鎷垮埌瀹炰緥锛堟帹鑽愶級
        /// </summary>
        public ItemInstance GetAt(int index)
        {
            if (!IsValidIndex(index)) return null;
            return slots[index].Instance;
        }

        /// <summary>
        /// 鍏煎鏃т唬鐮侊細濡傛灉杩樺湪鐢?GetAtDefinition / DisplayName
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

        public void ClearAll()
        {
            bool changed = false;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null || slots[i].IsEmpty) continue;
                slots[i].Clear();
                changed = true;
            }

            if (changed)
                OnChanged?.Invoke();
        }

        public bool IsFull() => FindFirstEmptyIndex() < 0;

        private int FindFirstEmptyIndex()
        {
            for (int i = 0; i < slots.Length; i++)
                if (slots[i].IsEmpty) return i;
            return -1;
        }

        /// <summary>
        /// 鉁?鏂帮細璁剧疆瀹炰緥锛堝厑璁?inst 涓?null 琛ㄧず娓呯┖锛?
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
        /// 鍏煎鏃х敤娉曪細浼?definition 浼氬寘瑁呮垚鏂板疄渚嬶紙娉ㄦ剰锛氫細鐢熸垚鏂?InstanceId锛?
        /// </summary>
        public bool SetAt(int index, ItemDefinition item)
        {
            if (item == null) return SetAt(index, (ItemInstance)null);
            return SetAt(index, new ItemInstance(item));
        }

        /// <summary>
        /// 鉁?Contains锛氭寜 definition 鍒ゆ柇锛堣儗鍖呴噷鍙鏈夊悓绫荤墿鍝佸氨绠楋級
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
        /// 鉁?RemoveOne锛氱Щ闄や换鎰忎竴涓 definition 鐨勫疄渚?
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
                    // RemoveAt 宸茬粡 Invoke 浜?
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 鉁咃紙鍙€夛級绉婚櫎鎸囧畾瀹炰緥锛氱敤浜庝綘浠ュ悗鍋氣€滄秷鑰楁煇涓€浠藉疄渚嬧€?
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
        // 鉁?鍐呴儴 Slot锛氭敼鎴愬瓨 ItemInstance
        // -----------------------------
        [Serializable]
        public class ItemSlot
        {
            [SerializeField] private ItemInstance instance;

            public ItemInstance Instance => instance;
            public ItemDefinition Item => instance != null ? instance.Definition : null; // 鍏煎鏃ц闂?

            public bool IsEmpty => instance == null || instance.Definition == null;

            public void Set(ItemInstance inst) => instance = inst;
            public void Clear() => instance = null;
        }
    }
}

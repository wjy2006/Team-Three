using System;
using System.Collections.Generic;
using Game.Systems.Items;
using UnityEngine;

namespace Game.Systems.Items.Runtime
{
    public static class RuntimeItemPriceOverrides
    {
        private static readonly Dictionary<string, int> BuyPriceByItemId = new Dictionary<string, int>(StringComparer.Ordinal);
        private static readonly Dictionary<ItemDefinition, int> BuyPriceByRef = new Dictionary<ItemDefinition, int>();

        public static int GetBuyPrice(ItemDefinition item)
        {
            if (item == null) return 0;

            string itemId = NormalizeItemId(item.ItemId);
            if (!string.IsNullOrEmpty(itemId) && BuyPriceByItemId.TryGetValue(itemId, out int byId))
                return Mathf.Max(0, byId);

            if (BuyPriceByRef.TryGetValue(item, out int byRef))
                return Mathf.Max(0, byRef);

            return Mathf.Max(0, item.BuyPrice);
        }

        public static void SetBuyPrice(ItemDefinition item, int buyPrice)
        {
            if (item == null) return;

            int clamped = Mathf.Max(0, buyPrice);
            string itemId = NormalizeItemId(item.ItemId);
            if (!string.IsNullOrEmpty(itemId))
            {
                BuyPriceByItemId[itemId] = clamped;
                return;
            }

            BuyPriceByRef[item] = clamped;
        }

        public static void ClearBuyPrice(ItemDefinition item)
        {
            if (item == null) return;

            string itemId = NormalizeItemId(item.ItemId);
            if (!string.IsNullOrEmpty(itemId))
                BuyPriceByItemId.Remove(itemId);

            BuyPriceByRef.Remove(item);
        }

        public static void ClearAll()
        {
            BuyPriceByItemId.Clear();
            BuyPriceByRef.Clear();
        }

        private static string NormalizeItemId(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId)) return null;
            return itemId.Trim();
        }
    }
}

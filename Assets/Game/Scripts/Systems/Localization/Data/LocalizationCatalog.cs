using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Localization/Localization Catalog")]
public class LocalizationCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string locale = "zh-CN";
        public LocalizationTable table;
    }

    public List<Entry> tables = new();

    public LocalizationTable Get(string locale)
    {
        foreach (var e in tables)
        {
            if (e != null && e.table != null && e.locale == locale)
                return e.table;
        }
        return null;
    }
}

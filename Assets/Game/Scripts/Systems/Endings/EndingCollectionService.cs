using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Systems.Endings
{
    [Serializable]
    public struct EndingRecord
    {
        public string id;
        public string title;
        public long unlockedAtUnixMs;
    }

    [Serializable]
    internal sealed class EndingRecordStore
    {
        public List<EndingRecord> records = new List<EndingRecord>();
    }

    public static class EndingCollectionService
    {
        private const string SaveKey = "game.endings.v1";

        private static bool loaded;
        private static EndingRecordStore store;

        public static bool Unlock(string endingId, string endingTitle = null)
        {
            string id = NormalizeId(endingId);
            if (string.IsNullOrEmpty(id)) return false;

            EnsureLoaded();

            int index = FindIndex(id);
            string title = string.IsNullOrWhiteSpace(endingTitle) ? id : endingTitle.Trim();
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            if (index >= 0)
            {
                bool changed = false;
                EndingRecord existing = store.records[index];

                if (string.IsNullOrWhiteSpace(existing.title) && !string.IsNullOrWhiteSpace(title))
                {
                    existing.title = title;
                    changed = true;
                }

                if (existing.unlockedAtUnixMs <= 0)
                {
                    existing.unlockedAtUnixMs = now;
                    changed = true;
                }

                if (changed)
                {
                    store.records[index] = existing;
                    Save();
                }

                return false;
            }

            store.records.Add(new EndingRecord
            {
                id = id,
                title = title,
                unlockedAtUnixMs = now
            });

            SortByUnlockedTime();
            Save();
            return true;
        }

        public static bool IsUnlocked(string endingId)
        {
            string id = NormalizeId(endingId);
            if (string.IsNullOrEmpty(id)) return false;

            EnsureLoaded();
            return FindIndex(id) >= 0;
        }

        public static IReadOnlyList<EndingRecord> GetUnlocked()
        {
            EnsureLoaded();
            return store.records;
        }

        public static void ClearAll()
        {
            EnsureLoaded();
            store.records.Clear();
            Save();
        }

        private static void EnsureLoaded()
        {
            if (loaded) return;
            loaded = true;

            string raw = PlayerPrefs.GetString(SaveKey, string.Empty);
            if (string.IsNullOrWhiteSpace(raw))
            {
                store = new EndingRecordStore();
                return;
            }

            try
            {
                store = JsonUtility.FromJson<EndingRecordStore>(raw);
            }
            catch
            {
                store = null;
            }

            if (store == null || store.records == null)
                store = new EndingRecordStore();

            PruneInvalid();
            SortByUnlockedTime();
        }

        private static void Save()
        {
            if (store == null) store = new EndingRecordStore();

            string json = JsonUtility.ToJson(store);
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
        }

        private static void PruneInvalid()
        {
            if (store == null || store.records == null) return;

            for (int i = store.records.Count - 1; i >= 0; i--)
            {
                if (string.IsNullOrEmpty(NormalizeId(store.records[i].id)))
                    store.records.RemoveAt(i);
            }
        }

        private static int FindIndex(string id)
        {
            if (store == null || store.records == null) return -1;

            for (int i = 0; i < store.records.Count; i++)
            {
                if (string.Equals(store.records[i].id, id, StringComparison.Ordinal))
                    return i;
            }

            return -1;
        }

        private static void SortByUnlockedTime()
        {
            if (store == null || store.records == null) return;

            store.records.Sort((a, b) =>
            {
                int byTime = a.unlockedAtUnixMs.CompareTo(b.unlockedAtUnixMs);
                if (byTime != 0) return byTime;
                return string.CompareOrdinal(a.id, b.id);
            });
        }

        private static string NormalizeId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            return id.Trim();
        }
    }
}

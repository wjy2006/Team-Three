#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.EditorTools
{
    public static class LocalizationTmpTextCollector
    {
        private const string CsvPath = "Assets/Game/GameData/Localization/zh-CN.csv";
        private static readonly string[] SceneFolders = { "Assets/Game/Scenes" };
        private static readonly string[] PrefabFolders = { "Assets/Game/Prefabs" };

        [MenuItem("Tools/Localization/Collect TMP Chinese Texts To zh-CN.csv")]
        public static void CollectChineseTextsToCsv()
        {
            if (!File.Exists(CsvPath))
            {
                Debug.LogError($"Localization collector: csv not found at '{CsvPath}'.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var setup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                ParseCsv(
                    File.ReadAllText(CsvPath, Encoding.UTF8),
                    out var keyToText,
                    out var textToKey);

                var existingKeys = new HashSet<string>(keyToText.Keys, StringComparer.Ordinal);
                var discoveredTexts = new HashSet<string>(StringComparer.Ordinal);

                CollectFromScenes(discoveredTexts);
                CollectFromPrefabs(discoveredTexts);

                var toAppend = new List<KeyValuePair<string, string>>();
                foreach (var text in discoveredTexts)
                {
                    if (textToKey.ContainsKey(text)) continue;

                    string key = GenerateAutoKey(text, keyToText, existingKeys);
                    existingKeys.Add(key);
                    keyToText[key] = text;
                    textToKey[text] = key;
                    toAppend.Add(new KeyValuePair<string, string>(key, text));
                }

                if (toAppend.Count == 0)
                {
                    Debug.Log("Localization collector: no new Chinese TMP texts to append.");
                    return;
                }

                toAppend.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
                AppendRows(CsvPath, toAppend);
                AssetDatabase.Refresh();

                Debug.Log($"Localization collector: appended {toAppend.Count} rows to '{CsvPath}'.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Localization collector failed: {ex}");
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        private static void CollectFromScenes(HashSet<string> outTexts)
        {
            var sceneGuids = AssetDatabase.FindAssets("t:Scene", SceneFolders);
            for (int i = 0; i < sceneGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var roots = scene.GetRootGameObjects();
                for (int r = 0; r < roots.Length; r++)
                {
                    var texts = roots[r].GetComponentsInChildren<TMP_Text>(true);
                    for (int t = 0; t < texts.Length; t++)
                        AddIfCandidate(texts[t] != null ? texts[t].text : null, outTexts);
                }
            }
        }

        private static void CollectFromPrefabs(HashSet<string> outTexts)
        {
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", PrefabFolders);
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var texts = root.GetComponentsInChildren<TMP_Text>(true);
                    for (int t = 0; t < texts.Length; t++)
                        AddIfCandidate(texts[t] != null ? texts[t].text : null, outTexts);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        private static void AddIfCandidate(string rawText, HashSet<string> outTexts)
        {
            if (string.IsNullOrWhiteSpace(rawText)) return;

            string normalized = rawText.Replace("\r\n", "\n").Trim();
            if (string.IsNullOrWhiteSpace(normalized)) return;
            if (!ContainsCjk(normalized)) return;

            outTexts.Add(normalized);
        }

        private static bool ContainsCjk(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c >= '\u4E00' && c <= '\u9FFF') return true;
                if (c >= '\u3400' && c <= '\u4DBF') return true;
                if (c >= '\uF900' && c <= '\uFAFF') return true;
            }
            return false;
        }

        private static string GenerateAutoKey(
            string text,
            Dictionary<string, string> keyToText,
            HashSet<string> existingKeys)
        {
            string baseKey = $"ui.editor.auto.t_{Fnv1A32(text):x8}";
            string key = baseKey;
            int suffix = 2;

            while (existingKeys.Contains(key))
            {
                if (keyToText.TryGetValue(key, out var existingText) && existingText == text)
                    return key;

                key = $"{baseKey}_{suffix}";
                suffix++;
            }

            return key;
        }

        private static uint Fnv1A32(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            uint hash = 2166136261u;
            for (int i = 0; i < bytes.Length; i++)
            {
                hash ^= bytes[i];
                hash *= 16777619u;
            }
            return hash;
        }

        private static void AppendRows(string path, List<KeyValuePair<string, string>> rows)
        {
            var sb = new StringBuilder();
            string existing = File.ReadAllText(path, Encoding.UTF8);

            if (!string.IsNullOrEmpty(existing) && !existing.EndsWith("\n", StringComparison.Ordinal))
                sb.AppendLine();

            for (int i = 0; i < rows.Count; i++)
            {
                sb.Append(EscapeCsvCell(rows[i].Key));
                sb.Append(',');
                sb.Append(EscapeCsvCell(rows[i].Value));
                sb.AppendLine();
            }

            File.AppendAllText(path, sb.ToString(), Encoding.UTF8);
        }

        private static string EscapeCsvCell(string value)
        {
            if (value == null) return "";
            bool needsQuotes = value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0;
            if (!needsQuotes) return value;
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        private static void ParseCsv(
            string csv,
            out Dictionary<string, string> keyToText,
            out Dictionary<string, string> textToKey)
        {
            keyToText = new Dictionary<string, string>(StringComparer.Ordinal);
            textToKey = new Dictionary<string, string>(StringComparer.Ordinal);

            var rows = ReadCsvRows(csv);
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.Count < 2) continue;

                string key = row[0].Trim();
                string text = row[1];

                if (string.IsNullOrEmpty(key) || key == "key") continue;

                keyToText[key] = text;
                if (!textToKey.ContainsKey(text))
                    textToKey[text] = key;
            }
        }

        private static List<List<string>> ReadCsvRows(string csv)
        {
            var result = new List<List<string>>();
            var row = new List<string>();
            var cell = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < csv.Length; i++)
            {
                char c = csv[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < csv.Length && csv[i + 1] == '"')
                        {
                            cell.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        cell.Append(c);
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        inQuotes = true;
                    }
                    else if (c == ',')
                    {
                        row.Add(cell.ToString());
                        cell.Clear();
                    }
                    else if (c == '\r')
                    {
                        // Ignore CR.
                    }
                    else if (c == '\n')
                    {
                        row.Add(cell.ToString());
                        cell.Clear();
                        result.Add(row);
                        row = new List<string>();
                    }
                    else
                    {
                        cell.Append(c);
                    }
                }
            }

            row.Add(cell.ToString());
            result.Add(row);
            return result;
        }
    }
}
#endif

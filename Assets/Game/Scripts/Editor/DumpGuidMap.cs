using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class DumpGuidMap
{
    [MenuItem("Tools/GUID Map/Dump Scripts GUID Map (CSV)")]
    public static void DumpScriptsCsv()
    {
        var sb = new StringBuilder();
        sb.AppendLine("path,guid");

        var guids = AssetDatabase.FindAssets("t:MonoScript");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;
            if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) continue;

            // CSV escape
            var safePath = path.Replace("\"", "\"\"");
            sb.Append('"').Append(safePath).Append('"').Append(',');
            sb.Append(guid).AppendLine();
        }

        var outPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../guid_map_scripts.csv"));
        File.WriteAllText(outPath, sb.ToString(), new UTF8Encoding(true));
        Debug.Log("Wrote: " + outPath);
    }

    [MenuItem("Tools/GUID Map/Dump ALL Assets GUID Map (CSV)")]
    public static void DumpAllAssetsCsv()
    {
        var sb = new StringBuilder();
        sb.AppendLine("path,guid");

        // 空 filter = 全资产
        var guids = AssetDatabase.FindAssets("");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;

            var safePath = path.Replace("\"", "\"\"");
            sb.Append('"').Append(safePath).Append('"').Append(',');
            sb.Append(guid).AppendLine();
        }

        var outPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../guid_map_all.csv"));
        File.WriteAllText(outPath, sb.ToString(), new UTF8Encoding(true));
        Debug.Log("Wrote: " + outPath);
    }
}

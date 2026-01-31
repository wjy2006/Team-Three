using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class LocalizationService : MonoBehaviour
{
    public LocalizationCatalog catalog;
    public string currentLocale = "zh-CN";

    private Dictionary<string, string> map = new(StringComparer.Ordinal);

    public void SetLocale(string locale)
    {
        currentLocale = locale;
        Load(locale);
    }

    public string Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        return map.TryGetValue(key, out var v) ? v : $"[{key}]";
    }

    void Awake()
    {
        Load(currentLocale);
    }

    private void Load(string locale)
    {
        map.Clear();

        if (catalog == null)
        {
            Debug.LogError("LocalizationService: catalog 未绑定");
            return;
        }

        var table = catalog.Get(locale);
        if (table == null || table.csv == null)
        {
            Debug.LogError($"LocalizationService: 找不到 locale={locale} 的表或 csv 未绑定");
            return;
        }

        ParseCsvToMap(table.csv.text, map);
        Debug.Log($"LocalizationService: loaded {map.Count} entries for {locale}");
    }

    // 轻量 CSV：两列 key,text，支持 text 里有逗号/引号/换行（用引号包裹）
    private static void ParseCsvToMap(string csv, Dictionary<string, string> outMap)
    {
        var rows = ReadCsvRows(csv);
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row.Count < 2) continue;

            var key = row[0].Trim();
            var text = row[1];

            // 跳过表头或空 key
            if (string.IsNullOrEmpty(key) || key == "key") continue;

            outMap[key] = text;
        }
    }

    // 简单 CSV 行/列解析（支持引号转义 ""）
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
                    // 处理 "" 作为一个 "
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
                    // ignore
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

        // last cell
        row.Add(cell.ToString());
        result.Add(row);

        return result;
    }
}

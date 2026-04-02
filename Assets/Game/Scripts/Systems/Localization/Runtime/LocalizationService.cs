using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class LocalizationService : MonoBehaviour
{
    public LocalizationCatalog catalog;
    public string currentLocale = "zh-CN";

    private readonly Dictionary<string, string> map = new(StringComparer.Ordinal);

    public void SetLocale(string locale)
    {
        currentLocale = locale;
        Load(locale);
    }

    public string Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        return map.TryGetValue(key, out var value) ? value : $"[{key}]";
    }

    private void Awake()
    {
        Load(currentLocale);
    }

    private void Load(string locale)
    {
        map.Clear();

        if (catalog == null)
        {
            Debug.LogError("LocalizationService: catalog is not assigned.");
            return;
        }

        var table = catalog.Get(locale);
        if (table == null || table.csv == null)
        {
            Debug.LogError($"LocalizationService: missing table or csv for locale={locale}.");
            return;
        }

        ParseCsvToMap(table.csv.text, map);
        Debug.Log($"LocalizationService: loaded {map.Count} entries for {locale}");
    }

    // Lightweight CSV parser for "key,text" rows.
    // Supports commas, quotes and newlines in quoted text fields.
    private static void ParseCsvToMap(string csv, Dictionary<string, string> outMap)
    {
        var rows = ReadCsvRows(csv);
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row.Count < 2) continue;

            var key = row[0].Trim();
            var text = row[1];

            // Skip header row and empty keys.
            if (string.IsNullOrEmpty(key) || key == "key") continue;

            outMap[key] = text;
        }
    }

    // Basic CSV row/column reader with escaped quote support ("").
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
                    // Treat doubled quote as a single quote.
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
                    // Ignore CR, LF handles end-of-row.
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

        // Last cell / row.
        row.Add(cell.ToString());
        result.Add(row);

        return result;
    }
}

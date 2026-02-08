using System;
using System.Collections.Generic;

[Serializable]
public class GlobalState
{
    private Dictionary<string, bool> bools = new();
    private Dictionary<string, int> ints = new();
    private Dictionary<string, string> strings = new();

    // ===== Bool =====
    public bool GetBool(string key)
        => bools.TryGetValue(key, out var v) && v;

    public void SetBool(string key, bool value)
        => bools[key] = value;

    // ===== Int =====
    public int GetInt(string key)
        => ints.TryGetValue(key, out var v) ? v : 0;

    public void SetInt(string key, int value)
        => ints[key] = value;

    public void AddInt(string key, int delta)
        => ints[key] = GetInt(key) + delta;

    // ===== String =====
    public string GetString(string key)
        => strings.TryGetValue(key, out var v) ? v : null;

    public void SetString(string key, string value)
        => strings[key] = value;

    // ===== Utility =====
    public bool HasKey(string key)
        => bools.ContainsKey(key) || ints.ContainsKey(key) || strings.ContainsKey(key);

    public void Clear(string key)
    {
        bools.Remove(key);
        ints.Remove(key);
        strings.Remove(key);
    }

    public void ClearAll()
    {
        bools.Clear();
        ints.Clear();
        strings.Clear();
    }
}

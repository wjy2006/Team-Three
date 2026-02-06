using System.Collections.Generic;
using UnityEngine;

public class StoryBlackboard : MonoBehaviour
{
    private readonly HashSet<string> flags = new();

    public bool HasFlag(string key) => flags.Contains(key);
    public void SetFlag(string key) => flags.Add(key);
    public void ClearFlag(string key) => flags.Remove(key);
}

using System;
using System.Collections.Generic;

[Serializable]
public class DialogueState
{
    // npcId -> talk count
    public Dictionary<string, int> talkCount = new();

    // 预留：剧情标记
    public HashSet<string> flags = new();

    public int GetTalkCount(string npcId)
        => talkCount.TryGetValue(npcId, out var v) ? v : 0;

    // 进入一次对话就+1，返回新次数（1,2,3..）
    public int IncrementTalkCount(string npcId)
    {
        int next = GetTalkCount(npcId) + 1;
        talkCount[npcId] = next;
        return next;
    }

    public bool HasFlag(string flag) => flags.Contains(flag);
    public void SetFlag(string flag) => flags.Add(flag);
}

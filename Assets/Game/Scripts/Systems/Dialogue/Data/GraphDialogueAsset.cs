using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Dialogue/Graph Dialogue")]
public class GraphDialogueAsset : DialogueAsset
{
    public string startNodeId = "start";
    public Node[] nodes;

    [Serializable]
    public class Node
    {
        public string id;
        public NodeType type;

        // Say 节点：你可以用 lines（多行），也可以用 speakerKey/textKey 单行
        public DialogueLine[] lines;
        public string speakerKey;
        public string textKey;

        // 跳转
        public string nextId;

        // IfBool 节点
        public string boolKey;
        public string trueNextId;
        public string falseNextId;

        // 写状态节点（SetBool / AddInt / SetInt）
        public bool boolValue;

        public string intKey;
        public int intValue;
    }

    public enum NodeType
    {
        Say,
        IfBool,
        SetBool,
        AddInt,
        SetInt,
        End
    }

    // Graph 不是一次性 BuildSession，所以这里不用
    public override DialogueSession BuildSession(string npcId, DialogueState state) => null;

    public Node Find(string nodeId)
    {
        if (nodes == null) return null;
        for (int i = 0; i < nodes.Length; i++)
            if (nodes[i] != null && nodes[i].id == nodeId) return nodes[i];
        return null;
    }
}

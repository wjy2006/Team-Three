using UnityEngine;

public abstract class DialogueAsset : ScriptableObject
{

    // 由系统调用：根据 npcId + state 生成本次会话内容
    public abstract DialogueSession BuildSession(string npcId, DialogueState state);
}

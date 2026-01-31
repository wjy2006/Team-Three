using UnityEngine;

public class InteractableNPC : MonoBehaviour
{
    public string npcId = "npc_001";
    public DialogueAsset dialogue;

    public void Interact()
    {
        if (GameRoot.I == null || GameRoot.I.Dialogue == null)
        {
            Debug.LogError("DialogueSystem 未就绪：确认 Boot 场景是否加载且 GameRoot 有 DialogueSystem", this);
            return;
        }

        if (dialogue == null)
        {
            Debug.LogWarning($"NPC {npcId} 没有绑定 DialogueAsset", this);
            return;
        }

        GameRoot.I.Dialogue.Open(npcId, dialogue);
    }
}

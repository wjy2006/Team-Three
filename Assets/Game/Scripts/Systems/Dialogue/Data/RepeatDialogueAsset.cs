using UnityEngine;

[CreateAssetMenu(menuName = "Game/Dialogue/Repeat Dialogue")]
public class RepeatDialogueAsset : DialogueAsset
{
    public DialogueLine[] lines;

    public override DialogueSession BuildSession(string npcId, DialogueState state)
    {
        // 普通NPC：永远重复同一段
        return new DialogueSession(lines);
    }
}

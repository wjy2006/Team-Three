using UnityEngine;

[CreateAssetMenu(menuName = "Game/Dialogue/Count Dialogue")]
public class CountDialogueAsset : DialogueAsset
{
    [Header("1st time")]
    public DialogueLine[] first;

    [Header("2nd time")]
    public DialogueLine[] second;

    [Header("3rd+ times")]
    public DialogueLine[] repeat;

    public override DialogueSession BuildSession(string npcId, DialogueState state)
    {
        int nextCount = state.IncrementTalkCount(npcId);

        DialogueLine[] chosen =
            nextCount == 1 ? first :
            nextCount == 2 ? second :
            repeat;

        return new DialogueSession(chosen);
    }
}

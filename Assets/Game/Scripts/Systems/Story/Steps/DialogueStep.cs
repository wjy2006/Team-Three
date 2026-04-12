using System.Collections;
using System;
using UnityEngine;

[Serializable]
public class DialogueStep : StoryStep
{
    public DialogueAsset dialogue;

    public override IEnumerator Play(StoryContext ctx)
    {
        if (dialogue == null) yield break;
        if (ctx?.Root == null || ctx.Root.Dialogue == null) yield break;

        var dialogueSystem = ctx.Root.Dialogue;
        bool done = false;
        void OnSessionClosed() => done = true;

        dialogueSystem.OnSessionClosed += OnSessionClosed;
        try
        {
            // Safety: if previous dialogue is still active for one frame, wait before opening next step.
            while (dialogueSystem.HasActiveSession)
                yield return null;

            dialogueSystem.Open("_story", dialogue);

            // If Open did nothing (e.g. empty asset), don't stall the story.
            if (!dialogueSystem.HasActiveSession)
                done = true;

            while (!done)
                yield return null;
        }
        finally
        {
            dialogueSystem.OnSessionClosed -= OnSessionClosed;
        }
    }
}

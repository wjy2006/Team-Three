using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Story/Steps/Dialogue")]
public class DialogueStep : StoryStep
{
    public DialogueAsset dialogue;

    public override IEnumerator Play(StoryContext ctx)
    {
        if (dialogue == null) yield break;
        if (ctx.Root == null || ctx.Root.Dialogue == null) yield break;

        bool done = false;

        void OnClosed() => done = true;

        ctx.Root.Dialogue.ui.OnClosed += OnClosed;
        ctx.Root.Dialogue.Open("_story", dialogue);

        while (!done) yield return null;

        ctx.Root.Dialogue.ui.OnClosed -= OnClosed;
    }
}

using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Story/Steps/Wait (Unscaled)")]
public class WaitUnscaledStep : StoryStep
{
    public float seconds = 0.25f;

    public override IEnumerator Play(StoryContext ctx)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }
}

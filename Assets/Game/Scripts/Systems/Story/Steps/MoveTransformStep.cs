using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Story/Steps/Move Transform")]
public class MoveTransformStep : StoryStep
{
    public Transform target;
    public Vector3 toPosition;
    public float duration = 0.4f;

    public override IEnumerator Play(StoryContext ctx)
    {
        if (target == null) yield break;

        Vector3 from = target.position;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float a = duration <= 0 ? 1f : Mathf.Clamp01(t / duration);
            target.position = Vector3.Lerp(from, toPosition, a);
            yield return null;
        }
        target.position = toPosition;
    }
}

using System.Collections;
using UnityEngine;

public abstract class StoryStep : ScriptableObject
{
    public abstract IEnumerator Play(StoryContext ctx);
}

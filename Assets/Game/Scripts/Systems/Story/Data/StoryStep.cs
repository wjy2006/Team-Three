using System.Collections;
using System;

[Serializable]
public abstract class StoryStep
{
    public abstract IEnumerator Play(StoryContext ctx);
}
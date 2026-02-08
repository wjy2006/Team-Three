using UnityEngine;

public abstract class StoryCondition : ScriptableObject, IStoryCondition
{
    public abstract bool Evaluate(GameEvent evt);
}

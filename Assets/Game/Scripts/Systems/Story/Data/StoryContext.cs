using UnityEngine;

public class StoryContext
{
    public GameRoot Root;
    public GlobalState Global;
    public MonoBehaviour Runner; // 用来 StartCoroutine 的宿主（StoryManager）
}

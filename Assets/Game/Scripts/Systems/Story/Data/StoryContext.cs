using UnityEngine;

public class StoryContext
{
    public GameRoot root;
    public MonoBehaviour runner; // 用来 StartCoroutine 的对象（通常是 StoryManager）
    public StoryBlackboard bb;   // 变量表
}

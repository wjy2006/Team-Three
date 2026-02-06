using System.Collections;
using UnityEngine;

public class StoryManager : MonoBehaviour
{
    public bool IsPlaying { get; private set; }

    public IEnumerator Play(StoryAsset story)
    {
        if (story == null || IsPlaying) yield break;
        IsPlaying = true;

        // 1) 锁输入
        if (story.lockPlayerInput && GameRoot.I?.playerInput != null)
            GameRoot.I.playerInput.SetAllGameplayEnabled(false); // 你需要在 input 里实现这个（后面说）

        // 2) 暂停世界
        if (story.pauseWorld && GameRoot.I?.Pause != null)
            GameRoot.I.Pause.PushPause("Story");

        var ctx = new StoryContext
        {
            root = GameRoot.I,
            runner = this,
            bb = GameRoot.I.Blackboard
        };

        // 3) 顺序执行 steps
        foreach (var step in story.steps)
        {
            if (step == null) continue;
            yield return step.Play(ctx);
        }

        // 4) 恢复
        if (story.pauseWorld && GameRoot.I?.Pause != null)
            GameRoot.I.Pause.PopPause("Story");

        if (story.lockPlayerInput && GameRoot.I?.playerInput != null)
            GameRoot.I.playerInput.SetAllGameplayEnabled(true);

        IsPlaying = false;
    }
}

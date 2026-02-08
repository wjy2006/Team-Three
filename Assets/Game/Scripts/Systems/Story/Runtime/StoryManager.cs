using System.Collections;
using UnityEngine;

public class StoryManager : MonoBehaviour
{
    public bool IsPlaying { get; private set; }

    public void Play(StoryAsset story)
    {
        if (story == null) return;
        if (IsPlaying) return;
        StartCoroutine(RunStory(story));
    }

    private IEnumerator RunStory(StoryAsset story)
    {
        IsPlaying = true;

        bool locked = false;
        bool paused = false;

        try
        {
            if (story.lockPlayerInput && GameRoot.I != null)
            {
                GameRoot.I.SetInputLocked(true);
                GameRoot.I.SetMoveLocked(true);
                locked = true;
            }

            if (story.pauseWorld && GameRoot.I?.Pause != null)
            {
                GameRoot.I.Pause.PushPause("Story");
                paused = true;
            }

            var ctx = new StoryContext
            {
                Root = GameRoot.I,
                Global = GameRoot.I.Global,
                Runner = this
            };

            if (story.steps != null)
            {
                foreach (var step in story.steps)
                {
                    if (step == null) continue;
                    yield return step.Play(ctx);
                }
            }
        }
        finally
        {
            // ✅ 先恢复 Pause（让世界跑起来），再恢复输入
            if (paused && GameRoot.I != null ? GameRoot.I.Pause : null != null)
                GameRoot.I.Pause.PopPause("Story");
            if (locked && GameRoot.I != null)
            {
                GameRoot.I.SetInputLocked(false);
                GameRoot.I.SetMoveLocked(false);
            }
            IsPlaying = false;
        }
    }
}

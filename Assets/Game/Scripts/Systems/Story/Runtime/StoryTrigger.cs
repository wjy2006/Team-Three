using UnityEngine;

public class StoryTrigger : MonoBehaviour, IStoryTrigger
{
    [SerializeField] private int priority = 0;
    public int Priority => priority;

    [Header("Trigger")]
    public bool triggerOnce = true;
    public bool consumeEvent = true;

    [Header("Story")]
    public StoryAsset story;
    public StoryCondition[] conditions;

    [Header("Once (GlobalState)")]
    [Tooltip("triggerOnce=true 时使用。建议手动填一个稳定且唯一的 key，比如 story/scene01/intro")]
    [SerializeField] private string onceKey;

    // 场景内缓存：同一场景生命周期内避免重复
    private bool firedLocal;

    private void Reset()
    {
        // 给个默认值，避免忘记填
        if (string.IsNullOrEmpty(onceKey))
            onceKey = $"story.once.{gameObject.name}";
    }

    private void OnEnable()
    {
        GameRoot.I?.Triggers?.Register(this);
    }

    private void OnDisable()
    {
        GameRoot.I?.Triggers?.Unregister(this);
    }

    public bool OnEvent(GameEvent evt)
    {
        // 1) 本地 once（同场景内）
        if (triggerOnce && firedLocal) return false;

        // 2) 全局 once（跨场景/重载）
        if (triggerOnce)
        {
            var g = GameRoot.I?.Global;
            if (g != null)
            {
                // 没填 key 的话，退化成只用本地 once（但会给你提示）
                if (string.IsNullOrEmpty(onceKey))
                {
                    Debug.LogWarning($"[StoryTrigger] {name} triggerOnce=true 但 onceKey 为空，将无法跨场景记忆。", this);
                }
                else if (g.GetBool(onceKey))
                {
                    return false; // ✅ 已经触发过
                }
            }
        }

        // 3) 条件检查
        if (conditions != null)
        {
            foreach (var c in conditions)
                if (c != null && !c.Evaluate(evt)) return false;
        }

        // 4) 触发：先标记，避免同帧/同事件递归触发
        firedLocal = true;

        if (triggerOnce && !string.IsNullOrEmpty(onceKey))
            GameRoot.I?.Global?.SetBool(onceKey, true);

        // 5) 播放剧情
        GameRoot.I?.Story?.Play(story);

        return consumeEvent;
    }
}

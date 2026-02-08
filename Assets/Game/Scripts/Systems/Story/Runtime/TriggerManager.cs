using System.Collections.Generic;
using UnityEngine;

public class TriggerManager : MonoBehaviour
{
    private readonly List<IStoryTrigger> triggers = new();
    private readonly List<IStoryTrigger> pendingRemove = new();

    private bool dispatching;
    private bool dirtyOrder; // 有新注册/注销时标记，避免每次 Raise 都 Sort

    public void Register(IStoryTrigger trigger)
    {
        if (trigger == null) return;
        if (triggers.Contains(trigger)) return;

        triggers.Add(trigger);
        dirtyOrder = true;
    }

    public void Unregister(IStoryTrigger trigger)
    {
        if (trigger == null) return;

        if (dispatching)
        {
            if (!pendingRemove.Contains(trigger))
                pendingRemove.Add(trigger);
            return;
        }

        if (triggers.Remove(trigger))
            dirtyOrder = true;
    }

    public void Raise(GameEvent evt)
    {
        if (evt == null) return;

        dispatching = true;

        // ✅ 先清理 null（Destroy 了的对象/脚本引用）
        bool removedNull = false;
        for (int i = triggers.Count - 1; i >= 0; i--)
        {
            if (triggers[i] == null)
            {
                triggers.RemoveAt(i);
                removedNull = true;
            }
        }
        if (removedNull) dirtyOrder = true;

        // ✅ 需要时才排序（优先级高的先触发）
        if (dirtyOrder)
        {
            triggers.Sort((a, b) =>
            {
                if (a == null && b == null) return 0;
                if (a == null) return 1;
                if (b == null) return -1;
                return b.Priority.CompareTo(a.Priority);
            });
            dirtyOrder = false;
        }

        // ✅ 按顺序派发；返回 true 则消耗事件，停止继续派发
        for (int i = 0; i < triggers.Count; i++)
        {
            var trigger = triggers[i];
            if (trigger == null) continue;

            bool consumed = false;
            try
            {
                consumed = trigger.OnEvent(evt);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }

            if (consumed)
                break;
        }

        dispatching = false;

        if (pendingRemove.Count > 0)
        {
            bool removedAny = false;
            for (int i = 0; i < pendingRemove.Count; i++)
            {
                if (pendingRemove[i] == null) continue;
                if (triggers.Remove(pendingRemove[i]))
                    removedAny = true;
            }
            pendingRemove.Clear();

            if (removedAny) dirtyOrder = true;
        }
    }
    public void RaiseNextFrame(GameEvent evt, MonoBehaviour runner)
    {
        if (evt == null || runner == null) return;
        runner.StartCoroutine(RaiseNextFrameCo(evt));
    }

    private System.Collections.IEnumerator RaiseNextFrameCo(GameEvent evt)
    {
        yield return null; // 等一帧（让所有 OnChanged/UI 刷新先跑完）
        Raise(evt);
    }
#if UNITY_EDITOR
    public int DebugTriggerCount => triggers.Count;
#endif
}

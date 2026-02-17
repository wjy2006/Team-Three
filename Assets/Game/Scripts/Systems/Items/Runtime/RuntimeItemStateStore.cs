using System.Collections.Generic;
using UnityEngine;

namespace Game.Systems.Items.Runtime
{
    /// <summary>
    /// 挂在 Player 上即可。用 InstanceId 记录每个物品实例的运行时状态。
    /// 目前只先做 int 状态（火箭HP），以后要扩展可以做多 key 或 struct。
    /// </summary>
    public class RuntimeItemStateStore : MonoBehaviour
    {
        private readonly Dictionary<string, int> intState = new();

        public int GetInt(string instanceId, int defaultValue)
        {
            if (string.IsNullOrEmpty(instanceId)) return defaultValue;
            return intState.TryGetValue(instanceId, out var v) ? v : defaultValue;
        }

        public void SetInt(string instanceId, int value)
        {
            if (string.IsNullOrEmpty(instanceId)) return;
            intState[instanceId] = Mathf.Max(0, value);
        }

        public void Remove(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId)) return;
            intState.Remove(instanceId);
        }
    }
}

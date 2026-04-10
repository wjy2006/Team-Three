using System;
using UnityEngine;

[DisallowMultipleComponent]
public class DisableByGlobalState : MonoBehaviour
{
    public enum RuleValueType
    {
        Bool,
        Int
    }

    public enum RuleMatchMode
    {
        Any,
        All
    }

    [Serializable]
    public struct Rule
    {
        public string key;
        public RuleValueType valueType;
        public bool expectedBool;
        public int expectedInt;

        public bool IsConfigured => !string.IsNullOrWhiteSpace(key);

        public bool IsMatch(GlobalState global)
        {
            if (global == null || !IsConfigured) return false;

            return valueType switch
            {
                RuleValueType.Bool => global.GetBool(key) == expectedBool,
                RuleValueType.Int => global.GetInt(key) == expectedInt,
                _ => false
            };
        }
    }

    [Header("Target")]
    [Tooltip("留空则禁用当前 GameObject。")]
    public GameObject target;

    [Header("Disable Rules")]
    [Tooltip("满足规则后就禁用目标。默认任意一条命中就禁用。")]
    public RuleMatchMode matchMode = RuleMatchMode.Any;
    public Rule[] rules;

    private void Reset()
    {
        target = gameObject;
    }

    private void Start()
    {
        Apply();
    }

    [ContextMenu("Apply")]
    public void Apply()
    {
        if (rules == null || rules.Length == 0) return;
        if (GameRoot.I == null || GameRoot.I.Global == null) return;

        GameObject targetObject = target != null ? target : gameObject;
        bool hasConfiguredRule = false;
        bool shouldDisable = matchMode == RuleMatchMode.All;

        foreach (Rule rule in rules)
        {
            if (!rule.IsConfigured) continue;

            hasConfiguredRule = true;
            bool matched = rule.IsMatch(GameRoot.I.Global);

            if (matchMode == RuleMatchMode.Any)
            {
                if (matched)
                {
                    shouldDisable = true;
                    break;
                }
            }
            else if (!matched)
            {
                shouldDisable = false;
                break;
            }
        }

        if (hasConfiguredRule && shouldDisable)
        {
            targetObject.SetActive(false);
        }
    }
}

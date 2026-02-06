using System.Collections.Generic;
using Game.Systems.Items;
using UnityEngine;

public class StoryTrigger : MonoBehaviour
{
    public StoryAsset story;
    public List<StoryCondition> conditions = new();
    public bool triggerOnce = true;

    private bool fired;

    void OnEnable()
    {
        GameEvents.OnDamaged += HandleDamaged;
        GameEvents.OnItemUsed += HandleItemUsed;
        GameEvents.OnSceneEntered += HandleSceneEntered;
    }

    void OnDisable()
    {
        GameEvents.OnDamaged -= HandleDamaged;
        GameEvents.OnItemUsed -= HandleItemUsed;
        GameEvents.OnSceneEntered -= HandleSceneEntered;
    }

    private void TryFire()
    {
        if (triggerOnce && fired) return;
        if (GameRoot.I == null || GameRoot.I.Story == null) return;
        if (GameRoot.I.Story.IsPlaying) return;

        foreach (var c in conditions)
            if (c != null && !c.Evaluate(GameRoot.I)) return;

        fired = true;
        StartCoroutine(GameRoot.I.Story.Play(story));
    }

    // 下面三个是“事件源”，你可以按需要删/拆
    private void HandleDamaged(GameObject who, float amount) => TryFire();
    private void HandleItemUsed(ItemDefinition item) => TryFire();
    private void HandleSceneEntered(string sceneName) => TryFire();
}

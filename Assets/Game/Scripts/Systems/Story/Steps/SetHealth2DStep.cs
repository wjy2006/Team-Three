using System;
using System.Collections;
using Game.Gameplay.Combat;
using UnityEngine;

[Serializable]
public class SetHealth2DStep : StoryStep
{
    public Health2D target;
    [Min(0.01f)] public float maxHp = 20f;
    public bool setCurrentToMax = true;
    [Min(0f)] public float currentHp = 20f;

    public override IEnumerator Play(StoryContext ctx)
    {
        if (target == null) yield break;

        target.maxHp = Mathf.Max(0.01f, maxHp);
        if (setCurrentToMax)
            target.hp = target.maxHp;
        else
            target.hp = Mathf.Clamp(currentHp, 0f, target.maxHp);

        yield break;
    }
}

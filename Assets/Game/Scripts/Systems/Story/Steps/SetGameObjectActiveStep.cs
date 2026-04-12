using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class SetGameObjectActiveStep : StoryStep
{
    public GameObject target;
    public bool active = true;

    public override IEnumerator Play(StoryContext ctx)
    {
        if (target != null)
            target.SetActive(active);

        yield break;
    }
}

using System.Collections;
using System;
using UnityEngine;

[Serializable]
public class SetGlobalBoolStep : StoryStep
{
    public string key;
    public bool value = true;

    public override IEnumerator Play(StoryContext ctx)
    {
        ctx?.Global?.SetBool(key, value);
        yield break;
    }
}
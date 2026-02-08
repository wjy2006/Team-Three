using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Story/Steps/Set Global Bool")]
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

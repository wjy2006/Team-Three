using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Story/Story Asset")]
public class StoryAsset : ScriptableObject
{
    public List<StoryStep> steps = new();
    public bool lockPlayerInput = true;
    public bool pauseWorld = true;
}

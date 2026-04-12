using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(menuName = "Story/Conditions/Scene/Active Scene")]
public class ActiveSceneCondition : StoryCondition
{
    [Tooltip("Match any of these scene names.")]
    public string[] sceneNames;
    [Tooltip("Invert result.")]
    public bool invert;

    public override bool Evaluate(GameEvent evt)
    {
        string current = SceneManager.GetActiveScene().name;
        bool match = false;

        if (sceneNames != null)
        {
            for (int i = 0; i < sceneNames.Length; i++)
            {
                string s = sceneNames[i];
                if (string.IsNullOrWhiteSpace(s)) continue;
                if (string.Equals(current, s.Trim(), StringComparison.Ordinal))
                {
                    match = true;
                    break;
                }
            }
        }

        return invert ? !match : match;
    }
}

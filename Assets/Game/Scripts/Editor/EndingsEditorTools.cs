using Game.Systems.Endings;
using UnityEditor;
using UnityEngine;

public static class EndingsEditorTools
{
    [MenuItem("Tools/Endings/Clear All Unlocked Endings")]
    private static void ClearAllUnlockedEndings()
    {
        bool ok = EditorUtility.DisplayDialog(
            "Clear Endings",
            "Clear all unlocked endings in local PlayerPrefs?",
            "Clear",
            "Cancel");
        if (!ok) return;

        EndingCollectionService.ClearAll();
        Debug.Log("[EndingsEditorTools] Cleared all unlocked endings.");
    }
}

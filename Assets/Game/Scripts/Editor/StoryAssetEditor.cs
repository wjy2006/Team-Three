#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StoryAsset))]
public class StoryAssetEditor : Editor
{
    SerializedProperty _steps;
    SerializedProperty _lockPlayerInput;
    SerializedProperty _pauseWorld;

    Type[] _stepTypes;
    string[] _stepTypeNames;
    int _selectedIndex;

    void OnEnable()
    {
        _steps = serializedObject.FindProperty("steps");
        _lockPlayerInput = serializedObject.FindProperty("lockPlayerInput");
        _pauseWorld = serializedObject.FindProperty("pauseWorld");

        // 找到所有 StoryStep 派生类（非抽象、非泛型）
        _stepTypes = TypeCache.GetTypesDerivedFrom<StoryStep>()
            .Where(t => !t.IsAbstract && !t.IsGenericType)
            .OrderBy(t => t.Name)
            .ToArray();

        _stepTypeNames = _stepTypes.Select(t => t.Name).ToArray();
        _selectedIndex = 0;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_lockPlayerInput);
        EditorGUILayout.PropertyField(_pauseWorld);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Steps", EditorStyles.boldLabel);

        DrawAddBar();
        EditorGUILayout.Space(6);

        // 画 steps 列表
        for (int i = 0; i < _steps.arraySize; i++)
        {
            var element = _steps.GetArrayElementAtIndex(i);

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                string title = element.managedReferenceValue != null
                    ? element.managedReferenceValue.GetType().Name
                    : "Null Step";

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"{i}. {title}", EditorStyles.boldLabel);

                    if (GUILayout.Button("▲", GUILayout.Width(28)) && i > 0)
                        _steps.MoveArrayElement(i, i - 1);

                    if (GUILayout.Button("▼", GUILayout.Width(28)) && i < _steps.arraySize - 1)
                        _steps.MoveArrayElement(i, i + 1);

                    if (GUILayout.Button("X", GUILayout.Width(28)))
                    {
                        // managedReference 删除需要先置空再删，避免残留
                        element.managedReferenceValue = null;
                        _steps.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }

                // 画该 step 的字段
                EditorGUILayout.PropertyField(element, includeChildren: true);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    void DrawAddBar()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (_stepTypeNames != null && _stepTypeNames.Length > 0)
                _selectedIndex = EditorGUILayout.Popup(_selectedIndex, _stepTypeNames);
            else
                EditorGUILayout.HelpBox("No StoryStep derived types found.", MessageType.Warning);

            using (new EditorGUI.DisabledScope(_stepTypes == null || _stepTypes.Length == 0))
            {
                if (GUILayout.Button("Add Step", GUILayout.Width(100)))
                {
                    var t = _stepTypes[_selectedIndex];
                    var instance = Activator.CreateInstance(t);
                    int index = _steps.arraySize;
                    _steps.InsertArrayElementAtIndex(index);
                    _steps.GetArrayElementAtIndex(index).managedReferenceValue = instance;
                }
            }
        }
    }
}
#endif
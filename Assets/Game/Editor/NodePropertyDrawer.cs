using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(GraphDialogueAsset.Node))]
public class GraphDialogueNodeDrawer : PropertyDrawer
{
    const float VERTICAL_SPACING = 2f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = 0f;

        var typeProp = property.FindPropertyRelative("type");
        var linesProp = property.FindPropertyRelative("lines");

        var type = (GraphDialogueAsset.NodeType)typeProp.enumValueIndex;

        // id + type
        height += Line();
        height += Line();

        switch (type)
        {
            case GraphDialogueAsset.NodeType.Say:
                height += EditorGUI.GetPropertyHeight(linesProp, true);
                height += Line();
                break;

            case GraphDialogueAsset.NodeType.IfBool:
                height += Line(); // boolKey
                height += Line(); // trueNext
                height += Line(); // falseNext
                break;

            case GraphDialogueAsset.NodeType.SetBool:
                height += Line(); // boolKey
                height += Line(); // boolValue
                height += Line(); // next
                break;

            case GraphDialogueAsset.NodeType.AddInt:
            case GraphDialogueAsset.NodeType.SetInt:
                height += Line(); // intKey
                height += Line(); // intValue
                height += Line(); // next
                break;

            case GraphDialogueAsset.NodeType.GiveItem:
                height += Line(); // item
                height += Line(); // success
                height += Line(); // fail
                break;

            case GraphDialogueAsset.NodeType.End:
                break;
        }

        return height + 8f;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        var typeProp = property.FindPropertyRelative("type");
        var type = (GraphDialogueAsset.NodeType)typeProp.enumValueIndex;

        // 根据类型选择背景色
        Color bgColor = GetColor(type);

        // 画背景
        EditorGUI.DrawRect(position, bgColor);

        // 留一点内边距（避免贴边）
        position.x += 4;
        position.width -= 8;
        position.y += 4;

        var idProp = property.FindPropertyRelative("id");

        var linesProp = property.FindPropertyRelative("lines");
        var nextIdProp = property.FindPropertyRelative("nextId");

        var boolKeyProp = property.FindPropertyRelative("boolKey");
        var trueNextProp = property.FindPropertyRelative("trueNextId");
        var falseNextProp = property.FindPropertyRelative("falseNextId");

        var boolValueProp = property.FindPropertyRelative("boolValue");

        var intKeyProp = property.FindPropertyRelative("intKey");
        var intValueProp = property.FindPropertyRelative("intValue");

        var itemProp = property.FindPropertyRelative("itemToGive");
        var successProp = property.FindPropertyRelative("successNextId");
        var failProp = property.FindPropertyRelative("failNextId");

        Rect r = position;
        r.height = EditorGUIUtility.singleLineHeight;

        // id
        EditorGUI.PropertyField(r, idProp);
        r.y += r.height + VERTICAL_SPACING;

        // type
        EditorGUI.PropertyField(r, typeProp);
        r.y += r.height + VERTICAL_SPACING;


        switch (type)
        {
            case GraphDialogueAsset.NodeType.Say:
                EditorGUI.PropertyField(r, linesProp, true);
                r.y += EditorGUI.GetPropertyHeight(linesProp, true) + VERTICAL_SPACING;

                EditorGUI.PropertyField(r, nextIdProp);
                break;

            case GraphDialogueAsset.NodeType.IfBool:
                EditorGUI.PropertyField(r, boolKeyProp);
                r.y += r.height + VERTICAL_SPACING;

                EditorGUI.PropertyField(r, trueNextProp);
                r.y += r.height + VERTICAL_SPACING;

                EditorGUI.PropertyField(r, falseNextProp);
                break;

            case GraphDialogueAsset.NodeType.SetBool:
                EditorGUI.PropertyField(r, boolKeyProp);
                r.y += r.height + VERTICAL_SPACING;

                EditorGUI.PropertyField(r, boolValueProp);
                r.y += r.height + VERTICAL_SPACING;

                EditorGUI.PropertyField(r, nextIdProp);
                break;

            case GraphDialogueAsset.NodeType.AddInt:
            case GraphDialogueAsset.NodeType.SetInt:
                EditorGUI.PropertyField(r, intKeyProp);
                r.y += r.height + VERTICAL_SPACING;

                EditorGUI.PropertyField(r, intValueProp);
                r.y += r.height + VERTICAL_SPACING;

                EditorGUI.PropertyField(r, nextIdProp);
                break;

            case GraphDialogueAsset.NodeType.GiveItem:
                EditorGUI.PropertyField(r, itemProp);
                r.y += r.height + VERTICAL_SPACING;

                EditorGUI.PropertyField(r, successProp);
                r.y += r.height + VERTICAL_SPACING;

                EditorGUI.PropertyField(r, failProp);
                break;

            case GraphDialogueAsset.NodeType.End:
                break;
        }

        EditorGUI.EndProperty();
    }

    float Line()
    {
        return EditorGUIUtility.singleLineHeight + VERTICAL_SPACING;
    }
    Color GetColor(GraphDialogueAsset.NodeType type)
    {
        switch (type)
        {
            case GraphDialogueAsset.NodeType.Say:
                return new Color(0.7f, 0.85f, 1f, 0.3f); // 淡蓝

            case GraphDialogueAsset.NodeType.IfBool:
                return new Color(0.85f, 0.7f, 1f, 0.3f); // 淡紫

            case GraphDialogueAsset.NodeType.SetBool:
                return new Color(0.7f, 1f, 0.7f, 0.3f); // 淡绿

            case GraphDialogueAsset.NodeType.AddInt:
            case GraphDialogueAsset.NodeType.SetInt:
                return new Color(1f, 0.85f, 0.6f, 0.3f); // 橙

            case GraphDialogueAsset.NodeType.GiveItem:
                return new Color(1f, 0.95f, 0.4f, 0.4f); // 金黄

            case GraphDialogueAsset.NodeType.End:
                return new Color(0.7f, 0.7f, 0.7f, 0.3f); // 灰

            default:
                return new Color(1f, 1f, 1f, 0.1f);
        }
    }

}

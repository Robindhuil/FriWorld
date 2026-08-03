using UnityEditor;
using UnityEngine;

/// <summary>Renders <c>[Layer] int</c> fields as Unity's layer dropdown.</summary>
[CustomPropertyDrawer(typeof(LayerAttribute))]
class LayerAttributeDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType == SerializedPropertyType.Integer)
            property.intValue = EditorGUI.LayerField(position, label, property.intValue);
        else
            EditorGUI.PropertyField(position, property, label);
    }
}

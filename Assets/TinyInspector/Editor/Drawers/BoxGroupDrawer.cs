using TinyInspector;
using UnityEditor;
using UnityEngine;

namespace TinyInspector
{
    [CustomPropertyDrawer(typeof(BoxGroupAttribute))]
    public class BoxGroupDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Don't draw any extra label or box here — let grouping be handled by the custom editor.
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.PropertyField(position, property, label, true);
            EditorGUI.EndProperty();
        }

    }
}
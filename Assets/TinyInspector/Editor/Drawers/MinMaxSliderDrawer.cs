using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace TinyInspector
{
    [CustomPropertyDrawer(typeof(MinMaxSliderAttribute))]
    public class MinMaxSliderDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (MinMaxSliderAttribute)attribute;

            // Build tooltip that includes existing tooltip if present
            string existingTooltip = label != null ? label.tooltip : null;
            string extraTooltip = $"Min Max Slider: {attr.minLimit} - {attr.maxLimit}";
            string combinedTooltip = string.IsNullOrEmpty(existingTooltip) ? extraTooltip : existingTooltip + "\n" + extraTooltip;

            // Create a GUIContent cloned from the original label and set combined tooltip
            GUIContent tooltipLabel = label != null ? new GUIContent(label) : new GUIContent();
            tooltipLabel.tooltip = combinedTooltip;

            if (property.propertyType == SerializedPropertyType.Vector2)
            {
                DrawVector2(position, property, tooltipLabel, attr.minLimit, attr.maxLimit);
            }
            else if (property.propertyType == SerializedPropertyType.Vector4)
            {
                // Unity sometimes serializes Vector2Int as Vector4 when using SerializedProperty; handle by name check
                if (property.type == "Vector2Int" || property.type == "UnityEngine.Vector2Int")
                {
                    DrawVector2Int(position, property, tooltipLabel, (int)attr.minLimit, (int)attr.maxLimit);
                }
                else
                {
                    EditorGUI.LabelField(position, label.text, "MinMaxSlider supports only Vector2 and Vector2Int");
                }
            }
            else if (property.propertyType == SerializedPropertyType.Vector2Int || property.type == "Vector2Int")
            {
                // SerializedProperty does not have Vector2Int propertyType in older versions; fallback
                DrawVector2Int(position, property, tooltipLabel, (int)attr.minLimit, (int)attr.maxLimit);
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "MinMaxSlider supports only Vector2 and Vector2Int");
            }
        }

        private void DrawVector2(Rect position, SerializedProperty property, GUIContent label, float minLimit, float maxLimit)
        {
            EditorGUI.BeginProperty(position, label, property);

            float lineHeight = EditorGUIUtility.singleLineHeight;

            // Use PrefixLabel so Unity handles label and indentation
            Rect contentRect = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            // Values
            SerializedProperty xProp = property.FindPropertyRelative("x");
            SerializedProperty yProp = property.FindPropertyRelative("y");

            float fieldWidth = 60f;
            float padding = 4f;

            // Place everything on the same line (contentRect.y)
            Rect xRect = new Rect(contentRect.x, contentRect.y, fieldWidth, lineHeight);
            Rect yRect = new Rect(contentRect.x + contentRect.width - fieldWidth, contentRect.y, fieldWidth, lineHeight);
            Rect sliderRect = new Rect(xRect.xMax + padding, contentRect.y, contentRect.width - fieldWidth * 2 - padding * 2, lineHeight);

            float xVal = xProp.floatValue;
            float yVal = yProp.floatValue;

            xVal = EditorGUI.FloatField(xRect, xVal);
            yVal = EditorGUI.FloatField(yRect, yVal);

            // Ensure min <= max for slider representation
            float vMin = Mathf.Min(xVal, yVal);
            float vMax = Mathf.Max(xVal, yVal);

            EditorGUI.MinMaxSlider(sliderRect, ref vMin, ref vMax, minLimit, maxLimit);

            // Map back to x/y: x = vMin, y = vMax
            xProp.floatValue = vMin;
            yProp.floatValue = vMax;

            EditorGUI.EndProperty();
        }

        private void DrawVector2Int(Rect position, SerializedProperty property, GUIContent label, int minLimit, int maxLimit)
        {
            EditorGUI.BeginProperty(position, label, property);

            float lineHeight = EditorGUIUtility.singleLineHeight;

            // Use PrefixLabel so Unity handles label and indentation
            Rect contentRect = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            // Vector2Int is stored as two ints; SerializedProperty may expose x and y
            SerializedProperty xProp = property.FindPropertyRelative("x") ?? property.FindPropertyRelative("m_X") ?? null;
            SerializedProperty yProp = property.FindPropertyRelative("y") ?? property.FindPropertyRelative("m_Y") ?? null;

            float fieldWidth = 60f;
            float padding = 4f;

            Rect xRect = new Rect(contentRect.x, contentRect.y, fieldWidth, lineHeight);
            Rect yRect = new Rect(contentRect.x + contentRect.width - fieldWidth, contentRect.y, fieldWidth, lineHeight);
            Rect sliderRect = new Rect(xRect.xMax + padding, contentRect.y, contentRect.width - fieldWidth * 2 - padding * 2, lineHeight);

            int xVal = xProp != null ? xProp.intValue : 0;
            int yVal = yProp != null ? yProp.intValue : 0;

            xVal = EditorGUI.IntField(xRect, xVal);
            yVal = EditorGUI.IntField(yRect, yVal);

            int vMin = Mathf.Min(xVal, yVal);
            int vMax = Mathf.Max(xVal, yVal);

            float fMin = vMin;
            float fMax = vMax;
            EditorGUI.MinMaxSlider(sliderRect, ref fMin, ref fMax, minLimit, maxLimit);

            vMin = Mathf.RoundToInt(fMin);
            vMax = Mathf.RoundToInt(fMax);

            if (xProp != null) xProp.intValue = vMin;
            if (yProp != null) yProp.intValue = vMax;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // Single line height
            return EditorGUIUtility.singleLineHeight;
        }
    }
}

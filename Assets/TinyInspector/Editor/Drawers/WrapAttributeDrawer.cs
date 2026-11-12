using UnityEditor;

using UnityEngine;
using UnityEngine.UIElements;

namespace TinyInspector
{
    [CustomPropertyDrawer(typeof(WrapAttribute))]
    public class WrapAttributeDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var attr = (WrapAttribute)attribute;
            string extraTooltip = $"Wrap: {attr.Min} - {attr.Max}";

            var propField = new TinyPropertyField(property, property.displayName);

            EventCallback<GeometryChangedEvent> geometryCallback = null;
            geometryCallback = (evt) =>
            {
                if (evt.target != propField)
                    return;

                PostProcessLabelTooltip(propField, extraTooltip);

                if (TryRegisterNumericWrapCallback(propField, property, attr))
                {
                    propField.UnregisterCallback(geometryCallback);
                }
            };

            propField.RegisterCallback(geometryCallback);

            return propField;
        }

        private static bool TryRegisterNumericWrapCallback(VisualElement root, SerializedProperty property, WrapAttribute attr)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    {
                        var intField = root.Q<IntegerField>();
                        if (intField == null)
                            return false;

                        RegisterIntegerWrapCallback(intField, property, attr);
                        return true;
                    }
                case SerializedPropertyType.Float:
                    {
                        var floatField = root.Q<FloatField>();
                        if (floatField == null)
                            return false;

                        RegisterFloatWrapCallback(floatField, property, attr);
                        return true;
                    }
            }

            return true;
        }

        private static void PostProcessLabelTooltip(VisualElement field, string tooltip)
        {
            if (string.IsNullOrEmpty(tooltip))
                return;

            var label = field.Q<Label>(className: "unity-base-field__label");
            if (label != null)
            {
                label.tooltip = tooltip;
            }
        }

        private static void RegisterIntegerWrapCallback(IntegerField field, SerializedProperty property, WrapAttribute attr)
        {
            field.RegisterValueChangedCallback((ChangeEvent<int> evt) =>
            {
                int min = (int)attr.Min;
                int max = (int)attr.Max;
                int newVal = evt.newValue;

                if (max > min)
                {
                    if (newVal > max)
                        newVal = min;
                    else if (newVal < min)
                        newVal = max;
                }

                if (property.intValue != newVal)
                {
                    property.intValue = newVal;
                    property.serializedObject.ApplyModifiedProperties();
                }

                field.SetValueWithoutNotify(newVal);
            });
        }

        private static void RegisterFloatWrapCallback(FloatField field, SerializedProperty property, WrapAttribute attr)
        {
            field.RegisterValueChangedCallback((ChangeEvent<float> evt) =>
            {
                float min = (float)attr.Min;
                float max = (float)attr.Max;
                float newVal = evt.newValue;

                if (max > min)
                {
                    if (newVal > max)
                        newVal = min;
                    else if (newVal < min)
                        newVal = max;
                }

                if (!Mathf.Approximately(property.floatValue, newVal))
                {
                    property.floatValue = newVal;
                    property.serializedObject.ApplyModifiedProperties();
                }

                field.SetValueWithoutNotify(newVal);
            });
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
    }
}

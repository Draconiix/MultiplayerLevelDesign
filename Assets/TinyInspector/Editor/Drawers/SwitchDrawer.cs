#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyInspector
{
    [CustomPropertyDrawer(typeof(SwitchAttribute))]
    public class SwitchDrawer : PropertyDrawer
    {
        // UIElements implementation using USS
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var field = new TinyPropertyField(property);

            // Only for boolean properties
            if (property.propertyType != SerializedPropertyType.Boolean)
            {
                return field;
            }

            var attr = (SwitchAttribute)attribute;

            var root = new VisualElement();
            root.style.marginLeft = 2;

            // Try load stylesheet from Assets (Resources folder)
            var ss = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Resources/TinyInspector/Editor/TinyInspector.uss");
            if (ss != null)
                root.styleSheets.Add(ss);

            root.AddToClassList("SwitchContainer");
            root.style.borderBottomColor = TinyInspectorStyles.BorderColor;
            root.style.borderLeftColor = TinyInspectorStyles.BorderColor;
            root.style.borderRightColor = TinyInspectorStyles.BorderColor;
            root.style.borderTopColor = TinyInspectorStyles.BorderColor;

            // Swap displayed texts: left shows OnLabel, right shows OffLabel
            var left = new Label(attr.OnLabel);
            left.AddToClassList("SwitchText");
            left.style.borderTopLeftRadius = 2;
            left.style.borderBottomLeftRadius = 2;

            var right = new Label(attr.OffLabel);
            right.AddToClassList("SwitchText");
            right.style.borderTopRightRadius = 2;
            right.style.borderBottomRightRadius = 2;

            root.Add(left);
            root.Add(right);

            left.AddToClassList("LeftLabel");
            right.AddToClassList("RightLabel");

            void UpdateVisuals()
            {
                bool val = property.boolValue;
                // remove active classes from container
                //root.RemoveFromClassList("ActiveSwitchOff");
                //root.RemoveFromClassList("ActiveSwitchOn");

                // remove selection overlay from labels
                left.RemoveFromClassList("SelectedLabel");
                right.RemoveFromClassList("SelectedLabel");

                if (val)
                {
                    // On selected -> container active (blue)
                    //root.AddToClassList("ActiveSwitchOn");

                    root.style.backgroundColor = TinyInspectorStyles.Instance.GetAccentColor(attr.Color);
                    left.style.color = TinyInspectorStyles.Instance.GetAccentTextColor(attr.Color);

                    // add selected overlay to right label (behavior unchanged)
                    right.AddToClassList("SelectedLabel");
                    left.style.backgroundColor = TinyInspectorStyles.Instance.GetAccentColor(attr.Color); 
                    right.style.backgroundColor = TinyInspectorStyles.ButtonColor;
                    right.style.color = TinyInspectorStyles.ButtonColor;

                    left.style.borderRightColor = new Color(0, 0, 0, 0);
                    right.style.borderLeftColor = TinyInspectorStyles.BorderColor;

                }
                else
                {
                    // Off selected -> container active (dark gray)
                    //root.AddToClassList("ActiveSwitchOff");

                    root.style.backgroundColor = TinyInspectorStyles.FieldColor;
                    left.style.color = TinyInspectorStyles.ButtonColor;

                    // add selected overlay to left label (behavior unchanged)
                    left.AddToClassList("SelectedLabel");
                    left.style.backgroundColor = TinyInspectorStyles.ButtonColor;
                    right.style.backgroundColor = TinyInspectorStyles.FieldColor;
                    right.style.color = TinyInspectorStyles.LabelColor;

                    right.style.borderLeftColor = new Color(0, 0, 0, 0);
                    left.style.borderRightColor = TinyInspectorStyles.BorderColor;
                }
            }

            UpdateVisuals();

            root.RegisterCallback<MouseUpEvent>(evt =>
            {
                // only respond to left click
                if (evt.button != (int)MouseButton.LeftMouse) return;

                property.serializedObject.Update();
                bool newVal = !property.boolValue;
                Undo.RecordObject(property.serializedObject.targetObject, "Toggle Switch");
                property.boolValue = newVal;
                property.serializedObject.ApplyModifiedProperties();
                UpdateVisuals();
                evt.StopPropagation();
            });

            // Ensure property label is present by wrapping with PropertyField-like container
            var wrapper = new VisualElement();
            wrapper.style.flexDirection = FlexDirection.Row;
            wrapper.style.alignItems = Align.Center;
            wrapper.style.flexGrow = 1;
            wrapper.style.paddingLeft = 2;
            wrapper.style.marginRight = -2;

            var labelElement = new Label(property.displayName);
            labelElement.style.unityTextAlign = TextAnchor.MiddleLeft;
            labelElement.style.flexGrow = 0;
            labelElement.style.alignSelf = Align.Center;
            labelElement.AddToClassList("TinyCustomLabelFixer");
            // set default inspector label width
            //labelElement.style.width = EditorGUIUtility.labelWidth;

            //wrapper.Add(labelElement);


            // Force Hide & Custom Label support
            var hideLabel = fieldInfo.GetCustomAttributes(typeof(HideLabelAttribute), true);
            var customLabel = fieldInfo.GetCustomAttribute<CustomLabelAttribute>();
            var tinyLabel = customLabel != null
                ? new TinyLabel(customLabel.Label, icon: (Texture2D)TinyIcons.GetIcon(customLabel.Icon))
                : new TinyLabel(property.displayName);

            if (hideLabel.Length == 0) wrapper.Add(tinyLabel);

            //wrapper.Add(new TinyLabel(property.displayName + "CHUJ"));

            if (!attr.Expand)
            {
                // add spacer to push root to the far right
                var spacer = new VisualElement();
                spacer.style.flexGrow = 1;
                wrapper.Add(spacer);

                wrapper.Add(root);

                // If not expanding, make container size to content
                root.style.flexGrow = 0;
                root.style.flexShrink = 0;
                root.style.width = StyleKeyword.Auto;
            }
            else
            {
                wrapper.Add(root);
                root.style.flexGrow = 1;
                root.style.flexShrink = 1;
            }

            return wrapper;
        }
    }
}
#endif

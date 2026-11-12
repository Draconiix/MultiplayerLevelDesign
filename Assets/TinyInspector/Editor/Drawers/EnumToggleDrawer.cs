using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyInspector
{
    [CustomPropertyDrawer(typeof(EnumToggleAttribute))]
    public class EnumToggleDrawer : PropertyDrawer
    {
        private const float buttonHeight = 17f;
        private const int maxPerLine = 3; // limit to 3 items per line

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            if (property.propertyType != SerializedPropertyType.Enum)
            {
                // fallback to default field for non-enum properties
                var fallback = new TinyPropertyField(property);
                root.Add(fallback);
                return root;
            }

            root.style.marginLeft = 2;
            root.style.marginRight = -2;

            // detect [Flags]
            bool isFlags = false;
            Array enumValues = null;
            if (fieldInfo != null && fieldInfo.FieldType != null && fieldInfo.FieldType.IsEnum)
            {
                isFlags = Attribute.IsDefined(fieldInfo.FieldType, typeof(FlagsAttribute));
                enumValues = Enum.GetValues(fieldInfo.FieldType);
            }

            // Main row: label on the left, buttons on the right
            var mainRow = new VisualElement();
            mainRow.style.flexDirection = FlexDirection.Row;
            mainRow.style.alignItems = Align.Center;
            root.Add(mainRow);

            // Label (left)
            /*var label = new Label(property.displayName);
            label.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Normal);
            // Use the same width as other Unity property labels
            label.style.width = EditorGUIUtility.labelWidth;
            label.style.marginRight = 4;
            label.style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleLeft);
            mainRow.Add(label);*/


            // Force Hide & Custom Label support
            var hideLabel = fieldInfo.GetCustomAttributes(typeof(HideLabelAttribute), true);
            var attr = fieldInfo.GetCustomAttribute<CustomLabelAttribute>();
            var tinyLabel = attr != null
                ? new TinyLabel(attr.Label, icon: (Texture2D)TinyIcons.GetIcon(attr.Icon))
                : new TinyLabel(property.displayName);

            if (hideLabel.Length == 0) mainRow.Add(tinyLabel);



            // Container for button rows (right side)
            var rightContainer = new VisualElement();
            rightContainer.style.flexDirection = FlexDirection.Column;
            rightContainer.style.flexGrow = 1;
            rightContainer.style.marginLeft = 2;
            mainRow.Add(rightContainer);

            var rowsContainer = new VisualElement();
            rowsContainer.style.flexDirection = FlexDirection.Column;
            rowsContainer.style.marginTop = 0;
            rowsContainer.style.flexGrow = 1;
            rightContainer.Add(rowsContainer);

            var enumNames = property.enumDisplayNames;
            int enumCount = enumNames.Length;

            // Build rows with up to maxPerLine buttons each
            int idx = 0;
            while (idx < enumCount)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.marginTop = 0;
                row.style.flexGrow = 0; // don't force rows to grow vertically
                row.style.alignItems = Align.Stretch; // ensure children stretch to fill height
                rowsContainer.Add(row);

                int items = Math.Min(maxPerLine, enumCount - idx);
                for (int i = 0; i < items; i++)
                {
                    int currentIndex = idx + i;

                    // compute mask for flags (or use index for non-flags)
                    int userValue;
                    if (isFlags && enumValues != null)
                    {
                        object ev = enumValues.GetValue(currentIndex);
                        userValue = Convert.ToInt32(ev);
                    }
                    else
                    {
                        userValue = currentIndex;
                    }

                    var btn = new Button(() =>
                    {
                        if (isFlags)
                        {
                            int mask = userValue;
                            if (mask == 0)
                            {
                                property.intValue = 0;
                            }
                            else
                            {
                                int newMask = property.intValue ^ mask;
                                property.intValue = newMask;
                            }
                        }
                        else
                        {
                            property.enumValueIndex = userValue;
                        }

                        property.serializedObject.ApplyModifiedProperties();
                        // Refresh visuals
                        UpdateButtons(rowsContainer, property, isFlags);
                    })
                    { text = enumNames[currentIndex] };

                    btn.userData = userValue;

                    // Layout: make equal widths by using flexGrow and zero flexBasis
                    btn.style.flexGrow = 1;
                    btn.style.flexShrink = 1;
                    btn.style.flexBasis = new StyleLength(new Length(0f));
                    btn.style.height = buttonHeight;

                    btn.style.marginRight = 0;
                    btn.style.marginBottom = 0;
                    btn.style.marginLeft = 0;
                    btn.style.marginTop = 0;

                    row.Add(btn);
                }

                idx += items;
            }

            // Initial visual update
            UpdateButtons(rowsContainer, property, isFlags);

            return root;
        }

        private void UpdateButtons(VisualElement rowsContainer, SerializedProperty property, bool isFlags)
        {
            foreach (VisualElement row in rowsContainer.Children())
            {
                foreach (VisualElement child in row.Children())
                {
                    if (child.userData is int userVal)
                    {
                        bool isActive;
                        if (isFlags)
                        {
                            int mask = userVal;
                            isActive = (property.intValue & mask) != 0;
                        }
                        else
                        {
                            int index = userVal;
                            isActive = (index == property.enumValueIndex);
                        }

                        // Background
                        child.style.backgroundColor = isActive ? new StyleColor(TinyInspectorStyles.Instance.GetAccentColor(TinyColor.Default)) : new StyleColor(EditorGUIUtility.isProSkin ? new Color(88f / 255f, 88f / 255f, 88f / 255f, 1f) : new Color(228 / 255f, 228 / 255f, 228 / 255f, 1f));
                        child.style.fontSize = 10;
                        // Opacity
                        child.style.opacity = isActive ? 1f : 0.5f;

                        // Text color and font style
                        child.style.color = isActive ? new StyleColor(TinyInspectorStyles.Instance.GetAccentTextColor(TinyColor.Default)) : new StyleColor(TinyInspectorStyles.LabelColor);
                        child.style.unityFontStyleAndWeight = isActive ? new StyleEnum<FontStyle>(FontStyle.Bold) : new StyleEnum<FontStyle>(FontStyle.Normal);

                        // Center text vertically/horizontally
                        child.style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter);
                    }
                }
            }
        }
    }
}

using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace TinyInspector
{
    [CustomPropertyDrawer(typeof(SuffixAttribute))]
    public class SuffixDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var attr = (SuffixAttribute)attribute;

            // Root container: row layout
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.alignItems = Align.Center;

            // Field container takes remaining space
            var fieldContainer = new VisualElement();
            fieldContainer.style.flexGrow = 1;

            // Create a PropertyField for the property and bind it
            var propField = new TinyPropertyField(property);
            propField.Bind(property.serializedObject);
            fieldContainer.Add(propField);

            // Suffix label
            var suffixLabel = new Label(attr.suffix ?? string.Empty);
            suffixLabel.style.fontSize = 9;
            suffixLabel.style.marginLeft = 2f;
            suffixLabel.style.unityTextAlign = TextAnchor.MiddleLeft;

            var icon = TinyIcons.GetIcon(attr.icon);

            if (attr.overlay)
            {
                // Make sure absolute-positioned children are positioned relative to this container
                fieldContainer.style.position = Position.Relative;

                // Add some right padding so the field's content doesn't overlap the overlay
                // fieldContainer.style.paddingRight = 20;

                // Position suffix inside the field on the right
                suffixLabel.style.position = Position.Absolute;
                suffixLabel.style.right = 6;
                suffixLabel.style.top = 4;
                suffixLabel.style.alignSelf = Align.Center;
                suffixLabel.style.unityTextAlign = TextAnchor.MiddleRight;

                fieldContainer.Add(propField);
                fieldContainer.Add(suffixLabel);

                if (icon != null)
                {
                    var iconImage = new VisualElement
                    {
                        style =
                        {
                            width = 11,
                            height = 11,
                            position = Position.Absolute,
                            right = 8,
                            top = 4,
                            alignSelf = Align.Center
                        }
                    };
                    iconImage.style.backgroundImage = (Texture2D)icon;
                    iconImage.style.unityBackgroundImageTintColor = TinyInspectorStyles.LabelColor;
                    suffixLabel.style.right = 18;
                    fieldContainer.Add(iconImage);
                }

                container.Add(fieldContainer);
            }
            else
            {
                // Default: suffix and icon placed next to the field
                container.Add(fieldContainer);
                container.Add(suffixLabel);

                if (icon != null)
                {
                    var iconImage = new VisualElement
                    {
                        style =
                        {
                            width = 12,
                            height = 12,
                            marginRight = 2
                        }
                    };
                    iconImage.style.backgroundImage = (Texture2D)icon;
                    iconImage.style.unityBackgroundImageTintColor = TinyInspectorStyles.LabelColor;

                    container.Add(iconImage);
                }
            }

            return container;
        }
    }
}

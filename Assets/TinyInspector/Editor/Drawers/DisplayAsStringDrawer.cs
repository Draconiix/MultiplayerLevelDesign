using System.Reflection;
using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#endif

namespace TinyInspector
{
    [CustomPropertyDrawer(typeof(DisplayAsStringAttribute))]
    public class DisplayAsStringDrawer : PropertyDrawer
    {
        // No IMGUI drawing - UI Toolkit usage only.
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Intentionally blank to enforce UI Toolkit usage only.
        }

#if UNITY_EDITOR
        // UI Toolkit implementation
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var attr = (DisplayAsStringAttribute)attribute;

            // If not a string property, fall back to default PropertyField (UI Toolkit)
            if (property.propertyType != SerializedPropertyType.String)
            {
                var fallback = new PropertyField(property);
                fallback.Bind(property.serializedObject);
                return fallback;
            }

            var container = new VisualElement();
            container.style.marginLeft = 3;

            // If a SpacerAttribute is present on the same field, add a spacer above
            var spacerAttr = fieldInfo.GetCustomAttribute<PropertySpaceAttribute>();
            if (spacerAttr != null)
            {
                var spacer = new VisualElement();
                spacer.style.height = spacerAttr.Height;
                spacer.style.minHeight = spacerAttr.Height;
                spacer.style.flexShrink = 0;
                container.Add(spacer);
            }

            // root row (single line)
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.AddToClassList("unity-property-field__inspector-property");

            // If label enabled, reserve prefix area equal to EditorGUIUtility.labelWidth
            if (attr.EnableLabel)
            {
                var prefixContainer = new VisualElement();
                float labelW = EditorGUIUtility.labelWidth;
                prefixContainer.style.width = labelW;
                prefixContainer.style.minWidth = labelW;
                prefixContainer.style.maxWidth = labelW;
                prefixContainer.style.flexDirection = FlexDirection.Row;
                prefixContainer.style.alignItems = Align.Center;

                // optional icon inside prefix
                if (attr.Icon != TinyIcon.None)
                {
                    var tex = TinyIcons.GetIcon(attr.Icon);
                    if (tex != null)
                    {
                        var img = new VisualElement();
                        img.style.width = 16;
                        img.style.height = 16;
                        img.style.marginRight = 2;
                        img.style.alignSelf = Align.Center;
                        img.style.backgroundImage = (Texture2D)tex;
                        img.style.unityBackgroundImageTintColor = TinyInspectorStyles.LabelColor;
                        prefixContainer.Add(img);
                    }
                }

                var prefixLabel = new Label(property.displayName);
                prefixLabel.style.flexGrow = 1;
                prefixLabel.style.whiteSpace = WhiteSpace.NoWrap;
                prefixLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
                prefixContainer.Add(prefixLabel);


                var label = new TinyLabel(property.displayName, attr.Icon != TinyIcon.None ? (Texture2D)TinyIcons.GetIcon(attr.Icon) : null);

                row.Add(label);
            }
            else
            {
                // If label not enabled but icon present, show icon with natural size (no reserved prefix)
                if (attr.Icon != TinyIcon.None)
                {
                    var tex = TinyIcons.GetIcon(attr.Icon);
                    if (tex != null)
                    {
                        var img = new VisualElement();
                        img.style.width = 16;
                        img.style.height = 16;
                        img.style.marginRight = 2;
                        img.style.alignSelf = Align.Center;
                        img.style.backgroundImage = (Texture2D)tex;
                        img.style.unityBackgroundImageTintColor = TinyInspectorStyles.LabelColor;
                        row.Add(img);
                    }
                }
            }

            // value label bound to the string property
            var valueLabel = new Label();
            valueLabel.name = "display-as-string-value";
            //valueLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            valueLabel.style.flexGrow = 1;
            valueLabel.style.whiteSpace = WhiteSpace.Normal; // allow wrapping if needed
            valueLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            // make it look like simple label (no extra padding)
            valueLabel.style.paddingLeft = 0;
            valueLabel.style.paddingRight = 0;

            if (attr.EnableLabel) valueLabel.style.unityTextAlign = TextAnchor.MiddleLeft;

            // set initial text immediately so it's visible even before binding applies
            valueLabel.text = property.stringValue;

            // bind the label to the serialized property so it updates automatically
            valueLabel.bindingPath = property.propertyPath;

            row.Add(valueLabel);

            // Bind the container so the bound label updates when the serialized object changes
            row.Bind(property.serializedObject);

            container.Add(row);

            return container;
        }
#endif

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}

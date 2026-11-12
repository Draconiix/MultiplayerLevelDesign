using UnityEditor;
using UnityEngine;
using Codice.Client.BaseCommands;

#if UNITY_EDITOR
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#endif

namespace TinyInspector
{
    [CustomPropertyDrawer(typeof(MultilineTextAreaAttribute))]
    public sealed class ResizeableTextAreaDrawer : PropertyDrawer
    {
#if UNITY_EDITOR
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                var fallback = new PropertyField(property);
                fallback.Bind(property.serializedObject);
                return fallback;
            }

            var attr = (MultilineTextAreaAttribute)attribute;

            TextField field;
            VisualElement root;

            root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;

            var LabelContainer = new VisualElement();
            LabelContainer.style.flexDirection = FlexDirection.Column;
            LabelContainer.style.flexGrow = 1;
            LabelContainer.style.flexShrink = 0;

            field = new TextField()
            {
                multiline = true,
                value = property.stringValue
            };

            var label = new TinyLabel(property.displayName);
            LabelContainer.Add(label);
            var label2 = new Label ($"Limit: {field.text.Length}/{attr.MaxCharacter}");
            label2.style.opacity = 0.7f;
            label2.style.fontSize = 10;
            label2.style.height = 10;
            label2.style.marginLeft = 2;
            if(attr.MaxCharacter > 0)LabelContainer.Add(label2);
            root.Add(LabelContainer);



            field.style.flexGrow = 0;
            field.style.flexShrink = 1;
            root.Add(field);

            if (attr.FullWidth)
            {
                root.style.flexDirection = FlexDirection.Column;
                LabelContainer.style.flexDirection = FlexDirection.Row;
                LabelContainer.style.justifyContent = Justify.SpaceBetween;
                LabelContainer.style.alignItems = Align.Center;

            }

            field.bindingPath = property.propertyPath;
            field.Bind(property.serializedObject);

            field.style.whiteSpace = WhiteSpace.Normal;
            //field.style.flexGrow = 1;

            float ResolveLineHeight()
            {

                var input = field.Q<VisualElement>("unity-text-input");
                if (input?.resolvedStyle.unityFont != null)
                {
                    var fontSize = input.resolvedStyle.fontSize;
                    var lh = EditorGUIUtility.singleLineHeight;
                    if (fontSize > 0)
                        lh = Mathf.Max(lh, fontSize - 2f);
                    return lh;
                }

                return EditorGUIUtility.singleLineHeight-3;
            }

            void UpdateHeight()
            {
                float lineHeight = ResolveLineHeight();

                float h = (lineHeight * attr.StartLines - 1) + 1f;

                field.style.minHeight = h;
                field.style.height = h;
            }

            void SetDefaultHeight()
            {
                UpdateHeight();
            }

            // Start at MinLines height, then grow when user edits / layout changes.
            field.schedule.Execute(SetDefaultHeight).ExecuteLater(0);

            if (attr.MaxCharacter >= 0)
            {
                field.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue != null && evt.newValue.Length > attr.MaxCharacter)
                    {
                        field.SetValueWithoutNotify(evt.newValue.Substring(0, attr.MaxCharacter));
                        property.serializedObject.ApplyModifiedProperties();
                    }
                    UpdateHeight();
                    label2.text = $"Limit: {field.text.Length}/{attr.MaxCharacter}";
                });
            }
            else
            {
                field.RegisterValueChangedCallback(_ => UpdateHeight());
            }
            field.RegisterCallback<GeometryChangedEvent>(_ => UpdateHeight());

            return root;
        }

#endif
    }
}

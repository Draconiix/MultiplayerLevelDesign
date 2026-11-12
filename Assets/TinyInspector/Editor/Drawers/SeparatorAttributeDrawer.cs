using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace TinyInspector
{
    [CustomPropertyDrawer(typeof(SeparatorAttribute))]
    public class SeparatorAttributeDrawer : PropertyDrawer
    {
        // UI Toolkit implementation
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var attr = (SeparatorAttribute)attribute;

            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Column;

            // Top spacing
            if (attr.SpacingTop > 0)
            {
                var top = new VisualElement();
                top.style.height = attr.SpacingTop;
                top.style.minHeight = attr.SpacingTop;
                top.style.flexShrink = 0;
                root.Add(top);
            }

            // Line
            var line = new VisualElement();
            line.style.height = attr.LineHeight;
            var defualtColor = TinyInspectorStyles.LabelColor;
            defualtColor.a = 0.3f;
            Color lineColor = attr.Color == TinyColor.Default ? defualtColor : TinyInspectorStyles.Instance.GetAccentColor(attr.Color);
            line.style.backgroundColor = lineColor;
            line.style.borderBottomLeftRadius = 2;
                        line.style.borderBottomRightRadius = 2;
            line.style.borderTopLeftRadius = 2;
            line.style.borderTopRightRadius = 2;
            root.Add(line);

            // Bottom spacing
            if (attr.SpacingBottom > 0)
            {
                var bot = new VisualElement();
                bot.style.height = attr.SpacingBottom;
                bot.style.minHeight = attr.SpacingBottom;
                bot.style.flexShrink = 0;
                root.Add(bot);
            }

            // Property field below
            var propField = new TinyPropertyField(property);
            propField.Bind(property.serializedObject);
            root.Add(propField);

            return root;
        }
    }
}

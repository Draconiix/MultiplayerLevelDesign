using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace TinyInspector
{
    [CustomPropertyDrawer(typeof(InfoBoxAttribute), true)]
    public class InfoBoxDrawer : PropertyDrawer
    {
        const float kPadding = 1f;

        // UI Toolkit implementation
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var attr = (InfoBoxAttribute)attribute;

            var root = new VisualElement();

            // Create the info box VisualElement from helper
            var infoBox = InfoBoxElement.Create(attr.title, attr.message, attr.type);
            root.Add(infoBox);

            // Property field below
            var propField = new TinyPropertyField(property);
            propField.Bind(property.serializedObject);
            root.Add(propField);

            return root;
        }

        private Texture GetIconForType(InfoBoxType type)
        {
           switch (type)
            {
                case InfoBoxType.Warning:
                    return TinyIcons.GetIcon(TinyIcon.Warning);
                case InfoBoxType.Error:
                    return TinyIcons.GetIcon(TinyIcon.Error);
                case InfoBoxType.Success:
                    return TinyIcons.GetIcon(TinyIcon.Success);
                case InfoBoxType.Info:
                default:
                    return TinyIcons.GetIcon(TinyIcon.Info);
            }
        }
    }
}
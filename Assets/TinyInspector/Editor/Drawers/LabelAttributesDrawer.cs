using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyInspector
{
    [CustomPropertyDrawer(typeof(CustomLabelAttribute))]
    public class CustomLabelAttributeDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var attr = (CustomLabelAttribute)attribute;

            var tex = TinyIcons.GetIcon(attr.Icon) as Texture2D;
            var field = new TinyPropertyField(property, attr.Label, tex);

            return field;
        }
    }

    [CustomPropertyDrawer(typeof(HideLabelAttribute))]
    public class HideLabelAttributeDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // Create a PropertyField with an empty label to hide the label
            var field = new TinyPropertyField(property, string.Empty);
            return field;
        }
    }
}

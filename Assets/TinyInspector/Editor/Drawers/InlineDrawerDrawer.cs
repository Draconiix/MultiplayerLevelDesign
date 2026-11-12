 using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
namespace TinyInspector
{
    [CustomPropertyDrawer(typeof(InlineDrawerAttribute))]
    public class InlineDrawerDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var attr = (InlineDrawerAttribute)attribute;

            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Column;
            root.style.borderLeftWidth = 1;
            root.style.borderRightWidth = 1;
            root.style.borderTopWidth = 1;
            root.style.borderBottomWidth = 1;
            root.style.marginBottom = 1;
            root.style.paddingLeft = 2;
            root.style.paddingRight = 2;
            root.style.paddingTop = 2;
            root.style.paddingBottom = 2;

            // Header
            var header = new Label(property.displayName);
            header.AddToClassList("simpledrawer-header");
            root.Add(header);

            var iterator = property.Copy();
            var end = iterator.GetEndProperty();

            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
            {
                enterChildren = false; // tylko pierwszy raz wg³¹b

                if (iterator.name == "m_Script")
                    continue;

                var field = new PropertyField(iterator);
                field.AddToClassList("InlineProperty");
                root.Add(field);
            }


            return root;
        }
    }
}
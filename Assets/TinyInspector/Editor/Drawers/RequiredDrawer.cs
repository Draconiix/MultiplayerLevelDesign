#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine;

namespace TinyInspector
{
    [CustomPropertyDrawer(typeof(RequiredAttribute))]
    public class RequiredDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // Create a container that will hold optional InfoBox and the property field
            var container = new VisualElement();

            var attr = this.attribute as RequiredAttribute;
            bool optional = !attr.isRequired;

            string infoTitle = optional ? "Warning" : "Error";
            string infoText = "This field is required.";

            // Create the InfoBox using the project's styled helper (always create so we can toggle)
            var infoElement = InfoBoxElement.Create(infoTitle, infoText, optional ? InfoBoxType.Warning : InfoBoxType.Error);
            // Initially hide; we'll compute initial visibility below
            infoElement.style.display = DisplayStyle.None;
            container.Add(infoElement);

            // Add the actual property field
            var field = new TinyPropertyField(property);
            container.Add(field);

            // We'll poll the serialized property while the element is attached to update visibility in real-time
            string path = property.propertyPath;
            var so = property.serializedObject;

            System.Action updateAction = () =>
            {
                if (so == null) return;
                so.Update();
                var p = so.FindProperty(path);
                if (p == null) return;

                bool showInfo = false;
                if (p.propertyType == SerializedPropertyType.ObjectReference)
                {
                    if (p.objectReferenceValue == null) showInfo = true;
                }
                else if (p.propertyType == SerializedPropertyType.String)
                {
                    if (string.IsNullOrEmpty(p.stringValue)) showInfo = true;
                }
                else if (p.isArray)
                {
                    if (p.arraySize == 0) showInfo = true;
                }
                else
                {
                    switch (p.propertyType)
                    {
                        case SerializedPropertyType.Integer:
                            if (p.intValue == 0) showInfo = true;
                            break;
                        case SerializedPropertyType.Float:
                            if (Mathf.Approximately(p.floatValue, 0f)) showInfo = true;
                            break;
                        case SerializedPropertyType.Boolean:
                            if (!p.boolValue) showInfo = true;
                            break;
                        default:
                            break;
                    }
                }

                infoElement.style.display = showInfo ? DisplayStyle.Flex : DisplayStyle.None;
            };

            EditorApplication.CallbackFunction callback = () => updateAction();

            // Attach / detach handlers to subscribe/unsubscribe the update loop
            container.RegisterCallback<AttachToPanelEvent>((evt) =>
            {
                // initial update
                updateAction();
                EditorApplication.update += callback;
            });

            container.RegisterCallback<DetachFromPanelEvent>((evt) =>
            {
                EditorApplication.update -= callback;
            });

            return container;
        }
    }
}
#endif

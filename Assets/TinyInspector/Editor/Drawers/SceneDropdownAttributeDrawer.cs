using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyInspector
{
    [CustomPropertyDrawer(typeof(SceneDropdownAttribute))]
    public class SceneDropdownAttributeDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            var scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .ToArray();

            var scenePaths = scenes.Select(s => s.path).ToArray();
            var sceneNames = scenePaths
                .Select(p => System.IO.Path.GetFileNameWithoutExtension(p))
                .ToArray();

            bool isInt = property.propertyType == SerializedPropertyType.Integer;
            bool isString = property.propertyType == SerializedPropertyType.String;

            if (!isInt && !isString)
            {
                root.Add(new PropertyField(property));
                return root;
            }

            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.marginLeft = 2;
            root.Add(container);

            // Force Hide & Custom Label support
            var hideLabel = fieldInfo.GetCustomAttributes(typeof(HideLabelAttribute), true);
            var attr = fieldInfo.GetCustomAttribute<CustomLabelAttribute>();
            var tinyLabel = attr != null
                ? new TinyLabel(attr.Label, icon: (Texture2D)TinyIcons.GetIcon(attr.Icon))
                : new TinyLabel(property.displayName);

            if (hideLabel.Length == 0) container.Add(tinyLabel);

            // Options
            var options = new string[sceneNames.Length + 1];
            options[0] = "(None)";

            for (int i = 0; i < sceneNames.Length; i++)
            {
                options[i + 1] = isInt
                    ? $"{sceneNames[i]} [{i}]"
                    : sceneNames[i];
            }

            int currentIndex = -1;

            if (isString)
            {
                if (!string.IsNullOrEmpty(property.stringValue))
                {
                    currentIndex = System.Array.IndexOf(scenePaths, property.stringValue);
                    if (currentIndex < 0)
                        currentIndex = System.Array.IndexOf(sceneNames, property.stringValue);
                }
            }
            else // int
            {
                currentIndex = property.intValue;
                if (currentIndex < -1 || currentIndex >= sceneNames.Length)
                    currentIndex = -1;
            }

            var popup = new PopupField<string>(
                //property.displayName,
                options.ToList(),
                currentIndex + 1
            );

            popup.style.flexGrow = 1;
            popup.style.marginLeft = 2;

            popup.RegisterValueChangedCallback(evt =>
            {
                int newIndex = System.Array.IndexOf(options, evt.newValue) - 1;


                if (isString)
                {
                    property.stringValue = newIndex >= 0
                        ? scenePaths[newIndex]
                        : string.Empty;
                }
                else
                {
                    property.intValue = newIndex; // -1 = None
                }

                property.serializedObject.ApplyModifiedProperties();
            });

            container.Add(popup);
            return root;
        }
    }
}

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace TinyInspector
{
    public static class InfoBoxElement
    {
        const float kPadding = 1f;

        // Backwards-compatible overload: accept InfoBoxAttribute
        public static VisualElement Create(InfoBoxAttribute attr)
        {
            return Create(attr.title, attr.message, attr.type);
        }

        // Create and return only the info box VisualElement. Drawing/binding the property is left to the caller.
        public static VisualElement Create(string title, string message, InfoBoxType type)
        {
            var infoBox = new HelpBox();
            infoBox.AddToClassList("InfoBox");
            // Make the InfoBox height equal to two inspector property lines.
            // Two property lines stacked have height = singleLineHeight * 2 + standardVerticalSpacing.
            float totalHeight = (EditorGUIUtility.singleLineHeight * 2f) + EditorGUIUtility.standardVerticalSpacing + kPadding * 2f;
            infoBox.style.height = totalHeight;

            // Set left border color based on type
            var leftColor = TinyInspectorStyles.Instance.GetInfoBoxIconColor(type);
            //infoBox.style.borderLeftColor = new StyleColor(leftColor);

            // Left square for icon
            var squareSize = totalHeight - kPadding * 2f;
            var iconContainer = new HelpBox();
            iconContainer.style.width = squareSize;
            iconContainer.style.height = squareSize;
            iconContainer.style.flexShrink = 0;
            iconContainer.AddToClassList("InfoBoxIconBox");
            iconContainer.style.borderRightColor = TinyInspectorStyles.BorderColor;

            // Icon image
            var tex = GetIconForType(type) as Texture2D;
            if (tex != null)
            {
                var img = new VisualElement();
                img.style.backgroundImage = tex;
                float iconSize = squareSize * 0.7f;
                img.style.width = iconSize+1;
                img.style.height = iconSize;
                img.AddToClassList("InfoBoxIcon");
                // Tint icon to match left border color
                img.style.unityBackgroundImageTintColor = new StyleColor(leftColor);
                iconContainer.Add(img);
            }

            if(type!=InfoBoxType.None)infoBox.Add(iconContainer);

            // Text container to the right of the icon
            var textContainer = new VisualElement();
            textContainer.style.flexDirection = FlexDirection.Column;
            textContainer.style.marginLeft = 4f;
            textContainer.style.flexGrow = 1;

            var titleLabel = new Label(title ?? string.Empty);
            titleLabel.AddToClassList("InfoBoxTitle");
            titleLabel.style.whiteSpace = WhiteSpace.Normal;

            var messageLabel = new Label(string.IsNullOrEmpty(message) ? string.Empty : message);
            messageLabel.AddToClassList("InfoBoxMessage");
            // show only one line and truncate the rest with ellipsis (handled by USS)
            messageLabel.style.whiteSpace = WhiteSpace.NoWrap;
            messageLabel.style.overflow = Overflow.Hidden;

            textContainer.Add(titleLabel);
            if (message != "") textContainer.Add(messageLabel);

            infoBox.Add(textContainer);

            // Hide built-in HelpBox labels that use the unity-help-box__label class
            infoBox.Query<Label>(className: "unity-help-box__label").ForEach(l => l.style.display = DisplayStyle.None);

            return infoBox;
        }

        private static Texture GetIconForType(InfoBoxType type)
        {
            switch (type)
            {
                case InfoBoxType.Warning:
                    return TinyIcons.GetIcon(TinyIcon.Warning);
                case InfoBoxType.Error:
                    return TinyIcons.GetIcon(TinyIcon.Error);
                case InfoBoxType.Success:
                    return TinyIcons.GetIcon(TinyIcon.Success);
                case InfoBoxType.Test:
                    return TinyIcons.GetIcon(TinyIcon.Lab);
                case InfoBoxType.Info:
                default:
                    return TinyIcons.GetIcon(TinyIcon.Info);
            }
        }
    }
}
#endif

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace TinyInspector
{
    [CustomPropertyDrawer(typeof(TitleAttribute))]
    public class TitleDrawer : PropertyDrawer
    {
        // Drawer-only offsets (kept as style hints)
        private const float topOffset = 3f;
        private const float bottomOffset = 3f;
        private const float lineGap = 2f;

        // UI Toolkit implementation
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var titleAttr = (TitleAttribute)attribute;

            // Root container
            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Column;

            // Try load stylesheet from Assets (project contains Resources/TinyInspector/... so we try known path)
            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/TinyInspector/Resources/TinyInspector/Editor/TinyInspector.uss");
            if (sheet != null)
                root.styleSheets.Add(sheet);

            // Spacer before property
            var spacer = new VisualElement();
            spacer.style.height = 4;
            root.Add(spacer);

            // Header area
            if (titleAttr.horizontal)
            {
                var headerRow = new VisualElement();
                headerRow.style.flexDirection = FlexDirection.Row;
                headerRow.style.alignItems = Align.Center;

                var left = new VisualElement();
                left.style.flexGrow = 1;
                left.style.flexShrink = 1;
                left.style.flexBasis = Length.Percent(50);
                left.style.alignItems = Align.Center;
                left.style.flexDirection = FlexDirection.Row;

                // Icon
                Texture iconTex = TinyIcons.GetIcon(titleAttr.icon);
                if (iconTex != null)
                {
                    var img = new VisualElement();
                    img.style.backgroundImage = new StyleBackground((Texture2D)iconTex);
                    img.style.width = 15;
                    img.style.height = 15;
                    img.style.marginRight = 2;
                    img.style.unityBackgroundImageTintColor = TinyInspectorStyles.LabelColor;
                    left.Add(img);
                }

                var titleLabel = new Label(titleAttr.title);
                titleLabel.AddToClassList("tinyinspector-title");
                titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                titleLabel.style.alignSelf = Align.FlexStart;
                left.Add(titleLabel);

                var right = new VisualElement();
                right.style.flexGrow = 1;
                right.style.flexShrink = 1;
                right.style.flexBasis = Length.Percent(50);
                right.style.alignItems = Align.Center;
                right.style.justifyContent = Justify.FlexEnd;
                right.style.alignItems = Align.Stretch;

                var descLabel = new Label(titleAttr.desc ?? string.Empty);
                descLabel.style.fontSize = 9;
                descLabel.style.unityTextAlign = TextAnchor.MiddleRight;
                descLabel.style.maxHeight = 18;
                descLabel.style.unityOverflowClipBox = OverflowClipBox.ContentBox;
                right.Add(descLabel);

                headerRow.Add(left);
                headerRow.Add(right);
                root.Add(headerRow);
            }
            else
            {
                // If description present, show icon larger and to the left spanning both title and desc
                bool hasDesc = !string.IsNullOrEmpty(titleAttr.desc);
                Texture iconTex = TinyIcons.GetIcon(titleAttr.icon);

                if (hasDesc && iconTex != null)
                {
                    var headerRow = new VisualElement();
                    headerRow.style.flexDirection = FlexDirection.Row;
                    headerRow.style.alignItems = Align.FlexStart;

                    var img = new VisualElement();
                    // Make icon taller to span title + desc (approximate two lines)
                    float iconHeight = 28;
                    img.style.backgroundImage = new StyleBackground((Texture2D)iconTex);
                    img.style.width = iconHeight; // square
                    img.style.height = iconHeight;
                    img.style.marginRight = 2;
                    img.style.alignSelf = Align.FlexStart;
                    img.style.unityBackgroundImageTintColor = TinyInspectorStyles.LabelColor;

                    var textColumn = new VisualElement();
                    textColumn.style.flexDirection = FlexDirection.Column;
                    textColumn.style.flexGrow = 1;

                    var titleLabel = new Label(titleAttr.title);
                    titleLabel.AddToClassList("tinyinspector-title");
                    titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                    titleLabel.style.marginBottom = 0;

                    var descLabel = new Label(titleAttr.desc);
                    descLabel.style.fontSize = 9;
                    descLabel.style.whiteSpace = WhiteSpace.Normal;
                    descLabel.style.unityTextAlign = TextAnchor.MiddleLeft;

                    textColumn.Add(titleLabel);
                    textColumn.Add(descLabel);

                    headerRow.Add(img);
                    headerRow.Add(textColumn);

                    root.Add(headerRow);
                }
                else
                {
                    var header = new VisualElement();
                    header.style.flexDirection = FlexDirection.Row;
                    header.style.alignItems = Align.Center;

                    if (iconTex != null)
                    {
                        var img = new VisualElement();
                        img.style.backgroundImage = new StyleBackground((Texture2D)iconTex);
                        img.style.width = 15;
                        img.style.height = 15;
                        img.style.marginRight = 2;
                        img.style.unityBackgroundImageTintColor = TinyInspectorStyles.LabelColor;
                        header.Add(img);
                    }

                    var titleLabel = new Label(titleAttr.title);
                    titleLabel.AddToClassList("tinyinspector-title");
                    titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                    header.Add(titleLabel);
                    root.Add(header);

                    if (!string.IsNullOrEmpty(titleAttr.desc))
                    {
                        var descLabel = new Label(titleAttr.desc);
                        descLabel.style.fontSize = 9;
                        descLabel.style.whiteSpace = WhiteSpace.Normal;
                        descLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
                        descLabel.style.marginTop = 2;
                        root.Add(descLabel);
                    }
                }
            }

            // Optional thin line
            if (titleAttr.drawLine)
            {
                var divider = new VisualElement();
                divider.style.height = 2;
                var lineColor = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.06f) : new Color(0f, 0f, 0f, 0.18f);
                divider.style.backgroundColor = lineColor;
                divider.style.marginBottom = lineGap;
                root.Add(divider);
            }

            // Property field
            var propField = new TinyPropertyField(property);
            propField.Bind(property.serializedObject);
            propField.style.marginTop = 2;
            root.Add(propField);

            return root;
        }
    }
}

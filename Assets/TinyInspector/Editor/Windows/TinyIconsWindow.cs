using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace TinyInspector
{
    public class TinyIconsWindow : EditorWindow
    {
        private ScrollView scrollView;
        private ToolbarSearchField searchField;
        private IntegerField iconSizeField;
        private VisualElement iconsContainer;
        private Label statusLabel;

        // Preview
        private VisualElement IconPreviewDark;
        private VisualElement IconPreviewLight;
        private Label IconNamePreview;
        private Label IconIDPreview;
        private Label IconSizePreview;
        private int selectedIconIndex = 1;


        private string search = string.Empty;
        private int iconSize = 48;
        private int padding = 2;

        private List<(string name, Texture tex)> cachedIcons = new List<(string name, Texture tex)>();
        private int totalIcons = 0;

        [MenuItem("Tools/Tiny Inspector/Icons Viewer")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<TinyIconsWindow>("Tiny Inspector Icon Viewer");
            wnd.titleContent = new GUIContent("Tiny Inspector Icon Viewer", EditorGUIUtility.IconContent("d_ViewToolZoom").image);
            wnd.minSize = new Vector2(300, 200);
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Tiny Inspector Icon Viewer", EditorGUIUtility.IconContent("d_ViewToolZoom").image);

            rootVisualElement.Clear();
            BuildUI();
            LoadStyleSheetIfExists();
            CollectIcons();
            RefreshIcons();
        }

        private void BuildUI()
        {
            // Toolbar
            var toolbar = new Toolbar();
            toolbar.AddToClassList("TinyToolbar");


            searchField = new ToolbarSearchField();
            searchField.tooltip = "Filter icons by name";
            searchField.RegisterValueChangedCallback(evt =>
            {
                search = evt.newValue ?? string.Empty;
                RefreshIcons();
            });
            searchField.style.minWidth = 200;
            searchField.style.marginLeft = 6;
            searchField.AddToClassList("TinyToolbarField");
            toolbar.Add(searchField);

            // left spacer to push the rest to the right
            var leftSpacer = new VisualElement();
            leftSpacer.style.flexGrow = 1;
            toolbar.Add(leftSpacer);

            var sizeSlider = new SliderInt(16, 128);
            sizeSlider.value = iconSize;
            sizeSlider.style.width = 150;
            sizeSlider.RegisterValueChangedCallback(evt =>
            {
                iconSize = evt.newValue;
                iconSizeField.SetValueWithoutNotify(iconSize);
                UpdateIconStyles();
            });
            sizeSlider.AddToClassList("TinyToolbarField");
            toolbar.Add(sizeSlider);

            // Icon size controls
            iconSizeField = new IntegerField();
            iconSizeField.value = iconSize;
            iconSizeField.RegisterCallback<FocusOutEvent>(_ =>
            {
                iconSize = Mathf.Clamp(iconSizeField.value, 16, 128);
                iconSizeField.SetValueWithoutNotify(iconSize);
                UpdateIconStyles();
            });
            iconSizeField.AddToClassList("TinyToolbarField");
            iconSizeField.style.width = 32;
            iconSizeField.style.marginLeft = 6;
            iconSizeField.style.marginRight = 6;
            iconSizeField.style.unityTextAlign = TextAnchor.MiddleCenter;
            toolbar.Add(iconSizeField);

            // Refresh button and status label on the far right

            statusLabel = new Label();
            statusLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            //toolbar.Add(statusLabel);


            var refreshBtn = new ToolbarButton(() =>
            {
                CollectIcons(forceReload: true);
                RefreshIcons();
            })
            { text = "Refresh" };

            refreshBtn.style.width = 80;
            refreshBtn.AddToClassList("TinyToolbarButton");

            toolbar.Add(refreshBtn);

            rootVisualElement.Add(toolbar);

            // Scroll view + icons container
            scrollView = new ScrollView();
            scrollView.style.flexGrow = 1;
            iconsContainer = new VisualElement();
            iconsContainer.style.flexDirection = FlexDirection.Row;
            iconsContainer.style.flexWrap = Wrap.Wrap;
            iconsContainer.style.alignItems = Align.FlexStart;
            iconsContainer.style.justifyContent = Justify.SpaceBetween;
            iconsContainer.style.paddingLeft = padding;
            iconsContainer.style.paddingTop = padding;
            scrollView.Add(iconsContainer);
            rootVisualElement.Add(scrollView);

            // Icon Preview
            var previewContainer = new VisualElement();
            previewContainer.style.minHeight = 61;
            //previewContainer.style.backgroundColor = TinyInspectorStyles.ContainerColor;
            previewContainer.style.flexDirection = FlexDirection.Row;
            previewContainer.style.borderTopColor = TinyInspectorStyles.BorderColor;
            previewContainer.style.borderTopWidth = 1;
            previewContainer.style.alignItems = Align.Center;
            rootVisualElement.Add(previewContainer);

            IconPreviewDark = new VisualElement();
            IconPreviewDark.AddToClassList("IconPreviewIcon");
            IconPreviewDark.style.borderBottomColor = TinyInspectorStyles.BorderColor;
            IconPreviewDark.style.borderTopColor = TinyInspectorStyles.BorderColor;
            IconPreviewDark.style.borderLeftColor = TinyInspectorStyles.BorderColor;
            IconPreviewDark.style.borderRightColor = TinyInspectorStyles.BorderColor;
            IconPreviewDark.style.backgroundColor = new Color(65 / 255f, 65 / 255f, 65 / 255f);
            IconPreviewDark.style.unityBackgroundImageTintColor = new Color(210 / 255f, 210 / 255f, 210 / 255f);
            previewContainer.Add(IconPreviewDark);


            IconPreviewLight = new VisualElement();
            IconPreviewLight.AddToClassList("IconPreviewIcon");
            IconPreviewLight.style.borderBottomColor = TinyInspectorStyles.BorderColor;
            IconPreviewLight.style.borderTopColor = TinyInspectorStyles.BorderColor;
            IconPreviewLight.style.borderLeftColor = TinyInspectorStyles.BorderColor;
            IconPreviewLight.style.borderRightColor = TinyInspectorStyles.BorderColor;
            IconPreviewLight.style.backgroundColor = new Color(200 / 255f, 200 / 255f, 200 / 255f);
            IconPreviewLight.style.unityBackgroundImageTintColor = new Color(32 / 255f, 32 / 255f, 32 / 255f);
            previewContainer.Add(IconPreviewLight);

            var TextContainer = new VisualElement();
            TextContainer.style.flexDirection = FlexDirection.Column;
            TextContainer.style.marginLeft = 6;
            previewContainer.Add(TextContainer);

            IconNamePreview = new Label();
            IconNamePreview.style.fontSize = 18;
            IconNamePreview.style.unityFontStyleAndWeight = FontStyle.Bold;
            IconNamePreview.style.unityTextAlign = TextAnchor.MiddleLeft;
            TextContainer.Add(IconNamePreview);

            IconIDPreview = new Label();
            IconIDPreview.style.fontSize = 10;
            IconIDPreview.style.unityTextAlign = TextAnchor.MiddleLeft;
            TextContainer.Add(IconIDPreview);

            IconSizePreview = new Label();
            IconSizePreview.style.fontSize = 10;
            IconSizePreview.style.unityTextAlign = TextAnchor.MiddleLeft;
            TextContainer.Add(IconSizePreview);
        }

        private void LoadStyleSheetIfExists()
        {
#if UNITY_EDITOR
            // Try load StyleSheet from Assets (project contains Resources/TinyInspector/... so we try known path)
            var path = "Assets/TinyInspector/Resources/TinyInspector/Editor/TinyInspector.uss";
            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            if (sheet != null)
            {
                rootVisualElement.styleSheets.Add(sheet);
            }
#endif
        }

        private void CollectIcons(bool forceReload = false)
        {
            if (cachedIcons.Count > 0 && !forceReload) return;

            cachedIcons.Clear();
            totalIcons = 0;

            Type enumType = null;
            Type tinyIconsType = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var t = asm.GetType("TinyInspector.TinyIcon");
                    if (t != null && t.IsEnum)
                    {
                        enumType = t;
                    }
                    if (tinyIconsType == null)
                    {
                        var tt = asm.GetType("TinyInspector.TinyIcons");
                        if (tt != null) tinyIconsType = tt;
                    }
                }
                catch { }
            }

            string[] names = new string[0];
            if (enumType != null)
                names = Enum.GetNames(enumType);

            totalIcons = names.Length;

            foreach (var name in names)
            {
                Texture tex = null;
                if (tinyIconsType != null)
                {
                    try
                    {
                        var mi = tinyIconsType.GetMethod("GetIcon", new Type[] { typeof(string) });
                        if (mi != null)
                        {
                            tex = mi.Invoke(null, new object[] { name }) as Texture;
                        }
                        else
                        {
                            var mi2 = tinyIconsType.GetMethod("GetIcon", new Type[] { enumType });
                            if (mi2 != null && enumType != null)
                            {
                                var val = Enum.Parse(enumType, name);
                                tex = mi2.Invoke(null, new object[] { val }) as Texture;
                            }
                        }
                    }
                    catch { }
                }

                cachedIcons.Add((name, tex));
            }
        }

        private void RefreshIcons()
        {
            iconsContainer.Clear();

            var filtered = cachedIcons.Where(item =>
                string.IsNullOrEmpty(search) || item.name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            //foreach (var item in filtered)
            for(int i = 1; i < filtered.Count; i++)
            {
                int index = i;
                var cell = CreateIconCell(filtered[i].name, filtered[i].tex, index);
                iconsContainer.Add(cell);
            }

            statusLabel.text = $"Find {totalIcons-1} Icons";
            UpdateIconStyles();
        }

        private VisualElement CreateIconCell(string name, Texture tex, int index)
        {
            var cell = new VisualElement();
            cell.RegisterCallback<ClickEvent>(evt => ChangePreviewIcon(index));
            cell.style.flexDirection = FlexDirection.Column;
            cell.style.alignItems = Align.Center;
            cell.style.justifyContent = Justify.FlexStart;
            cell.style.marginRight = 2;
            cell.style.marginLeft = 2;
            cell.style.marginBottom = 4;

            var img = new Image();
            img.image = tex;
            img.scaleMode = ScaleMode.ScaleToFit;
            img.style.width = iconSize *1.25f;
            img.style.height = iconSize * 1.25f;
            img.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            img.style.paddingTop = iconSize/8f;
            img.style.paddingBottom = iconSize / 8f;
            img.style.paddingRight = iconSize / 8f;
            img.style.paddingLeft = iconSize / 8f;
            img.style.borderTopWidth = 1;
            img.style.borderBottomWidth = 1;
            img.style.borderLeftWidth = 1;
            img.style.borderRightWidth = 1;
            img.style.borderBottomLeftRadius = 3;
            img.style.borderBottomRightRadius = 3;
            img.style.borderTopLeftRadius = 3;
            img.style.borderTopRightRadius = 3;
            img.style.borderTopColor = new Color(0, 0, 0, 0.15f);
            img.style.borderBottomColor = new Color(0, 0, 0, 0.15f);
            img.style.borderLeftColor = new Color(0, 0, 0, 0.15f);
            img.style.borderRightColor = new Color(0, 0, 0, 0.15f);
            img.style.backgroundColor = new Color(0, 0, 0, 0.03f);
            img.tooltip = $"{name}\nID: {(int)(TinyIcon)Enum.Parse(typeof(TinyIcon), name)}";

            if (tex == null)
            {
                var placeholder = new Label("no tex");
                placeholder.style.unityTextAlign = TextAnchor.MiddleCenter;
                placeholder.style.width = iconSize;
                placeholder.style.height = iconSize;
                placeholder.style.marginTop = 0;
                placeholder.style.backgroundColor = new Color(0, 0, 0, 0.02f);
                cell.Add(placeholder);
            }
            else
            {
                cell.Add(img);
            }

            var lbl = new Label(name);
            lbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            lbl.style.width = iconSize + padding;
            lbl.style.whiteSpace = WhiteSpace.Normal;
            lbl.style.fontSize = 9;
            lbl.style.marginTop = 1;
            lbl.style.marginBottom = 3;

            
            //cell.Add(lbl);

            return cell;
        }

        private void ChangePreviewIcon(int index)
        {
            selectedIconIndex = index;

            IconPreviewDark.style.backgroundImage = (Texture2D)cachedIcons[index].tex;
            IconPreviewLight.style.backgroundImage = (Texture2D)cachedIcons[index].tex;
            IconNamePreview.text = $"{cachedIcons[index].name}";
            IconIDPreview.text =   $"ID:       {(int)(TinyIcon)Enum.Parse(typeof(TinyIcon), cachedIcons[index].name)}";
            IconSizePreview.text = $"Size:   {cachedIcons[index].tex.height}x{cachedIcons[index].tex.width}";
        }

        private void UpdateIconStyles()
        {
            // update existing cell sizes
            foreach (var child in iconsContainer.Children())
            {
                if (child.childCount > 0)
                {
                    var first = child.ElementAt(0) as VisualElement;
                    if (first != null)
                    {
                        first.style.width = iconSize;
                        first.style.height = iconSize;
                    }

                    // label width
                    if (child.childCount > 1)
                    {
                        var lbl = child.ElementAt(1) as Label;
                        if (lbl != null)
                        {
                            lbl.style.width = iconSize + padding;
                        }
                    }
                }
            }
        }
    }
}
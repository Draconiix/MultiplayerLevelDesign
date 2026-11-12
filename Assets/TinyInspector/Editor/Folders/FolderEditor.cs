using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyInspector.ColoredFolders
{
    public class FolderEditor : EditorWindow
    {
        static FolderDataList FolderDataList;

        private const int WINDOW_WIDTH = 210;

        private static FolderEditor _instance;
        private static List<Color> _colors;
        private static Texture2D[] _colorIcons;

        public static System.Action<Color> OnColorSelected;
        public static System.Action<int> OnMarkSelected;

        public static void Open(Vector2 position)
        {
            CloseExistingWindow();
            LoadColors();
            RebuildColorIcons();

            Vector2 windowSize = CalculatePopupSize();
            ShowPopupWindow(position, windowSize);
        }

        private void OnEnable()
        {
            if (rootVisualElement != null)
            {
                RebuildUi();
            }
        }

        private void CreateGUI()
        {
            RebuildUi();
        }

        private void OnDisable()
        {
            _instance = null;
        }

        private void OnLostFocus()
        {
            CloseEditorWindow();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                CloseEditorWindow();
            }
        }

        private void OnGUI()
        {
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                ClearColorIcons();
            }
        }

        private static void CloseExistingWindow()
        {
            if (_instance != null)
            {
                _instance.Close();
                _instance = null;
            }
        }

        private static void ShowPopupWindow(Vector2 position, Vector2 size)
        {
            _instance = CreateInstance<FolderEditor>();
            _instance.position = new Rect(position.x, position.y, size.x, size.y);
            _instance.minSize = size;
            _instance.ShowPopup();
        }

        private static Vector2 CalculatePopupSize()
        {
            int colorRowCount = Mathf.CeilToInt((_colors.Count + 1) / (float)8);
            int markerRowCount = CountMarkerRows();
            int totalHeight = (colorRowCount + markerRowCount) * 30 + 22;

            return new Vector2(WINDOW_WIDTH, totalHeight);
        }

        private static int CountMarkerRows()
        {
            if (FolderDataList == null) return 0;

            var groups = FolderDataList.Icons.GroupBy(entry => entry.IconCategory);
            int totalMarkerRows = 0;

            bool isFirstGroup = true;
            foreach (var group in groups)
            {
                int groupIconCount = group.Count();
                if (isFirstGroup)
                {
                    groupIconCount += 1; // None icon button
                    isFirstGroup = false;
                }
                int iconRows = Mathf.CeilToInt(groupIconCount / (float)8);
                totalMarkerRows += iconRows;
            }

            return totalMarkerRows;
        }

        private static void LoadColors()
        {
            LoadMarkerLibrary();
            _colors = FolderDataList.Colors;
        }

        private static void LoadMarkerLibrary()
        {
            if (FolderDataList == null)
            {
                FolderDataList = Resources.Load<FolderDataList>("TinyInspector/Colored Folders");
                if (FolderDataList == null)
                {
                    Debug.LogWarning($"Customizable Folders can't find settings file!");
                }
            }
        }

        private static void RebuildColorIcons()
        {
            ClearColorIcons();

            if (_colors == null || _colors.Count == 0) return;

            _colorIcons = new Texture2D[_colors.Count];

            for (int i = 0; i < _colors.Count; i++)
            {
                _colorIcons[i] = CreateSolidColorTexture(_colors[i]);
            }
        }

        private static Texture2D CreateSolidColorTexture(Color color)
        {
            var texture = new Texture2D(24, 24, TextureFormat.ARGB32, false)
            {
                wrapMode = TextureWrapMode.Clamp
            };

            Color[] pixels = new Color[24 * 24];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static void ClearColorIcons()
        {
            if (_colorIcons == null) return;

            foreach (var icon in _colorIcons)
            {
                if (icon != null)
                {
                    DestroyImmediate(icon);
                }
            }
            _colorIcons = null;
        }

        private void CloseEditorWindow()
        {
            Close();
            _instance = null;
        }

        private void RebuildUi()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.style.paddingLeft = 4;
            rootVisualElement.style.paddingRight = 4;
            rootVisualElement.style.paddingTop = 2;
            rootVisualElement.style.paddingBottom = 4;

            rootVisualElement.style.borderBottomLeftRadius = 4;
            rootVisualElement.style.borderBottomRightRadius = 4;
            rootVisualElement.style.borderTopLeftRadius = 4;
            rootVisualElement.style.borderTopRightRadius = 4;

            rootVisualElement.style.borderBottomWidth = 1;
            rootVisualElement.style.borderBottomColor = TinyInspectorStyles.BorderColor;
            rootVisualElement.style.borderTopWidth = 1;
            rootVisualElement.style.borderTopColor = TinyInspectorStyles.BorderColor;
            rootVisualElement.style.borderLeftWidth = 1;
            rootVisualElement.style.borderLeftColor = TinyInspectorStyles.BorderColor;
            rootVisualElement.style.borderRightWidth = 1;
            rootVisualElement.style.borderRightColor = TinyInspectorStyles.BorderColor;

            if (FolderDataList == null)
            {
                LoadMarkerLibrary();
            }

            if (FolderDataList == null)
            {
                return;
            }

            var colorLabel = new Label("Folder Color");
            colorLabel.style.marginTop = 2;
            colorLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            colorLabel.style.fontSize = 10;
            rootVisualElement.Add(colorLabel);

            var colorsGrid = CreateIconGrid();
            rootVisualElement.Add(colorsGrid);

            PopulateColorButtons(colorsGrid);

            var spacer = new VisualElement();
            spacer.style.height = 8;
            rootVisualElement.Add(spacer);

            var iconLabel = new Label("Folder Icon");
            iconLabel.style.marginTop = 2;
            iconLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            iconLabel.style.fontSize = 10;
            rootVisualElement.Add(iconLabel);

            var markersRoot = new VisualElement();
            markersRoot.style.flexDirection = FlexDirection.Column;
            rootVisualElement.Add(markersRoot);

            PopulateMarkerButtons(markersRoot);
        }

        private static VisualElement CreateIconGrid()
        {
            var grid = new VisualElement();
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.flexWrap = Wrap.Wrap;
            grid.style.alignItems = Align.FlexStart;
            grid.style.marginBottom = 2;
            return grid;
        }

        private static Button CreateIconButton(Texture icon, bool isColor, bool isDefault, System.Action onClick)
        {
            var button = new Button(onClick)
            {
                text = string.Empty
            };

            if(isDefault) button.tooltip = isColor ? "Set Color to Defualt" : "Remove Folder Icon";

            button.focusable = false;

            button.style.width = 24;
            button.style.height = 24;
            button.style.marginRight = 0;
            button.style.marginLeft = 1;
            button.style.marginBottom = 1;
            button.style.paddingLeft = 0;
            button.style.paddingRight = 0;
            button.style.paddingTop = 0;
            button.style.paddingBottom = 0;

            var texture2D = icon as Texture2D;
            if (texture2D != null)
            {
                button.style.backgroundImage = new StyleBackground(texture2D);
                if (!isColor || isDefault)
                {
                    button.style.backgroundSize = new BackgroundSize(20, 20);
                }
            }

            button.RegisterCallback<MouseUpEvent>(evt =>
            {
                if ((evt.modifiers & EventModifiers.Alt) == 0 || evt.button != (int)MouseButton.LeftMouse)
                {
                    return;
                }

                onClick?.Invoke();
                evt.StopImmediatePropagation();
            });

            return button;
        }

        private void PopulateColorButtons(VisualElement colorsGrid)
        {
            if (_colors == null) LoadColors();
            if (_colorIcons == null) RebuildColorIcons();

                colorsGrid.Add(CreateIconButton(EditorGUIUtility.IconContent("d_P4_DeletedLocal").image, true, true, () =>
                {
                    OnColorSelected?.Invoke(Color.clear);
                    CloseEditorWindow();
                }));

            if (_colorIcons == null || _colors == null) return;

            for (int i = 0; i < _colorIcons.Length && i < _colors.Count; i++)
            {
                int colorIndex = i;
                var icon = _colorIcons[i];
                colorsGrid.Add(CreateIconButton(icon, true, false, () =>
                {
                    OnColorSelected?.Invoke(_colors[colorIndex]);
                    CloseEditorWindow();
                }));
            }
        }

        private void PopulateMarkerButtons(VisualElement markersRoot)
        {
            if (FolderDataList?.Icons == null || FolderDataList.Icons.Count == 0) return;

            var groups = FolderDataList.Icons
                .Select((entry, index) => new { entry, index })
                .GroupBy(x => x.entry.IconCategory);

            bool addedNone = false;
            foreach (var group in groups)
            {
                var grid = CreateIconGrid();
                markersRoot.Add(grid);

                if (!addedNone )
                {
                    grid.Add(CreateIconButton(EditorGUIUtility.IconContent("d_P4_DeletedLocal").image, false, false, () =>
                    {
                        OnMarkSelected?.Invoke(-1);
                        CloseEditorWindow();
                    }));
                    addedNone = true;
                }

                foreach (var item in group)
                {
                    if (item.entry.Icon == null) continue;

                    int markerIndex = item.index;
                    var icon = item.entry.Icon;
                    grid.Add(CreateIconButton(icon, false, false, () =>
                    {
                        OnMarkSelected?.Invoke(markerIndex);
                        CloseEditorWindow();
                    }));
                }
            }
        }
    }
}
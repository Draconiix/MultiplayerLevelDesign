using UnityEditor;
using UnityEngine;
using System.Linq;
using System.IO;

namespace TinyInspector.ColoredFolders
{
    [InitializeOnLoad]
    public static class CustomizableFolder
    {
        private const float GRADIENT_START_ALPHA = 0.4f;
        private const float GRADIENT_END_ALPHA = 0.0f;
        private const int GRADIENT_WIDTH = 128;
        private const int GRADIENT_HEIGHT = 1;

        private static FolderDataList _markerLibrary;
        private static Texture2D _gradientTexture;
        private static Texture2D _whiteFolderIcon;
        private static Texture2D _whiteEmptyFolderIcon;
        private static Color _lastGradientColor = Color.clear;

        static CustomizableFolder()
        {
            InitializeFolderRendering();
        }

        private static void InitializeFolderRendering()
        {
            LoadResources();
            EditorApplication.projectWindowItemOnGUI += DrawProjectWindowItem;
        }

        private static void LoadResources()
        {
            LoadFolderData();
            LoadFolderIcons();
        }

        private static void LoadFolderData()
        {
            const string folderPath = "Assets/TinyInspector/Resources/TinyInspector";
            const string assetPath = "Assets/TinyInspector/Resources/TinyInspector/Colored Folders.asset";

            if (_markerLibrary == null)
            {
                _markerLibrary = AssetDatabase.LoadAssetAtPath<FolderDataList>(assetPath);

                if (_markerLibrary == null)
                {
                    // upewnij się że folder istnieje
                    if (!AssetDatabase.IsValidFolder(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                        AssetDatabase.Refresh();
                    }

                    // utwórz domyślny asset
                    _markerLibrary = ScriptableObject.CreateInstance<FolderDataList>();
                    AssetDatabase.CreateAsset(_markerLibrary, assetPath);
                    AssetDatabase.SaveAssets();

                    Debug.Log("Created default FolderDataList settings file.");
                }
            }
        }

        private static void LoadFolderIcons()
        {
            if (_whiteFolderIcon == null || _whiteEmptyFolderIcon == null)
            {
                _whiteFolderIcon = EditorGUIUtility.IconContent("d_Folder Icon").image as Texture2D;
                _whiteEmptyFolderIcon = EditorGUIUtility.IconContent("d_FolderEmpty Icon").image as Texture2D;
            }
        }

        private static void RefreshGradientTexture(Color baseColor)
        {
            if (_gradientTexture != null && baseColor.Equals(_lastGradientColor))
            {
                return;
            }

            ReleaseGradientTexture();
            CreateGradientTexture(baseColor);
            _lastGradientColor = baseColor;
        }

        private static void CreateGradientTexture(Color baseColor)
        {
            if (baseColor == Color.clear) return;

            _gradientTexture = new Texture2D(GRADIENT_WIDTH, GRADIENT_HEIGHT, TextureFormat.ARGB32, false)
            {
                wrapMode = TextureWrapMode.Clamp
            };

            Color[] pixels = new Color[GRADIENT_WIDTH * GRADIENT_HEIGHT];

            for (int x = 0; x < GRADIENT_WIDTH; x++)
            {
                float t = x / (float)(GRADIENT_WIDTH - 1);
                float alpha = Mathf.Lerp(GRADIENT_START_ALPHA, GRADIENT_END_ALPHA, t);
                pixels[x] = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            }

            _gradientTexture.SetPixels(pixels);
            _gradientTexture.Apply();
        }

        private static void ReleaseGradientTexture()
        {
            if (_gradientTexture != null)
            {
                Object.DestroyImmediate(_gradientTexture);
                _gradientTexture = null;
            }
        }

        private static void DrawProjectWindowItem(string guid, Rect rect)
        {
            LoadFolderData();

            if (_markerLibrary == null) return;
            if (!_markerLibrary.EnableCustomFolders) return;

            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!AssetDatabase.IsValidFolder(path)) return;

            bool isColumnView = rect.width > rect.height;

            DrawFolderColor(path, rect, isColumnView);
            DrawFolderMarker(path, rect, isColumnView);
            HandleFolderClick(path, rect);
        }

        private static void DrawFolderColor(string path, Rect rect, bool isColumnView)
        {
            LoadFolderData();
            var color = _markerLibrary.Folders.FirstOrDefault(f => f.FolderPath == path)?.FolderColor ?? Color.clear;

            if (isColumnView)
            {
                DrawColumnViewColor(color, rect);
            }
            else
            {
                DrawIconViewColor(path, color, rect);
            }
        }

        private static void DrawColumnViewColor(Color color, Rect rect)
        {
            RefreshGradientTexture(color);
            if (_gradientTexture == null) return;

            Rect fullRect = new Rect(rect.x - 160, rect.y, rect.width + 160, rect.height);
            GUI.DrawTexture(fullRect, _gradientTexture, ScaleMode.StretchToFill, true);
        }

        private static void DrawIconViewColor(string path, Color color, Rect rect)
        {
            bool isEmpty = AssetDatabase.FindAssets("", new[] { path }).Length == 0;

            var icon = isEmpty ? _whiteEmptyFolderIcon : _whiteFolderIcon;
            if (icon == null) return;

            Rect iconRect = new Rect(rect.x, rect.y, rect.width, rect.width);

            Color oldColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
            GUI.color = oldColor;
        }

        private static void DrawFolderMarker(string path, Rect rect, bool isColumnView)
        {
            var index = _markerLibrary.Folders.FirstOrDefault(f => f.FolderPath == path)?.MarkerID ?? -1;
            if (index == -1) return;

            var icon = _markerLibrary.Icons[index].Icon;
            if (icon == null) return;

            Rect iconRect = GetMarkerIconRect(rect, isColumnView);
            DrawOutlinedTexture(iconRect, icon, 2, isColumnView);
        }

        private static void DrawOutlinedTexture(Rect rect, Texture icon, int thickness, bool isColumnView)
        {
            Color oldColor = GUI.color;
            Color outlineColor = EditorGUIUtility.isProSkin
                ? new Color(51 / 255f, 51 / 255f, 51 / 255f, 1f)
                : new Color(190 / 255f, 190 / 255f, 190 / 255f, 1f);

            GUI.color = outlineColor;

            if (!isColumnView)
            {
                for (int x = -thickness; x <= thickness; x++)
                {
                    for (int y = -thickness; y <= thickness; y++)
                    {
                        if (x == 0 && y == 0) continue;

                        Rect offsetRect = new Rect(rect.x + x, rect.y + y, rect.width, rect.height);
                        GUI.DrawTexture(offsetRect, icon, ScaleMode.ScaleToFit);
                    }
                }
            }

            GUI.color = oldColor;
            GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit);
        }

        private static Rect GetMarkerIconRect(Rect rect, bool isColumnView)
        {
            float height = rect.height;
            float width = rect.width;

            LoadFolderData();

            float sizeMultiplier = isColumnView ? 0.5f : 0.35f;
            if (isColumnView) sizeMultiplier = 0.5f;            
            else if (_markerLibrary.FolderIconSize == FolderIconSize.Large) sizeMultiplier = 0.35f;       
            else if (_markerLibrary.FolderIconSize == FolderIconSize.Medium) sizeMultiplier = 0.30f;            
            else if (_markerLibrary.FolderIconSize == FolderIconSize.Small) sizeMultiplier = 0.25f;            
            
            
            
            float size = height * sizeMultiplier;

            float offsetX = 0;
            float offsetY = 0;

            if (isColumnView)
            {
                offsetX = rect.x + height - size + height * 0.1f;
                offsetY = rect.y + height - size;
            }
            else
            {
                if (_markerLibrary.FolderIconPosition == FolderIconPosition.TopLeft)
                {
                    offsetX = rect.x;
                    offsetY = rect.y + height * 0.2f - size * 0.5f;
                }
                if (_markerLibrary.FolderIconPosition == FolderIconPosition.MiddleLeft)
                {
                    offsetX = rect.x;
                    offsetY = rect.y + height * 0.45f - size * 0.5f;
                }
                if (_markerLibrary.FolderIconPosition == FolderIconPosition.BottomLeft)
                {
                    offsetX = rect.x;
                    offsetY = rect.y + height * 0.6f - size * 0.5f;
                }

                if (_markerLibrary.FolderIconPosition == FolderIconPosition.TopCenter)
                {
                    offsetX = (rect.xMax - size + rect.x)/2;
                    offsetY = rect.y + height * 0.24f - size * 0.5f;
                }
                if (_markerLibrary.FolderIconPosition == FolderIconPosition.MiddleCenter)
                {
                    offsetX = (rect.xMax - size + rect.x) / 2;
                    offsetY = rect.y + height * 0.475f - size * 0.5f;
                }
                if (_markerLibrary.FolderIconPosition == FolderIconPosition.BottomCenter)
                {
                    offsetX = (rect.xMax - size + rect.x) / 2;
                    offsetY = rect.y + height * 0.6f - size * 0.5f;
                }

                if (_markerLibrary.FolderIconPosition == FolderIconPosition.TopRight)
                {
                    offsetX = rect.xMax - size;
                    offsetY = rect.y + height * 0.275f - size * 0.5f;
                }
                if (_markerLibrary.FolderIconPosition == FolderIconPosition.MiddleRight)
                {
                    offsetX = rect.xMax - size;
                    offsetY = rect.y + height * 0.475f - size * 0.5f;
                }
                if (_markerLibrary.FolderIconPosition == FolderIconPosition.BottomRight)
                {
                    offsetX = rect.xMax - size;
                    offsetY = rect.y + height * 0.6f - size * 0.5f;
                }
            }

            return new Rect(offsetX, offsetY, size, size);
        }

        private static void HandleFolderClick(string path, Rect rect)
        {
            if (Event.current.type != EventType.MouseDown ||
                !rect.Contains(Event.current.mousePosition) ||
                Event.current.button != 0)
                return;

            if (!IsEditorHotkeyPressed()) return;

            Vector2 mouseScreenPos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
            OpenEditorPopup(path, mouseScreenPos);
            Event.current.Use();
        }

        private static bool IsEditorHotkeyPressed()
        {
            return Event.current.alt;
        }

        private static void OpenEditorPopup(string path, Vector2 mouseScreenPos)
        {
            FolderEditor.OnColorSelected = selectedColor =>
            {
                UpdateFolderColor(path, selectedColor);
            };

            FolderEditor.OnMarkSelected = selectedMarker =>
            {
                UpdateFolderMarker(path, selectedMarker);
            };

            FolderEditor.Open(mouseScreenPos);
        }

        private static void UpdateFolderColor(string path, Color selectedColor)
        {
            LoadFolderData();
            if (selectedColor.a == 0f || selectedColor == Color.clear)
            {
                var folder = _markerLibrary.Folders.Find(f => f.FolderPath == path);
                if (folder != null)
                {
                    folder.FolderColor = Color.clear;
                }
            }
            else
            {
                var folder = _markerLibrary.Folders.Find(f => f.FolderPath == path);

                if (folder != null)
                {
                    folder.FolderColor = selectedColor;
                }
                else
                {
                    _markerLibrary.Folders.Add(new FolderData
                    {
                        FolderPath = path,
                        FolderColor = selectedColor
                    });
                }
            }

            SaveChangesAndRepaint(_markerLibrary);
        }

        private static void UpdateFolderMarker(string path, int selectedMarker)
        {
            if (selectedMarker == 0)
            {
                var folder = _markerLibrary.Folders.Find(f => f.FolderPath == path);
                if (folder != null)
                {
                    folder.MarkerID = 0;
                }
            }
            else
            {
                var folder = _markerLibrary.Folders.Find(f => f.FolderPath == path);
                if (folder != null)
                {
                    folder.MarkerID = selectedMarker;
                }
                else
                { 
                    _markerLibrary.Folders.Add(new FolderData
                    {
                        FolderPath = path,
                        FolderColor = Color.clear,
                        MarkerID = selectedMarker
                    });
                }
            }

            SaveChangesAndRepaint(_markerLibrary);
        }

        private static void SaveChangesAndRepaint(FolderDataList list)
        {
            UnityEditor.EditorUtility.SetDirty(list);
            UnityEditor.AssetDatabase.SaveAssets();

            EditorApplication.RepaintProjectWindow();
        }
    }

    [InitializeOnLoad]
    public static class proFolderImportPrompt
    {
        static proFolderImportPrompt()
        {
            if (!SessionState.GetBool("TinyInspector_CustomizableFolders", false))
            {
                SessionState.SetBool("TinyInspector_CustomizableFolders", true);
                EditorApplication.delayCall += () =>
                {
                    EditorUtility.RequestScriptReload();
                };
            }
        }
    }
}
#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
namespace TinyInspector
{
    // Simple ScriptableObject to persist project-level Tiny Inspector settings
    public class TinyInspectorProjectSettings : ScriptableObject
    {
        [EnumToggle]
        public TinyColor Accent = TinyColor.Default;
        public Color InfoBoxIconColor = new Color(0.2f, 0.6f, 0.85f);
        public Color WarningBoxIconColor = new Color(0.95f, 0.75f, 0.2f);
        public Color ErrorBoxIconColor = new Color(0.9f, 0.3f, 0.3f);
        public Color SuccessBoxIconColor = new Color(0.3f, 0.85f, 0.4f);
        public Color DebugBoxIconColor = new Color(0.6f, 0.6f, 0.6f);

        [System.Serializable]
        public class ColorEntry
        {
            public TinyColor Name;

            public Color AccentColor = new Color(0.2f, 0.6f, 0.85f);
            public Color AccentTextColor = Color.white;

            public Color BoxHeaderColor = new Color(0.12f, 0.12f, 0.12f);
            public Color BoxContentColor = new Color(0.12f, 0.12f, 0.12f);
        }

        // Palette that can be edited in Project Settings
        //[TableList]
        public List<ColorEntry> ColorPalette = new List<ColorEntry>();

        internal const string k_AssetPath = "Assets/TinyInspector/Resources/TinyInspector/TinyInspectorSettings.asset";

        public static TinyInspectorProjectSettings LoadOrCreate()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TinyInspectorProjectSettings>(k_AssetPath);
            if (settings == null)
            {
                // Ensure folder exists
                var dir = System.IO.Path.GetDirectoryName(k_AssetPath);
                if (!AssetDatabase.IsValidFolder(dir))
                {
                    // create nested folders if needed
                    var parts = dir.Split('/');
                    string cur = "";
                    for (int i = 0; i < parts.Length; i++)
                    {
                        var part = parts[i];
                        if (i == 0) { cur = part; continue; }
                        var next = cur + "/" + part;
                        if (!AssetDatabase.IsValidFolder(next))
                        {
                            AssetDatabase.CreateFolder(cur, part);
                        }
                        cur = next;
                    }
                }

                settings = ScriptableObject.CreateInstance<TinyInspectorProjectSettings>();
                // Only initialize default palette when creating the asset for the first time
                EnsureDefaultPalette(settings);
                AssetDatabase.CreateAsset(settings, k_AssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            else
            {
                // Do not automatically modify existing settings asset (don't add missing defaults)
            }
            return settings;
        }

        internal static void EnsureDefaultPalette(TinyInspectorProjectSettings settings)
        {
            if (settings == null) return;

            var values = System.Enum.GetValues(typeof(TinyColor));
            foreach (TinyColor v in values)
            {
                bool found = false;
                foreach (var e in settings.ColorPalette)
                {
                    if (e.Name == v) { found = true; break; }
                }
                if (!found)
                {
                    var entry = new ColorEntry();
                    entry.Name = v;
                    // sensible defaults (light theme-like); dark theme will be approximated elsewhere
                    switch (v)
                    {
                        case TinyColor.Red:
                            entry.AccentColor = new Color(1f, 0.3f, 0.3f);
                            entry.BoxHeaderColor = new Color(0.45f, 0.12f, 0.12f);
                            entry.BoxContentColor = new Color(0.18f, 0.08f, 0.08f);
                            break;
                        case TinyColor.Orange:
                            entry.AccentColor = new Color(0.98f, 0.53f, 0.20f);
                            entry.BoxHeaderColor = new Color(0.45f, 0.28f, 0.12f);
                            entry.BoxContentColor = new Color(0.2f, 0.12f, 0.08f);
                            break;
                        case TinyColor.Yellow:
                            entry.AccentColor = new Color(1f, 0.85f, 0.35f);
                            entry.BoxHeaderColor = new Color(0.45f, 0.4f, 0.12f);
                            entry.BoxContentColor = new Color(0.22f, 0.18f, 0.08f);
                            break;
                        case TinyColor.Lime:
                            entry.AccentColor = new Color(0.52f, 0.8f, 0.09f);
                            entry.BoxHeaderColor = new Color(0.2f, 0.4f, 0.12f);
                            entry.BoxContentColor = new Color(0.12f, 0.24f, 0.08f);
                            break;
                        case TinyColor.Green:
                            entry.AccentColor = new Color(0.2f, 0.6f, 0.25f);
                            entry.BoxHeaderColor = new Color(0.12f, 0.36f, 0.18f);
                            entry.BoxContentColor = new Color(0.08f, 0.28f, 0.12f);
                            break;

                        case TinyColor.Teal:
                            entry.AccentColor = new Color(0.08f, 0.72f, 0.65f);
                            entry.BoxHeaderColor = new Color(0.06f, 0.36f, 0.34f);
                            entry.BoxContentColor = new Color(0.06f, 0.28f, 0.28f);
                            break;
                        case TinyColor.Cyan:
                            entry.AccentColor = new Color(0.02f, 0.71f, 0.83f);
                            entry.BoxHeaderColor = new Color(0.06f, 0.36f, 0.36f);
                            entry.BoxContentColor = new Color(0.06f, 0.28f, 0.32f);
                            break;

                        case TinyColor.Blue:
                            entry.AccentColor = new Color(0.2f, 0.6f, 0.85f);
                            entry.BoxHeaderColor = new Color(0.12f, 0.28f, 0.45f);
                            entry.BoxContentColor = new Color(0.08f, 0.16f, 0.28f);
                            break;
                        case TinyColor.Indigo:
                            entry.AccentColor = new Color(0.39f, 0.40f, 0.95f);
                            entry.BoxHeaderColor = new Color(0.2f, 0.18f, 0.4f);
                            entry.BoxContentColor = new Color(0.12f, 0.12f, 0.28f);
                            break;

                        case TinyColor.Purple:
                            entry.AccentColor = new Color(0.7f, 0.45f, 0.9f);
                            entry.BoxHeaderColor = new Color(0.36f, 0.16f, 0.32f);
                            entry.BoxContentColor = new Color(0.22f, 0.12f, 0.26f);
                            break;
                        case TinyColor.Fushsia:
                            entry.AccentColor = new Color(0.85f, 0.27f, 0.93f);
                            entry.BoxHeaderColor = new Color(0.36f, 0.12f, 0.36f);
                            entry.BoxContentColor = new Color(0.24f, 0.1f, 0.24f);
                            break;
                        case TinyColor.Pink:
                            entry.AccentColor = new Color(1f, 0.5f, 0.75f);
                            entry.BoxHeaderColor = new Color(0.44f, 0.18f, 0.28f);
                            entry.BoxContentColor = new Color(0.28f, 0.12f, 0.2f);
                            break;

                        case TinyColor.Slate:
                            entry.AccentColor = new Color(0.39f, 0.46f, 0.55f);
                            entry.BoxHeaderColor = new Color(0.18f, 0.22f, 0.28f);
                            entry.BoxContentColor = new Color(0.12f, 0.14f, 0.18f);
                            break;
                        case TinyColor.Default:
                        default:
                            entry.AccentColor = new Color(0.2f, 0.6f, 0.85f);
                            entry.BoxHeaderColor = new Color(0.12f, 0.12f, 0.12f);
                            entry.BoxContentColor = new Color(0.12f, 0.12f, 0.12f);
                            break;
                    }

                    settings.ColorPalette.Add(entry);
                }
            }
        }

        // Removed SettingsProvider: expose settings editing via a custom EditorWindow instead
    }

    // Custom Editor window to edit Tiny Inspector project settings (replaces Project Settings provider)
    public class TinyInspectorSettingsWindow : EditorWindow
    {
        private TinyInspectorProjectSettings settings;
        private SerializedObject so;
        private ScrollView scrollView;

        [MenuItem("Tools/Tiny Inspector/Settings")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<TinyInspectorSettingsWindow>("Tiny Inspector Settings");
            wnd.minSize = new Vector2(480, 200);
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Tiny Inspector Settings", EditorGUIUtility.IconContent("d_Settings").image);

            settings = TinyInspectorProjectSettings.LoadOrCreate();
            if (settings != null)
                so = new SerializedObject(settings);

            // Build UI when enabled
            if (rootVisualElement != null)
                BuildUI();
        }

        // Called by Unity UI Toolkit to construct UI
        public void CreateGUI()
        {
            if (settings == null)
                settings = TinyInspectorProjectSettings.LoadOrCreate();
            if (settings != null)
                so = new SerializedObject(settings);


            BuildUI();
        }

        private void BuildUI()
        {
            rootVisualElement.Clear();

            StyleSheet sheet = Resources.Load<StyleSheet>("TinyInspector/Editor/TinyInspector");
            if (sheet != null) rootVisualElement.styleSheets.Add(sheet);

            // Create a scrollable content area so fields don't get squashed
            var contentScroll = new ScrollView();
            contentScroll.style.flexGrow = 1;
            contentScroll.contentContainer.style.flexDirection = FlexDirection.Column;
            contentScroll.style.marginTop = 6;
            contentScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            rootVisualElement.style.paddingBottom = 6;
            rootVisualElement.style.paddingTop = 6;
            rootVisualElement.style.paddingLeft = 6;
            rootVisualElement.style.paddingRight = 0;

            var titleLabel = new Label("Tiny Inspector Settings");
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.fontSize = 24;
            contentScroll.Add(titleLabel);

            var InfoBox = InfoBoxElement.Create("Changes are not updated immediately!", "To see changes in color settings, you need to redisplay the currently open inspector", type: InfoBoxType.Warning);
            contentScroll.Add(InfoBox);

            if (so != null) so.Update();


            Subtitle("General Settings");

            // Add property fields into the scroll view (guard nulls)
            if (so != null)
            {
                var accentProp = so.FindProperty("Accent");
                if (accentProp != null)
                {
                    var propField = new TinyPropertyField(accentProp, labelText: "Defualt Accent Color");
                    contentScroll.Add(propField);
                }

                Subtitle("Attribute Settings");

                var infoProp = so.FindProperty("InfoBoxIconColor");
                if (infoProp != null) contentScroll.Add(new TinyPropertyField(infoProp, labelText: "Info Icon Color"));

                var warnProp = so.FindProperty("WarningBoxIconColor");
                if (warnProp != null) contentScroll.Add(new TinyPropertyField(warnProp, labelText: "Warning Icon Color"));

                var errProp = so.FindProperty("ErrorBoxIconColor");
                if (errProp != null) contentScroll.Add(new TinyPropertyField(errProp, labelText: "Error Icon Color"));

                var succProp = so.FindProperty("SuccessBoxIconColor");
                if (succProp != null) contentScroll.Add(new TinyPropertyField(succProp, labelText: "Success Icon Color"));

                var dbgProp = so.FindProperty("DebugBoxIconColor");
                if (dbgProp != null) contentScroll.Add(new TinyPropertyField(dbgProp, labelText: "Debug Icon Color"));
            }

            Subtitle("Color Pallette Settings");
            if (settings != null)
            {
                foreach (var entry in settings.ColorPalette)
                {
                    var rows = new VisualElement();
                    rows.style.flexDirection = FlexDirection.Row;
                    rows.style.marginBottom = 8;
                    rows.style.marginTop = 8;
                    rows.style.alignItems = Align.Stretch;


                    var preview = new VisualElement();
                    //preview.style.marginBottom = -6;
                    preview.style.width = 150;

                    var palletteHeader = new VisualElement();
                    palletteHeader.AddToClassList("BoxHeader");
                    palletteHeader.style.paddingLeft = 6;
                    palletteHeader.style.paddingRight = 6;
                    palletteHeader.style.paddingTop = 6;
                    palletteHeader.style.paddingBottom = 4;
                    palletteHeader.style.backgroundColor = entry.BoxHeaderColor;

                    var palletteLabel = new Label("Group Preview");
                    palletteLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                    palletteLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
                    //palletteLabel.style.color = TinyInspectorStyles.Instance.GetAccentTextColor(entry.Name);
                    palletteHeader.Add(palletteLabel);
                    preview.Add(palletteHeader);

                    var palletteContainer = new VisualElement();
                    palletteContainer.AddToClassList("BoxContent");
                    palletteContainer.style.paddingLeft = 6;
                    palletteContainer.style.paddingRight = 6;
                    palletteContainer.style.marginBottom = 2;
                    palletteContainer.style.backgroundColor = entry.BoxContentColor;
                    preview.Add(palletteContainer);

                    var ProgressBarPreview = new VisualElement();
                    ProgressBarPreview.style.height = 16;
                    ProgressBarPreview.style.marginBottom = 4;
                    ProgressBarPreview.style.borderBottomLeftRadius = 3;
                    ProgressBarPreview.style.borderBottomRightRadius = 3;
                    ProgressBarPreview.style.borderTopLeftRadius = 3;
                    ProgressBarPreview.style.borderTopRightRadius = 3;
                    ProgressBarPreview.style.backgroundColor = entry.AccentColor;

                    var ProgressBarText = new Label("Accent Text ");
                    ProgressBarText.style.color = entry.AccentTextColor;
                    ProgressBarText.style.unityFontStyleAndWeight = FontStyle.Bold;
                    ProgressBarText.style.fontSize = 10;
                    ProgressBarText.style.unityTextAlign = TextAnchor.MiddleCenter;
                    ProgressBarText.style.height = 16;
                    ProgressBarPreview.Add(ProgressBarText);

                    preview.Add(ProgressBarPreview);

                    palletteContainer.Add(new Label($"Color = {entry.Name.ToString()}"));

                    rows.Add(preview);



                    var cols = new VisualElement();
                    cols.style.flexDirection = FlexDirection.Column;
                    cols.style.flexGrow = 1;
                    cols.style.alignSelf = Align.Stretch;
                    cols.AddToClassList("TinyInspectorSettingsColorEntry");
                    cols.style.marginLeft = 8;
                    rows.Add(cols);

                    var row1 = new VisualElement();
                    row1.style.flexDirection = FlexDirection.Row;
                    row1.style.flexGrow = 1;
                    var row2 = new VisualElement();
                    row2.style.flexDirection = FlexDirection.Row;
                    row2.style.flexGrow = 1;
                    var row3 = new VisualElement();
                    row3.style.flexDirection = FlexDirection.Row;
                    row3.style.flexGrow = 1;
                    var row4 = new VisualElement();
                    row4.style.flexDirection = FlexDirection.Row;
                    row4.style.flexGrow = 1;

                    cols.Add(row1);
                    cols.Add(row2);
                    cols.Add(row3);
                    cols.Add(row4);

                    var lbl1 = new Label("Accent Background Color") { style = { fontSize = 10, flexGrow = 1 } };
                    lbl1.style.flexBasis = 0;
                    lbl1.style.minWidth = 0;
                    row1.Add(lbl1);
                    var lbl2 = new Label("Accent Text Color") { style = { fontSize = 10, flexGrow = 1 } };
                    lbl2.style.flexBasis = 0;
                    lbl2.style.minWidth = 0;
                    row1.Add(lbl2);

                    var accentField = new ColorField();
                    accentField.style.fontSize = 10;
                    accentField.value = entry.AccentColor;
                    accentField.RegisterValueChangedCallback(evt =>
                    {
                        entry.AccentColor = evt.newValue;
                        ProgressBarPreview.style.backgroundColor = entry.AccentColor;

                        SaveSettings();
                    });
                    accentField.style.flexGrow = 1;
                    accentField.style.flexBasis = 0;
                    accentField.style.minWidth = 0;
                    row2.Add(accentField);

                    var accentTextField = new ColorField();
                    accentTextField.style.fontSize = 10;
                    accentTextField.value = entry.AccentTextColor;
                    accentTextField.RegisterValueChangedCallback(evt =>
                    {
                        entry.AccentTextColor = evt.newValue;
                        ProgressBarText.style.color = entry.AccentTextColor;

                        SaveSettings();
                    });
                    accentTextField.style.flexGrow = 1;
                    accentTextField.style.flexBasis = 0;
                    accentTextField.style.minWidth = 0;
                    row2.Add(accentTextField);


                    var lbl3 = new Label("Group Header Color") { style = { fontSize = 10, flexGrow = 1 } };
                    lbl3.style.flexBasis = 0;
                    lbl3.style.minWidth = 0;
                    row3.Add(lbl3);
                    var lbl4 = new Label("Group Content Color") { style = { fontSize = 10, flexGrow = 1 } };
                    lbl4.style.flexBasis = 0;
                    lbl4.style.minWidth = 0;
                    row3.Add(lbl4);

                    var headerColorField = new ColorField();
                    headerColorField.style.flexGrow = 1;
                    headerColorField.style.fontSize = 10;
                    headerColorField.value = entry.BoxHeaderColor;
                    headerColorField.RegisterValueChangedCallback(evt =>
                    {
                        entry.BoxHeaderColor = evt.newValue;
                        palletteHeader.style.backgroundColor = entry.BoxHeaderColor;

                        SaveSettings();
                    });
                    headerColorField.style.flexBasis = 0;
                    headerColorField.style.minWidth = 0;
                    row4.Add(headerColorField);
                    var contentColorField = new ColorField();
                    contentColorField.style.flexGrow = 1;
                    contentColorField.style.fontSize = 10;
                    contentColorField.value = entry.BoxContentColor;
                    contentColorField.RegisterValueChangedCallback(evt =>
                    {
                        entry.BoxContentColor = evt.newValue;
                        palletteContainer.style.backgroundColor = entry.BoxContentColor;

                        SaveSettings();
                    });
                    contentColorField.style.flexBasis = 0;
                    contentColorField.style.minWidth = 0;
                    row4.Add(contentColorField);



                    contentScroll.Add(rows);
                }
            }

            rootVisualElement.Add(contentScroll);

            // Bind once after the UI tree is built
            if (so != null)
                rootVisualElement.Bind(so);




            void Subtitle(string text)
            {
                var subtitle = new Label(text);
                subtitle.style.unityFontStyleAndWeight = FontStyle.Bold;
                subtitle.style.marginTop = 8;
                contentScroll.Add(subtitle);
            }
            void SubSubtitle(string text)
            {
                var subtitle = new Label(text);
                subtitle.style.unityFontStyleAndWeight = FontStyle.Normal;
                subtitle.style.marginTop = 2;
                contentScroll.Add(subtitle);
            }

           
        }

        private void SaveSettings()
        {
            if (settings == null) return;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            if (so != null)
                so.ApplyModifiedProperties();

            // Rebuild UI to reflect changes
            
        }
    }
}

#endif
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyInspector.ColoredFolders
{
    public class ColoredFoldersWindow : EditorWindow
    {
        private FolderDataList settings;
        private SerializedObject so;
        private ScrollView scrollView;

        [MenuItem("Tools/Tiny Inspector/Colored Folder Settings")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<ColoredFoldersWindow>("Colored Folder Settings");
            wnd.minSize = new Vector2(480, 200);
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Colored Folder Settings", EditorGUIUtility.IconContent("Folder Icon").image);

            settings = Resources.Load<FolderDataList>("TinyInspector/Colored Folders");
            if (settings != null)
                so = new SerializedObject(settings);

            if (rootVisualElement != null)
                BuildUI();
        }

        public void CreateGUI()
        {
            if (settings == null)
                settings = Resources.Load<FolderDataList>("TinyInspector/Colored Folders");
            if (settings != null)
                so = new SerializedObject(settings);


            BuildUI();
        }

        private void BuildUI()
        {
            rootVisualElement.Clear();

            StyleSheet sheet = Resources.Load<StyleSheet>("TinyInspector/Editor/TinyInspector");
            if (sheet != null) rootVisualElement.styleSheets.Add(sheet);

            var contentScroll = new ScrollView();
            contentScroll.style.flexGrow = 1;
            contentScroll.contentContainer.style.flexDirection = FlexDirection.Column;
            contentScroll.style.marginTop = 6;
            contentScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            rootVisualElement.style.paddingBottom = 6;
            rootVisualElement.style.paddingTop = 6;
            rootVisualElement.style.paddingLeft = 6;
            rootVisualElement.style.paddingRight = 6;

            var titleLabel = new Label("Tiny Inspector Folders");
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.fontSize = 24;
            contentScroll.Add(titleLabel);

            Subtitle("Settings");
            var EnableCustomFolders = so.FindProperty("EnableCustomFolders");
            if (EnableCustomFolders != null) contentScroll.Add(new TinyPropertyField(EnableCustomFolders));
            var FolderIconPosition = so.FindProperty("FolderIconPosition");
            if (FolderIconPosition != null) contentScroll.Add(new TinyPropertyField(FolderIconPosition));
            var FolderIconSize = so.FindProperty("FolderIconSize");
            if (FolderIconSize != null) contentScroll.Add(new TinyPropertyField(FolderIconSize));

            contentScroll.Add(new VisualElement { style = { height = 16 } });

            Button te = new Button(() =>
            {
                var asset = Resources.Load<FolderDataList>("TinyInspector/Colored Folders");

                if (asset != null)
                {
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                }
            });
            te.style.height = 48;
            te.text = "Select Colored Folder SO";
            contentScroll.Add(te);

            rootVisualElement.Add(contentScroll);

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

        }
    }
}
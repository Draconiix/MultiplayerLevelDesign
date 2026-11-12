using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static UnityEditor.Timeline.TimelinePlaybackControls;

namespace TinyInspector.ColoredFolders
{
    [System.Serializable]
    public class FolderDataList : ScriptableObject
    {
        [PropertySpace(6), Switch("Disable", "Enable")]
        public bool EnableCustomFolders = true;
        [PropertySpace(6), EnumToggle]
        public FolderIconPosition FolderIconPosition = FolderIconPosition.BottomRight;
        [PropertySpace(6), EnumToggle]
        public FolderIconSize FolderIconSize = FolderIconSize.Large;


        [PropertySpace(8), Reorderable]
        public List<Color> Colors = new List<Color>();

        [TableList]
        public List<FolderData> Folders = new List<FolderData>();

        [TableList]
        public List<FolderIcons> Icons = new List<FolderIcons>();

        /*        public FolderDataList()
                {
                    ResetToDefault();
                }
        */

        void OnEnable()
        {
            if (Icons == null)
            {
                ResetToDefault();
            }
        }


        [Button("Reset To Default")]
        public void ResetToDefault()
        {
            EnableCustomFolders = true;
            FolderIconPosition = FolderIconPosition.BottomRight;
            FolderIconSize = FolderIconSize.Large;

            Colors.Clear();
            Colors.Add(Hex("#ec273fc8"));
            Colors.Add(Hex("#de5d3ac8"));
            Colors.Add(Hex("#e98537c8"));
            Colors.Add(Hex("#f3a833c8"));
            Colors.Add(Hex("#9de64ec8"));
            Colors.Add(Hex("#5ab552c8"));
            Colors.Add(Hex("#26854cc8"));
            Colors.Add(Hex("#006554c8"));
            Colors.Add(Hex("#3859b3c8"));
            Colors.Add(Hex("#3388dec8"));
            Colors.Add(Hex("#36c5f4c8"));
            Colors.Add(Hex("#6dead6c8"));
            Colors.Add(Hex("#cc99ffc8"));
            Colors.Add(Hex("#B078C8c8"));
            Colors.Add(Hex("#714D9Ac8"));

            Folders.Clear();

            Icons.Clear();
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/{TinyIcon.Annoucment.ToString()}.png"), IconCategory = "Default" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/{TinyIcon.Audio.ToString()}.png"), IconCategory = "Default" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/{TinyIcon.Bookmark.ToString()}.png"), IconCategory = "Default" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/{TinyIcon.Bot.ToString()}.png"), IconCategory = "Default" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/{TinyIcon.Box.ToString()}.png"), IconCategory = "Default" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/{TinyIcon.Camera.ToString()}.png"), IconCategory = "Default" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/{TinyIcon.Chat.ToString()}.png"), IconCategory = "Default" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/{TinyIcon.Cloth.ToString()}.png"), IconCategory = "Default" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/{TinyIcon.Cloud.ToString()}.png"), IconCategory = "Default" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/{TinyIcon.Code.ToString()}.png"), IconCategory = "Default" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/{TinyIcon.Download.ToString()}.png"), IconCategory = "Default" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/{TinyIcon.Edit.ToString()}.png"), IconCategory = "Default" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/{TinyIcon.Filter.ToString()}.png"), IconCategory = "Default" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/{TinyIcon.Fire.ToString()}.png"), IconCategory = "Default" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/{TinyIcon.Gamepad.ToString()}.png"), IconCategory = "Default" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/{TinyIcon.GPU.ToString()}.png"), IconCategory = "Default" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/{TinyIcon.Graph.ToString()}.png"), IconCategory = "Default" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/{TinyIcon.Grass.ToString()}.png"), IconCategory = "Default" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/{TinyIcon.Heart.ToString()}.png"), IconCategory = "Default" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/{TinyIcon.Image.ToString()}.png"), IconCategory = "Default" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/{TinyIcon.Key.ToString()}.png"), IconCategory = "Default" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/{TinyIcon.Lab.ToString()}.png"), IconCategory = "Default" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/{TinyIcon.Link.ToString()}.png"), IconCategory = "Default" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/{TinyIcon.Map.ToString()}.png"), IconCategory = "Default" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/{TinyIcon.Material.ToString()}.png"), IconCategory = "Default" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/{TinyIcon.Object.ToString()}.png"), IconCategory = "Default" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/{TinyIcon.Radioactive.ToString()}.png"), IconCategory = "Default" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/{TinyIcon.Save.ToString()}.png"), IconCategory = "Default" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/{TinyIcon.Script.ToString()}.png"), IconCategory = "Default" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/{TinyIcon.Search.ToString()}.png"), IconCategory = "Default" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/{TinyIcon.Settings.ToString()}.png"), IconCategory = "Default" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/{TinyIcon.Shield.ToString()}.png"), IconCategory = "Default" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/{TinyIcon.Star.ToString()}.png"), IconCategory = "Default" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/{TinyIcon.Tag.ToString()}.png"), IconCategory = "Default" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/{TinyIcon.Thunder.ToString()}.png"), IconCategory = "Default" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/{TinyIcon.Toolbox.ToString()}.png"), IconCategory = "Default" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/{TinyIcon.User.ToString()}.png"), IconCategory = "Default" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/{TinyIcon.Wrench.ToString()}.png"), IconCategory = "Default" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/{TinyIcon.WWW.ToString()}.png"), IconCategory = "Default" });

            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/Colored/ScriptableObject.png"), IconCategory = "Colored" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/Colored/Prefab.png"), IconCategory = "Colored" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/Colored/PrefabVariant.png"), IconCategory = "Colored" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/Colored/AudioClip.png"), IconCategory = "Colored" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/Colored/AudioListener.png"), IconCategory = "Colored" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/Colored/BoxCollider.png"), IconCategory = "Colored" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/Colored/GUI.png"), IconCategory = "Colored" });
            Icons.Add(new FolderIcons() { Icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/TinyInspector/Resources/TinyInspector/Colored/MeshCollider.png"), IconCategory = "Colored" });
        }

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }
    }



    [System.Serializable]
    public class FolderData
    {
        public string FolderPath;
        public Color FolderColor = Color.clear;
        public int MarkerID = -1;
    }
    [System.Serializable]
    public class FolderIcons
    {
        public Texture2D Icon;
        public string IconCategory;
    }




    [System.Serializable]
    public enum FolderIconPosition
    {
        TopLeft = 0,
        TopCenter = 1,
        TopRight = 2,
        MiddleLeft = 3,
        MiddleCenter = 4,
        MiddleRight = 5,
        BottomLeft =6 ,
        BottomCenter = 7,
        BottomRight = 8,
    }

    [System.Serializable]
    public enum FolderIconSize
    {
               Small = 0,
        Medium = 1,
        Large = 2,
    }
}
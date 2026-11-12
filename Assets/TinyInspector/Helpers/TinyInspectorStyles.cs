#if UNITY_EDITOR

using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TinyInspector
{
    public static class TinyInspectorStyles
    {
        public static Color LabelColor => EditorGUIUtility.isProSkin ? new Color(210 / 255f, 210 / 255f, 210 / 255f) :
            new Color(32 / 255f, 32 / 255f, 32 / 255f);
        public static Color BorderColor => EditorGUIUtility.isProSkin ? new Color(36 / 255f, 36 / 255f, 36 / 255f) : 
            new Color(161 / 255f, 161 / 255f, 161 / 255f);
        public static Color ContainerColor => EditorGUIUtility.isProSkin ? new Color(65 / 255f, 65 / 255f, 65 / 255f) : 
            new Color(200 / 255f, 200 / 255f, 200 / 255f);
        public static Color HeaderColor => EditorGUIUtility.isProSkin ? new Color(53 / 255f, 53 / 255f, 53 / 255f) :
            new Color(182 / 255f, 182 / 255f, 182 / 255f);
        public static Color FieldColor => EditorGUIUtility.isProSkin ? new Color(42 / 255f, 42 / 255f, 42 / 255f) :
            new Color(237 / 255f, 237 / 255f, 237 / 255f);
        public static Color ButtonColor => EditorGUIUtility.isProSkin ? new Color(88 / 255f, 88 / 255f, 88 / 255f) :
            new Color(228 / 255f, 228 / 255f, 228 / 255f);




        // Backwards-compatible API used by ProgressBarDrawer and other code
        public static TinyInspectorStylesInstance Instance { get; } = new TinyInspectorStylesInstance();

        public class TinyInspectorStylesInstance
        {
            public Color GetInfoBoxIconColor(InfoBoxType type)
            {
                var settings = AssetDatabase.LoadAssetAtPath<TinyInspectorProjectSettings>(TinyInspectorProjectSettings.k_AssetPath);
                switch (type)
                {
                    case InfoBoxType.Info:
                        return settings.InfoBoxIconColor;
                    case InfoBoxType.Warning:
                        return settings.WarningBoxIconColor;
                    case InfoBoxType.Error:
                        return settings.ErrorBoxIconColor;
                    case InfoBoxType.Success:
                        return settings.SuccessBoxIconColor;
                    case InfoBoxType.Test:
                        return settings.DebugBoxIconColor;
                    default:
                        return Color.white;
                }
            }

            public Color GetAccentColor(TinyColor color)
            {
                var settings = AssetDatabase.LoadAssetAtPath<TinyInspectorProjectSettings>(TinyInspectorProjectSettings.k_AssetPath);
                if (settings != null && settings.ColorPalette != null)
                {
                    if (color == TinyColor.Default) color = settings.Accent;

                    var e = settings.ColorPalette.FirstOrDefault(x => x.Name == color);
                    if (e != null) return e.AccentColor;
                }
                return new Color(0.2f, 0.6f, 0.85f);
            }
            public Color GetAccentTextColor(TinyColor color)
            {
                var settings = AssetDatabase.LoadAssetAtPath<TinyInspectorProjectSettings>(TinyInspectorProjectSettings.k_AssetPath);
                if (settings != null && settings.ColorPalette != null)
                {
                    if (color == TinyColor.Default) color = settings.Accent;

                    var e = settings.ColorPalette.FirstOrDefault(x => x.Name == color);
                    if (e != null) return e.AccentTextColor;
                }
                return new Color(1,1,1);
            }




            public Color GetBoxHeaderColor(TinyColor color)
            {
                var settings = AssetDatabase.LoadAssetAtPath<TinyInspectorProjectSettings>(TinyInspectorProjectSettings.k_AssetPath);
                if (settings != null && settings.ColorPalette != null)
                {
                    var e = settings.ColorPalette.FirstOrDefault(x => x.Name == color);
                    if (e != null) return e.BoxHeaderColor;
                }
                return HeaderColor;
            }
            public Color GetBoxContentColor(TinyColor color)
            {
                var settings = AssetDatabase.LoadAssetAtPath<TinyInspectorProjectSettings>(TinyInspectorProjectSettings.k_AssetPath);
                if (settings != null && settings.ColorPalette != null)
                {
                    var e = settings.ColorPalette.FirstOrDefault(x => x.Name == color);
                    if (e != null) return e.BoxContentColor;
                }
                return ContainerColor;
            }
        }
    }
}

#endif
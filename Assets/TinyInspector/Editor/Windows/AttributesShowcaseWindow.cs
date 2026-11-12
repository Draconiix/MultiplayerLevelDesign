using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using TinyInspector;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GridBrushBase;

namespace TinyInspector.Editor
{
    // Attributes Showcase Window - UI Toolkit implementation with manual categories
    public class AttributesShowcaseWindow : EditorWindow
    {
        private ScrollView leftScroll;
        private ScrollView rightScroll;

        private Dictionary<string, List<AttributeInfo>> categories = new Dictionary<string, List<AttributeInfo>>();

        private string leftSearchQuery = string.Empty;
        private ToolbarSearchField leftSearchField;
        private VisualElement leftListContainer;


        #region Classes

        #region Group Attribute Examples

        [Serializable]
        internal class BoxGroupExample : ScriptableObject
        {
            [BoxGroup("Base Box")]
            public string Value1;

            [BoxGroup("Base Box")]
            public float Value2;

            [BoxGroup("Base Box/Boxed", TinyIcon.Storm)]
            public string BoxedValue1;

            [BoxGroup("Colored", TinyColor.Red)]
            public int ColoredBoxValue1;

            // Variables do not have to be in the same order as BoxGroup
            [BoxGroup("Base Box/Boxed")]
            public float BoxedValue2;
        }

        [Serializable]
        internal class FoldoutGroupExample : ScriptableObject
        {
            [FoldoutGroup("Base Foldout")]
            public string Value1;

            [FoldoutGroup("Foldout with Icon", TinyIcon.Lab)]
            public int IconValue1;

            [FoldoutGroup("Foldout with Icon/Colored", TinyColor.Purple)]
            public int ColoredValue1;
        }

        [Serializable]
        internal class TabGroupExample : ScriptableObject
        {
            [TabGroup("Tabs", "Normal")]
            public string NormalValue1;

            [TabGroup("Tabs", "Icon", TinyIcon.Save)]
            public string IconValue1;
            [TabGroup("Tabs", "Icon")]
            public string IconValue2;

            [TabGroup("Tabs", "Colored", TinyColor.Green)]
            public string ColorValue1;
        }

        [Serializable]
        internal class HorizontalGroupExample : ScriptableObject
        {
            [HorizontalGroup("Split")]

            [BoxGroup("Split/Left")]
            public string LeftValue1;

            [BoxGroup("Split/Right")]
            public int RightValue1;

            [BoxGroup("Split/Right")]
            public int RightValue2;
        }
        [Serializable]
        internal class VerticalGroupExample : ScriptableObject
        {
            [HorizontalGroup("Split")]

            [VerticalGroup("Split/Left")]
            [BoxGroup("Split/Left/Box 1")]
            public string Box1Value;

            [BoxGroup("Split/Left/Box 2")]
            public string Box2Value;

            [BoxGroup("Split/Right")]
            public int RightValue1;
        }

        #endregion

        #region Layout Attribute Examples

        [Serializable]
        internal class SpacerExample : ScriptableObject
        {
            [PropertySpace()]
            public string Spacer8 = "8 is the default value";

            [PropertySpace(16)]
            public string Spacer16;

            [PropertySpace(32)]
            public string Spacer32;
        }

        #endregion

        #region Decoration Attribute Examples

        [Serializable]
        internal class TitleExample : ScriptableObject
        {
            [Title("Example Title")]
            public string Title1;

            [Title("Example Title", "Example Description")]
            public string Title2;

            [Title("Example Title", "Example Description", TinyIcon.Bell)]
            public string Title3;

            [Title("Example Title", "Example Description", TinyIcon.AlignVertical, Separator: true)]
            public string Title4;

            [Title("Example Title", "Example Description", TinyIcon.AlignLeft, RowDirection: false)]
            public string Title5;
        }

        [Serializable]
        internal class SeparatorExample : ScriptableObject
        {
            [Separator]
            public string Separator;

            [Separator(8)]
            public string Thick;

            [Separator(12, 24)]
            public string BeforeAfter;

            [Separator(4, TinyColor.Lime)]
            public string Colored;
        }

        [Serializable]
        internal class PreviewFieldExample : ScriptableObject
        {
            [PreviewField]
            public Sprite SpritePreview;

            [PreviewField(5)]
            public Material MaterialPreview;

            [PreviewField(false)]
            public Texture TexturePreview;
        }

        [Serializable]
        internal class InfoBoxExample : ScriptableObject
        {
            [InfoBox("Title")]
            public string InfoBox1;

            [InfoBox("Title", "Long Message")]
            public string InfoBox2;

            [InfoBox("Title", InfoBoxType.None)]
            public string InfoBox3;

            [InfoBox("Title", "Long Message", InfoBoxType.Success)]
            public string InfoBox4;
        }

        [Serializable]
        internal class DisplayAsStringExample : ScriptableObject
        {
            [DisplayAsString]
            public string String1 = "Property Value";

            [DisplayAsString(true)]
            public string String2 = "Property Value";

            [DisplayAsString(TinyIcon.Mail)]
            public string String3 = "Property Value";

            [DisplayAsString(true, TinyIcon.Drop)]
            public string String4 = "Property Value";
        }

        [Serializable]
        internal class MultilineExample : ScriptableObject
        {
            [MultilineTextArea(3)]
            public string Multiline1;

            [MultilineTextArea(6, 300)]
            public string Multiline2;

            [MultilineTextArea(4, FullWidth: true)]
            public string Multiline3;

        }

        #endregion

        #region Collection Attribute Examples

        [Serializable]
        internal class ReorderableExample : ScriptableObject
        {
            [Reorderable]
            public List<string> StringList = new List<string> { "Item 1", "Item 2", "Item 3" };

            [Reorderable]
            public List<int> IntList = new List<int> { 10, 20, 30 };

            [Reorderable]
            public List<Vector3> Vector3List = new List<Vector3> { new Vector3(1, 2, 3), new Vector3(4, 5, 6), new Vector3(7, 8, 9) };

            [Reorderable]
            public List<ExampleCustomClass> CustomClasses = new List<ExampleCustomClass>
        {
            new ExampleCustomClass { Name = "First", Value = 1 },
            new ExampleCustomClass { Name = "Second", Value = 2 },
            new ExampleCustomClass { Name = "Third", Value = 3 }
        };

            [Reorderable, InlineDrawer]
            public List<ExampleCustomClass> CustomInlineClasses = new List<ExampleCustomClass>
        {
            new ExampleCustomClass { Name = "First", Value = 1 },
            new ExampleCustomClass { Name = "Second", Value = 2 },
            new ExampleCustomClass { Name = "Third", Value = 3 }
        };


            // Simple custom class for demonstration
            [Serializable]
            internal class ExampleCustomClass
            {
                public string Name;
                public int Value;
            }
        }

        [Serializable]
        internal class TableListExample : ScriptableObject
        {

            [TableList]
            public List<ExampleCustomClass> CustomInlineClasses = new List<ExampleCustomClass>
        {
            new ExampleCustomClass { Name = "First", Value = 1 },
            new ExampleCustomClass { Name = "Second", Value = 2 },
            new ExampleCustomClass { Name = "Third", Value = 3 }
        };


            // Simple custom class for demonstration
            [Serializable]
            internal class ExampleCustomClass
            {
                public string Name;
                public int Value;
            }
        }

        #endregion

        #region Selector Attribute Examples

        [Serializable]
        internal class EnumToggleExample : ScriptableObject
        {
            [EnumToggle]
            public SampleEnum EnumField;

            [EnumToggle]
            public SampleFlag FlagField;

            public enum SampleEnum
            {
                OptionA,
                OptionB,
                OptionC,
                OptionD,
                OptionE,
                OptionF
            }

            [System.Flags]
            public enum SampleFlag
            {
                FlagOptionA = 1,
                FlagOptionB = 2,
                FlagOptionC = 4,
                FlagOptionD = 8,
                FlagOptionE = 16,
                FlagOptionF = 32
            }
        }
        [Serializable]
        internal class SceneDropdownExample : ScriptableObject
        {
            [SceneDropdown]
            public string SceneName;

            [SceneDropdown]
            public int IntName;
        }

        #endregion

        #region Number Attribute Examples

        [Serializable]
        internal class MinMaxSliderExample : ScriptableObject
        {
            [MinMaxSlider(0, 100)]
            public Vector2Int Slider1 = new Vector2Int(25, 75);

            [MinMaxSlider(0, 100)]
            public Vector2 Slider2 = new Vector2(25, 75);
        }

        [Serializable]
        internal class ProgressBarExample : ScriptableObject
        {
            [ProgressBar(0, 100)]
            public int Progress1 = 75;

            [ProgressBar(0, 100, 32)]
            public int Progress2 = 75;

            [ProgressBar(0, 100, "Health", Color: TinyColor.Red, FullWidth = true)]
            public int Progress3 = 75;

            [ProgressBar(0, 100, ShowValue = false)]
            public int Progress4 = 75;

            [ProgressBar(0, 100, 32, ShowValueField = false)]
            public int Progress5 = 75;
        }

        [Serializable]
        internal class WrapExample : ScriptableObject
        {
            [Wrap(0, 100)]
            public int WrappedValue1 = 20;

            [Wrap(0, 100)]
            public float WrappedValue2 = 50;

            [Wrap(0, 100)]
            public double WrappedValue3 = 80;
        }
        #endregion

        [Serializable]
        internal class SuffixExample : ScriptableObject
        {
            [Suffix("Suffix")]
            public string Test;

            [Suffix("Suffix", IsOverlay: false)]
            public string TextSpacing;

            [Suffix(Icon: TinyIcon.Audio)]
            public string Icon;

            [Suffix(Icon: TinyIcon.Download, IsOverlay: false)]
            public string IconSpasing;

            [Suffix("Suffix", TinyIcon.Audio)]
            public string TextIcon;

            [Suffix("Suffix", TinyIcon.Download, false)]
            public string TextIconSpasing;
        }
        [Serializable]
        internal class HideLabelExample : ScriptableObject
        {
            [HideLabel]
            public string HidedLabel;
        }
        [Serializable]
        internal class CustomLabelExample : ScriptableObject
        {
            [CustomLabel("Custom Label")]
            public string Defualt;

            [CustomLabel("Custom Label with Icon", TinyIcon.Computer)]
            public string WithIcon;
        }


        [Serializable]
        internal class RequiredExample : ScriptableObject
        {
            [Required]
            public GameObject Error;

            [Required(false)]
            public GameObject Warning;
        }


        [Serializable]
        internal class DictionaryTableExample
        {

        }



        [Serializable]
        internal class ButtonExample : ScriptableObject
        {
            [Button("Base Button")]
            public void SomeFunction()
            {
                Debug.Log("Test");
            }
            [Button("Button (Custom Height)", 64)]
            public void SomeFunction1() 
            {
                Debug.Log("Test");
            }
            [Button("Button with Icon", Icon: TinyIcon.Add)]
            public void SomeFunction2()
            {
                Debug.Log("Test");
            }
            [Button("Colored Button", Color: TinyColor.Green)]
            public void SomeFunction3()
            {
                Debug.Log("Test");
            }

            [Button("Button with Attributes")]
            public void SomeFunctionWithAttributes(string val1, int val2, bool var3)
            {
                Debug.Log("Test");
            }
        }
        [Serializable]
        internal class SwitchExample : ScriptableObject
        {
            [Switch]
            public bool Defualt;

            [Switch(Expand: true)]
            public bool Expanded;

            [Switch("Custom OFF", "Custom ON")]
            public bool Labels;

            [Switch("Custom OFF", "Custom ON", Expand: true)]
            public bool LabelsAndExpanded;

            [Switch(TinyColor.Red)]
            public bool Color;

            [Switch(TinyColor.Green, Expand: true)]
            public bool ColorAndExpanded;
        }



        [Serializable]
        internal class IFConditionExample : ScriptableObject
        {
            public UnityEngine.Object SomeObject;
            [EnumToggle]
            public TestEnum SomeEnum;
            [Switch]
            public bool IsToggled;


            [ShowIf("IsToggled")]
            public string ShowWhenToggleOn;

            [HideIf("IsToggled")]
            public string HideWhenToggleOn;

            [ShowIf("SomeObject")]
            public string ShowWhenNotNull;

            [HideIf("SomeObject")]
            public string ShowWhenNull;

            [ShowIf("SomeEnum", TestEnum.FirstOption)]
            public string ShowOnlyWhenFirst;

            [HideIf("SomeEnum", TestEnum.FirstOption)]
            public string HideWhenFirst;

            public enum TestEnum
            {
                FirstOption,
                SecondOption,
                ThirdOption
            }
        }
        [Serializable]
        internal class PlaymodeConditionExample : ScriptableObject
        {
            [ShowInPlayMode] public int ShowInPlayMode;
            [HideInPlayMode] public int HideInPlayMode;
            [EnableInPlayMode] public int EnableInPlayMode;
            [DisableInPlayMode] public int DisableInPlayMode;
        }
        [Serializable]
        internal class EditmodeConditionExample : ScriptableObject
        {
            [ShowInEditMode] public int ShowInEditMode;
            [HideInEditMode] public int HideInEditMode;
            [EnableInEditMode] public int EnableInEditMode;
            [DisableInEditMode] public int DisableInEditMode;
        }
        [Serializable]
        internal class PrefabConditionExample : ScriptableObject
        {
            [ShowInPrefab] public int ShowInPrefab;
            [HideInPrefab] public int HideInPrefab;
            [EnableInPrefab] public int EnableInPrefab;
            [DisableInPrefab] public int DisableInPrefab;
        }

        [Serializable]
        internal class EnableIFDisableIFExample : ScriptableObject
        {
            [EnableIf("Condition")]
            public string EnableIFTrue;

            [DisableIf("Condition")]
            public string DisableIFTrue;

            [Switch]
            public bool Condition;
        }



        [Serializable, MonoscriptInfo("Script Description", "www.example.com")]
        internal class MonoscriptInfoExample : ScriptableObject
        {
            public string someValue;
        }

        [Serializable]
        internal class InlineDrawerExample : ScriptableObject
        {
            [InlineDrawer]
            public CustomClassInternal InlineClass;

            [Serializable]
            internal class CustomClassInternal
            {
                public string Value1;
                public int Value2;
            }
        }



        #region Code

        public static string BoxGroupCode = @"[BoxGroup(""Base Box"")]
public string Value1;

[BoxGroup(""Base Box"")]
public float Value2;

[BoxGroup(""Base Box/Boxed"", TinyIcon.Storm)]
public string BoxedValue1;

[BoxGroup(""Colored"", TinyColor.Red)]
public int ColoredBoxValue1;

// Variables do not have to be in the same order as BoxGroup
[BoxGroup(""Base Box/Boxed"")]
public float BoxedValue2;";

        public static string FoldoutGroupCode = @"[FoldoutGroup(""Base Foldout"")]
public string Value1;

[FoldoutGroup(""Foldout with Icon"", TinyIcon.Lab)]
public int IconValue1;

[FoldoutGroup(""Foldout with Icon/Colored"", TinyColor.Purple)]
public int ColoredValue1;";

        public static string TabGroupCode = @"[TabGroup(""Tabs"", ""Normal"")]
public string NormalValue1;

[TabGroup(""Tabs"", ""Icon"", TinyIcon.Save)]
public string IconValue1;

[TabGroup(""Tabs"", ""Icon"")]
public string IconValue2;

[TabGroup(""Tabs"", ""Colored"", TinyColor.Green)]
public string ColorValue1;";

        public static string HorizontalGroupCode = @"[HorizontalGroup(""Split"")]

[BoxGroup(""Split/Left"")]
public string LeftValue1;

[BoxGroup(""Split/Right"")]
public int RightValue1;

[BoxGroup(""Split/Right"")]
public int RightValue2;";

        public static string VerticalGroupCode = @"[HorizontalGroup(""Split"")]

[VerticalGroup(""Split/Left"")]
[BoxGroup(""Split/Left/Box 1"")]
public string Box1Value;

[BoxGroup(""Split/Left/Box 2"")]
public string Box2Value;

[BoxGroup(""Split/Right"")]
public int RightValue1;";



        public static string PropertySpaceCode = @"[Spacer()]
public string Spacer8 = ""8 is the default value"";

[Spacer(16)]
public string Spacer16;

[Spacer(32)]
public string Spacer32;";



        public static string TitleCode = @"[Title(""Example Title"")]
public string Title1;

[Title(""Example Title"", ""Example Description"")]
public string Title2;

[Title(""Example Title"", ""Example Description"", TinyIcon.Bell)]
public string Title3;

[Title(""Example Title"", ""Example Description"", TinyIcon.AlignVertical, Separator: true)]
public string Title4;

[Title(""Example Title"", ""Example Description"", TinyIcon.AlignLeft, RowDirection: false)]
public string Title5;";

        public static string SeparatorCode = @"[Separator]
public string Separator;

[Separator(8)]
public string Thick;

[Separator(12, 24)]
public string BeforeAfter;

[Separator(4, TinyColor.Lime)]
public string Colored;";

        public static string PreviewFieldCode = @"[PreviewField]
public Sprite SpritePreview;

[PreviewField(5)]
public Texture TexturePreview;

[PreviewField(false)]
public Material MaterialPreview;";

        public static string InfoBoxCode = @"[InfoBox(""Title"")]
public string InfoBox1;

[InfoBox(""Title"", ""Long Message"")]
public string InfoBox2;

[InfoBox(""Title"", InfoBoxType.None)]
public string InfoBox3;

[InfoBox(""Title"", ""Long Message"", InfoBoxType.Success)]
public string InfoBox4;";

        public static string DisplayAsStringCode = @"[DisplayAsString]
public string String1 = ""Property Value"";

[DisplayAsString(true)]
public string String2 = ""Property Value"";

[DisplayAsString(TinyIcon.Mail)]
public string String3 = ""Property Value"";

[DisplayAsString(true, TinyIcon.Ammo)]
public string String4 = ""Property Value"";";



        public static string ReorderableListCode = @"[Reorderable]
public List<string> StringList = new List<string> { ""Item 1"", ""Item 2"", ""Item 3"" };

[Reorderable]
public List<int> IntList = new List<int> { 10, 20, 30 };

[Reorderable]
public List<Vector3> Vector3List = new List<Vector3> 
{ 
    new Vector3(1, 2, 3), 
    new Vector3(4, 5, 6), 
    new Vector3(7, 8, 9) 
};

[Reorderable]
public List<ExampleCustomClass> CustomClasses = new List<ExampleCustomClass>
{
    new ExampleCustomClass { Name = ""First"", Value = 1 },
    new ExampleCustomClass { Name = ""Second"", Value = 2 },
    new ExampleCustomClass { Name = ""Third"", Value = 3 }
};

[Reorderable, InlineDrawer]
public List<ExampleCustomClass> CustomInlineClasses = new List<ExampleCustomClass>
{
    new ExampleCustomClass { Name = ""First"", Value = 1 },
    new ExampleCustomClass { Name = ""Second"", Value = 2 },
    new ExampleCustomClass { Name = ""Third"", Value = 3 }
};";

        public static string TableListCode = @"[TableList]
public List<ExampleCustomClass> CustomInlineClasses = new List<ExampleCustomClass>
{
    new ExampleCustomClass { Name = ""First"", Value = 1 },
    new ExampleCustomClass { Name = ""Second"", Value = 2 },
    new ExampleCustomClass { Name = ""Third"", Value = 3 }
};";



        public static string EnumToggleCode = @"[EnumToggle]
public SampleEnum EnumField;

[EnumToggle]
public SampleFlag FlagField;";

        public static string SceneDropdownCode = @"[SceneDropdown]
public string SceneName;

[SceneDropdown]
public int IntName;";



        public static string MinMaxSliderode = @"[MinMaxSlider(0, 100)]
public Vector2Int Slider1 = new Vector2Int(25, 75);

[MinMaxSlider(0, 100)]
public Vector2 Slider2 = new Vector2(25, 75);";

        public static string ProgressBarCode = @"[ProgressBar(0, 100)]
public int Progress1 = 75;

[ProgressBar(0, 100, 32)]
public int Progress2 = 75;

[ProgressBar(0, 100, ""Health"", Color: TinyColor.Red, FullWidth = true)]
public int Progress3 = 75;

[ProgressBar(0, 100, ShowValue = false)]
public int Progress4 = 75;

[ProgressBar(0, 100, 32, ShowValueField = false)]
public int Progress5 = 75;";

        public static string WrapCode = @"[Wrap(0, 100)]
public int WrappedValue1 = 20;

[Wrap(0, 100)]
public float WrappedValue2 = 50;

[Wrap(0, 100)]
public double WrappedValue3 = 80;";



        public static string SuffixCode = @"[Suffix(""Suffix"")]
public string Test;

[Suffix(""Suffix"", IsOverlay: false)]
public string TextSpacing;

[Suffix(Icon: TinyIcon.Audio)]
public string Icon;

[Suffix(Icon: TinyIcon.Download, IsOverlay: false)]
public string IconSpasing;

[Suffix(""Suffix"", TinyIcon.Audio)]
public string TextIcon;

[Suffix(""Suffix"", TinyIcon.Download, false)]
public string TextIconSpasing;";

        public static string HideLabelCode = @"[HideLabel]
public string HidedLabel;";

        public static string CustomLabelCode = @"[CustomLabel(""Custom Label"")]
public string Defualt;

[CustomLabel(""Custom Label with Icon"", TinyIcon.Computer)]
public string WithIcon;";

        public static string SwitchCode = @"[Switch]
public bool Defualt;

[Switch(Expand: true)]
public bool Expanded;

[Switch(""Custom OFF"", ""Custom ON"")]
public bool Labels;

[Switch(""Custom OFF"", ""Custom ON"", Expand: true)]
public bool LabelsAndExpanded;

[Switch(TinyColor.Red)]
public bool Color;

[Switch(TinyColor.Green, Expand: true)]
public bool ColorAndExpanded;";

        public static string MultilineTextAraCode = @"[MultilineTextArea(3)]
public string Multiline1;

[MultilineTextArea(6, 300)]
public string Multiline2;

[MultilineTextArea(4, fullWidth: true)]
public string Multiline3;";



        public static string ButtonCode = @"[Button(""Base Button"")]
public void SomeFunction()
{
    Debug.Log(""Test"");
}
[Button(""Button (Custom Height)"", 64)]
public void SomeFunction1() 
{
    Debug.Log(""Test"");
}
[Button(""Button with Icon"", Icon: TinyIcon.Add)]
public void SomeFunction2()
{
    Debug.Log(""Test"");
}
[Button(""Colored Button"", Color: TinyColor.Green)]
public void SomeFunction3()
{
    Debug.Log(""Test"");
}

[Button(""Button with Attributes"")]
public void SomeFunctionWithAttributes(string val1, int val2, bool var3)
{
    Debug.Log(""Test"");
}";



        public static string ConditionIFCode = @"// Demo Purpose Only
public UnityEngine.Object SomeObject;
[EnumToggle] public TestEnum SomeEnum;
[Switch] public bool IsToggled;


[ShowIf(""IsToggled"")]
public string ShowWhenToggleOn;

[HideIf(""IsToggled"")]
public string HideWhenToggleOn;

[ShowIf(""SomeObject"")]
public string ShowWhenNotNull;

[HideIf(""SomeObject"")]
public string ShowWhenNull;

[ShowIf(""SomeEnum"", TestEnum.FirstOption)]
public string ShowOnlyWhenFirst;

[HideIf(""SomeEnum"", TestEnum.FirstOption)]
public string HideWhenFirst;

// Demo Purpose Only
public enum TestEnum
{
    FirstOption,
    SecondOption,
    ThirdOption
}";

        public static string ConditionPlayModeCode = @"[ShowInPlayMode] 
public int ShowInPlayMode;

[HideInPlayMode] 
public int HideInPlayMode;

[EnableInPlayMode] 
public int EnableInPlayMode;

[DisableInPlayMode] 
public int DisableInPlayMode;";

        public static string ConditionEdtModeCode = @"[ShowInEditMode] 
public int ShowInEditMode;

[HideInEditMode] 
public int HideInEditMode;

[EnableInEditMode] 
public int EnableInEditMode;

[DisableInEditMode] 
public int DisableInEditMode;";

        public static string ConditionPrefabCode = @"[ShowInPrefab] 
public int ShowInPrefab;

[HideInPrefab] 
public int HideInPrefab;

[EnableInPrefab] 
public int EnableInPrefab;

[DisableInPrefab] 
public int DisableInPrefab;";



        public static string RequiredCode = @"[Required]
public GameObject Error;

[Required(false)]
public GameObject Warning;";



        public static string MonoscriptInfoCode = @"[Serializable, MonoscriptInfo(""Script Description"", ""www.example.com"")]
public class MonoscriptInfoExample : MonoBehaviour
{
    public string someValue;
}";

        public static string InlineDrawerCode = @"[InlineDrawer]
public CustomClassInternal InlineClass;";

        #endregion



        // Generic holder that uses SerializeReference so we can store arbitrary serializable object instances
        // 'inlineRender' controls whether MyCustomEditor should render the 'item' inline (default true).
        internal class GenericHolder : ScriptableObject { [SerializeReference] public object item; public bool inlineRender = true; }

        #endregion

        private ScriptableObject currentHolder = null;
        private UnityEditor.Editor currentHolderEditor = null;
        private ScriptableObject currentTempScriptable = null;
        private Dictionary<UnityEngine.Object, string> originalMonoScriptNames = new Dictionary<UnityEngine.Object, string>();

        private object activeExampleInstance = null;

        [MenuItem("Tools /Tiny Inspector/Attributes Showcase")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<AttributesShowcaseWindow>("Attributes Showcase");
            wnd.InitializeData();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Attribute Showcase", EditorGUIUtility.IconContent("d_UnityEditor.InspectorWindow").image);

            InitializeData();
            ConstructUI();
        }

        private void OnDisable()
        {
            if (currentHolder != null)
            {
                DestroyImmediate(currentHolder);
                currentHolder = null;
            }
            if (currentHolderEditor != null)
            {
                DestroyImmediate(currentHolderEditor);
                currentHolderEditor = null;
            }
            RestoreOriginalMonoScriptNames();
        }

        private void RestoreOriginalMonoScriptNames()
        {
            if (originalMonoScriptNames == null) return;
            foreach (var kv in originalMonoScriptNames)
            {
                try
                {
                    if (kv.Key != null)
                        kv.Key.name = kv.Value;
                }
                catch { }
            }
            originalMonoScriptNames.Clear();
        }

        private void InitializeData()
        {
            categories.Clear();

            categories["Group Attributes"] = new List<AttributeInfo>
            {
                new AttributeInfo("Box Group", "Places properties inside a boxed section in the Inspector, making related values visually grouped and easier to scan and understand.", 
                "group-attributes/box-group", typeof(BoxGroupExample),BoxGroupCode , "1.0.0a"),

                new AttributeInfo("Foldout Group", "Organizes properties into a collapsible foldout section, allowing users to hide or reveal values as needed for cleaner Inspectors.", 
                "group-attributes/foldout-group", typeof(FoldoutGroupExample),FoldoutGroupCode , "1.0.0a"),

                new AttributeInfo("Tab Group", "Splits properties into tabs within the same group, helping organize large sets of values into clean, logical sections.", 
                "group-attributes/tab-group", typeof(TabGroupExample),TabGroupCode , "1.0.0a"),

                new AttributeInfo("Horizontal Group", "Arranges multiple properties side by side in a single row, useful for compact layouts and closely related values.", "" +
                "group-attributes/horizontal-group", typeof(HorizontalGroupExample),HorizontalGroupCode , "1.0.0a"),

                new AttributeInfo("Vertical Group", "Collects properties into a vertical group, mainly used in combination with other group attributes to control layout structure.", 
                "group-attributes/vertical-group", typeof(VerticalGroupExample),VerticalGroupCode , "1.0.0a"),
            };

            categories["Layout Attributes"] = new List<AttributeInfo>
            {
                new AttributeInfo("Property Space", "Adds vertical spacing between Inspector fields, similar to Unity’s default space, but with explicit height control for more precise and consistent layout tuning.", 
                "layout-attributes/property-spacer", typeof(SpacerExample),PropertySpaceCode , "1.0.0a" ),

                //new AttributeInfo("Label Width", "Desc",
                //"layout-attributes/label-width", typeof(HorizontalVerticalGroupExample),BoxGroupCode , "1.0.0a" ),

                //new AttributeInfo("Indent", "Desc", "
                //layout-attributes/indent", typeof(HorizontalVerticalGroupExample),BoxGroupCode , "1.0.0a" ),
            };

            categories["Decoration Attributes"] = new List<AttributeInfo>
            {
                new AttributeInfo("Title", "Extends section headers with a title, description, and optional icon, allowing you to clearly separate and document Inspector sections with enhanced visual structure.", 
                "decoration-attributes/title", typeof(TitleExample),TitleCode , "1.0.0a" ),

                new AttributeInfo("Separtor", "Draws a visual separator line between fields, helping to group related properties and improve Inspector readability.", 
                "decoration-attributes/separator", typeof(SeparatorExample),SeparatorCode , "1.0.0a" ),

                //new AttributeInfo("Image Area", "Desc",
                //"decoration-attributes/image-area", typeof(HorizontalVerticalGroupExample),BoxGroupCode , "1.0.0a" ),

                new AttributeInfo("Preview Field", "Displays a live preview of the assigned asset directly in the Inspector, allowing quick inspection without opening separate windows.", 
                "decoration-attributes/preview-field", typeof(PreviewFieldExample),PreviewFieldCode , "1.0.0a", exclusiveTypes: new[] { "Object", "Sprite", "Texture", "Component", "Material" } ),

                //new AttributeInfo("GUI Color", "Desc",
                //"decoration-attributes/gui-color", typeof(HorizontalVerticalGroupExample),BoxGroupCode , "1.0.0a" ),

                //new AttributeInfo("Steps", "Desc",
                //"decoration-attributes/steps", typeof(HorizontalVerticalGroupExample),BoxGroupCode , "1.0.0a" ),

                new AttributeInfo("Info Box", "Creates a customizable help box with title, message, and type, offering more control and styling options than Unity’s default help boxes.", 
                "decoration-attributes/info-box", typeof(InfoBoxExample),InfoBoxCode , "1.0.0a" ),

                new AttributeInfo("Display As String", "Displays a property value as read-only text in the Inspector, without an editable field, useful for diagnostics and runtime information.", 
                "decoration-attributes/display-as-string", typeof(DisplayAsStringExample),DisplayAsStringCode , "1.0.0a", exclusiveTypes: new[] { "String" } ),
            };

            categories["Collection Attributes"] = new List<AttributeInfo>
            {
                new AttributeInfo("Reorderable List", "Renders lists and arrays using a clean, modern layout with drag-and-drop reordering, making collection editing faster and more readable than the default Inspector view.", 
                "/collection-attributes/reorderable-list", typeof(ReorderableExample),ReorderableListCode , "1.0.0a", exclusiveTypes: new[] {  "List", "Array" } ),

                new AttributeInfo("Table List", "Displays lists and arrays in a table-style layout, making structured data easier to scan, compare, and edit directly in the Inspector.", 
                "/collection-attributes/table-list", typeof(TableListExample),TableListCode , "1.0.0a", exclusiveTypes: new[] { "List", "Array" } ),

                //new AttributeInfo("Dictionary Table", "Desc",
                //"/collection-attributes/pageID", typeof(HorizontalVerticalGroupExample),BoxGroupCode , "1.0.0a" ),
            };

            categories["Selector Attributes"] = new List<AttributeInfo>
            {
                //new AttributeInfo("Value Dropdown", "Desc",
                //"selecter-attributes/value-dropdown", typeof(HorizontalVerticalGroupExample),BoxGroupCode , "1.0.0a" ),

                new AttributeInfo("Enum Toggle", "Renders enum values as a toggle grid instead of a dropdown, enabling faster selection and better visibility of all available options at once.", 
                "selecter-attributes/enum-toggle", typeof(EnumToggleExample),EnumToggleCode , "1.0.0a", exclusiveTypes: new[] { "Enums" } ),

                new AttributeInfo("Scene Dropdown", "Displays a dropdown list containing all scenes included in the Build Settings, allowing you to select a scene asset safely without relying on string names or manual typing.", 
                "selecter-attributes/scene-dropdown", typeof(SceneDropdownExample),SceneDropdownCode , "1.0.0a", exclusiveTypes: new[] { "String", "Int" } ),

                //new AttributeInfo("Tag Dropdown", "Desc",
                //"selecter-attributes/tag-dropdown", typeof(HorizontalVerticalGroupExample),BoxGroupCode , "1.0.0a" ),

                //new AttributeInfo("Sorting Layer Dropdown", "Desc",
                //"selecter-attributes/pageID", typeof(HorizontalVerticalGroupExample),BoxGroupCode , "1.0.0a" ),

                //new AttributeInfo("Animator Parameter", "Desc",
                //"selecter-attributes/pageID", typeof(HorizontalVerticalGroupExample),BoxGroupCode , "1.0.0a" ),

                //new AttributeInfo("Material Parameter", "Desc",
                //"selecter-attributes/pageID", typeof(HorizontalVerticalGroupExample),BoxGroupCode , "1.0.0a" ),

                //new AttributeInfo("Path Picker", "Desc",
                //"selecter-attributes/pageID", typeof(HorizontalVerticalGroupExample),BoxGroupCode , "1.0.0a" ),

                //new AttributeInfo("Resizable Text Area",
                //"Desc", "selecter-attributes/pageID", typeof(HorizontalVerticalGroupExample),BoxGroupCode , "1.0.0a" ),
            };

            categories["Number Attributes"] = new List<AttributeInfo>
            {
                new AttributeInfo("Min Max Slider", "Limits a value using a single dual-handle slider, making it ideal for randomness ranges, clamps, and min–max constraints.", 
                "number-attributes/min-max-slider", typeof(MinMaxSliderExample), MinMaxSliderode, "1.0.0a", exclusiveTypes: new[] { "Vector2", "Vector2Int" } ),

                new AttributeInfo("Progress Bar", "Replaces the default field with a progress bar, providing a clean and visual way to represent numeric values.",
                "number-attributes/progress-bar", typeof(ProgressBarExample), ProgressBarCode, "1.0.0a", exclusiveTypes: new[] { "Numeric" } ),

                new AttributeInfo("Wrap", "Constrains a numeric field to a defined range and wraps the value to the opposite limit when exceeded.", 
                "number-attributes/wrap", typeof(WrapExample), WrapCode, "1.0.0a", exclusiveTypes: new[] { "Numeric" } ),

                //new AttributeInfo("Formatted Number", "Desc",
                //"number-attributes/formatted-number", typeof(HorizontalVerticalGroupExample), BoxGroupCode, "1.0.0a" ),
            };

            categories["Misc Attributes"] = new List<AttributeInfo>
            {
                new AttributeInfo("Suffix", "Adds extra contextual information to a field by displaying a text or icon suffix next to it, optionally overlaid on the field itself.", 
                "misc-attributes/suffix", typeof(SuffixExample),SuffixCode , "1.0.0a" ),

                new AttributeInfo("Hide Label", "Hides the field label in the Inspector, allowing for cleaner layouts or custom-aligned UI elements.", 
                "misc-attributes/hide-label", typeof(HideLabelExample),HideLabelCode , "1.0.0a" ),

                new AttributeInfo("Custom Label", "Overrides the default field label and optionally adds an icon for clearer or more descriptive Inspector layouts.", 
                "misc-attributes/custom-label", typeof(CustomLabelExample),CustomLabelCode , "1.0.0a" ),

                new AttributeInfo("Switch", "Replaces the default boolean field with a styled switch control that supports custom on/off labels and color styling.", 
                "misc-attributes/switch", typeof(SwitchExample),SwitchCode , "1.0.0a", exclusiveTypes: new[] { "Bool" } ),

                new AttributeInfo("Multiline Text Area", "Creates a multiline text area with configurable height and optional character limit for flexible inspector input.", 
                "misc-attributes/multiline-text-area", typeof(MultilineExample),MultilineTextAraCode , "1.0.0a", exclusiveTypes: new[] { "String" } ),
            };

            categories["Action Attributes"] = new List<AttributeInfo>
            {
                new AttributeInfo("Button", "Displays a clickable button in the Inspector that allows you to execute a method directly without writing custom editor code.", 
                "action-attributes/pageID", typeof(ButtonExample),ButtonCode , "1.0.0a", exclusiveTypes: new[] { "Void (Function)" } ),

                //new AttributeInfo("Reset Button", "Desc",
                //"action-attributes/reset-button", typeof(HorizontalVerticalGroupExample),BoxGroupCode , "1.0.0a" ),
            };

            categories["Condition Attributes"] = new List<AttributeInfo>
            {
                new AttributeInfo("Show / Hide IF", "Conditionally shows or hides a field in the Inspector based on the value of another property, allowing for dynamic and context-aware layouts.", 
                "condition-attributes/show-hide-if", typeof(IFConditionExample),ConditionIFCode , "1.0.0a" ),

                new AttributeInfo("Enable / Disable IF", "Conditionally enables or disables a field depending on another property's value, keeping it visible while controlling whether it can be edited.", 
                "condition-attributes/enable-disable-if", typeof(IFConditionExample),ConditionIFCode , "1.0.0a" ),
                
                new AttributeInfo("Show / Hide in Play Mode", "Displays or hides a field automatically when the application enters Play Mode, helping separate runtime-only data from edit-time configuration.", 
                "condition-attributes/show-hide-in-play-mode", typeof(PlaymodeConditionExample),ConditionPlayModeCode , "1.0.0a" ),
                
                new AttributeInfo("Enable / Disable in Play Mode", "Enables or disables a field during Play Mode, preventing unintended runtime changes while keeping the value visible in the Inspector.", 
                "condition-attributes/enable-disable-in-play-mode", typeof(PlaymodeConditionExample),ConditionPlayModeCode , "1.0.0a" ),
                
                new AttributeInfo("Show / Hide in Edit Mode", "Controls field visibility while in Edit Mode, allowing you to simplify the Inspector by hiding runtime-specific properties.", 
                "condition-attributes/show-hide-in-play-mode", typeof(EditmodeConditionExample),ConditionEdtModeCode , "1.0.0a" ),
                
                new AttributeInfo("Enable / Disable in Edit Mode", "Disables or enables a field in Edit Mode, useful for locking values that should only be modified during runtime.", 
                "condition-attributes/enable-disable-in-play-mode", typeof(EditmodeConditionExample),ConditionEdtModeCode , "1.0.0a" ),
                
                new AttributeInfo("Show / Hide in Prefab", "Shows or hides a field when viewing a Prefab asset, keeping prefab configuration clean and focused.", 
                "condition-attributes/show-hide-in-prefab", typeof(PrefabConditionExample),ConditionPrefabCode , "1.0.0a" ),
                
                new AttributeInfo("Enable / Disable in Prefab", "Enables or disables a field inside Prefab view, restricting modifications to values that should not be changed at the prefab level.", 
                "condition-attributes/enable-disable-in-prefab", typeof(PrefabConditionExample),ConditionPrefabCode , "1.0.0a" ),
            };

            categories["Validation Attributes"] = new List<AttributeInfo>
            {
                new AttributeInfo("Required", "Displays a validation message in the Inspector when the field value is null or empty, making missing references or data immediately visible.", 
                "validation-attributes/required", typeof(RequiredExample),RequiredCode , "1.0.0a", exclusiveTypes: new[] { "ObjectReference", "Array", "List", "String", "Integer", "Float", "Bool" }  ),

                //new AttributeInfo("Scene Only", "Desc",
                //"validation-attributes/scene-only", typeof(HorizontalVerticalGroupExample),BoxGroupCode , "1.0.0a" ),

                //new AttributeInfo("Asset Only", "Desc",
                //"validation-attributes/asset-only", typeof(HorizontalVerticalGroupExample),BoxGroupCode , "1.0.0a" ),

                //new AttributeInfo("Child Only", "Desc",
                //"validation-attributes/child-only", typeof(HorizontalVerticalGroupExample),BoxGroupCode , "1.0.0a" ),

                //new AttributeInfo("Read Only", "Desc",
                //"validation-attributes/read-only", typeof(HorizontalVerticalGroupExample),BoxGroupCode , "1.0.0a" ),
            };

            categories["Script Attributes"] = new List<AttributeInfo>
            {
                new AttributeInfo("Monoscript Info", "Adds a description and documentation link to the script header in the Inspector, right next to the MonoBehaviour or ScriptableObject reference.", 
                "script-attributes/monoscript-info", typeof(MonoscriptInfoExample),MonoscriptInfoCode , "1.0.0a", true, false, exclusiveTypes: new[] { "MonoBehaviour", "ScriptableObject" }  ),

                new AttributeInfo("Inline Drawer", "Renders custom class fields inline without a foldout. No custom drawer required. Keeps inspectors flat, readable, and fast to work with.", 
                "script-attributes/inline-drawer", typeof(InlineDrawerExample),InlineDrawerCode , "1.0.0a", exclusiveTypes: new[] { "Classes" }  ),

                //new AttributeInfo("Inline Editor", "Desc",
                //"cript-attributes/inline-editor", typeof(HorizontalVerticalGroupExample),BoxGroupCode , "1.0.0a" ),

                //new AttributeInfo("Custom Context Menu", "Desc",
                //"cript-attributes/custom-context-menu", typeof(HorizontalVerticalGroupExample),BoxGroupCode , "1.0.0a" ),
            };
        }

        private void PopulateLeftPane()
        {
            // ensure the leftListContainer exists
            if (leftListContainer == null)
                return;

            leftListContainer.Clear();

            // Build simple category headers (Label) and list of buttons inside leftListContainer
            foreach (var cat in categories)
            {
                // category label
                var catLabel = new Label(cat.Key);
                catLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                catLabel.style.marginLeft = 6;
                catLabel.style.marginTop = 8;
                catLabel.style.marginBottom = 0;
                leftListContainer.Add(catLabel);

                for (int i = 0; i < cat.Value.Count; i++)
                {
                    var info = cat.Value[i];
                
                    if (!string.IsNullOrEmpty(leftSearchQuery))
                    {
                        var q = leftSearchQuery.Trim();
                        if ((info.Name ?? string.Empty).IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0 && (info.Description ?? string.Empty).IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0 /*&& (code ?? string.Empty).IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0*/)
                            continue;
                    }

                    var localInfo = info;
                    var btn = new Button(() => OnAttributeSelected(localInfo)) { text = info.Name };
                    btn.style.unityTextAlign = TextAnchor.MiddleLeft;
                    btn.style.marginLeft = 6;
                    //btn.style.marginTop = 2;
                    btn.style.marginBottom = 0;
                    btn.style.height = 32;
                    btn.style.paddingLeft = 8;
                    btn.style.paddingRight = 8;

                    if(cat.Value.Count > 1)
                    {
                        if (i == 0) { btn.AddToClassList("ButtonShowcaseTop"); }
                        else if (i == cat.Value.Count - 1) { btn.AddToClassList("ButtonShowcaseBottom"); }
                        else { btn.AddToClassList("ButtonShowcaseMiddle"); }
                    }

                    leftListContainer.Add(btn);
                }
            }
        }

        private void ConstructUI()
        {
            rootVisualElement.Clear();

            StyleSheet sheet = Resources.Load<StyleSheet>("TinyInspector/Editor/TinyInspector");
            rootVisualElement.styleSheets.Add(sheet);



            // Add toolbar at the top
            var toolbar = new Toolbar();
            toolbar.AddToClassList("TinyToolbar");
            
            rootVisualElement.Add(toolbar);


            var start = new ToolbarSpacer();
            start.style.width = 6;
            toolbar.Add(start);


            // Search field (persistent so it does not lose focus)
            leftSearchField = new ToolbarSearchField();
            leftSearchField.AddToClassList("TinyToolbarField");
            leftSearchField.value = leftSearchQuery;
            leftSearchField.RegisterValueChangedCallback(evt =>
            {
                leftSearchQuery = evt.newValue ?? string.Empty;
                PopulateLeftPane();
            });
            leftSearchField.RegisterCallback<FocusOutEvent>(_ => { /* leave as-is */ });
            toolbar.Add(leftSearchField);

            // Spacer
            var spacer = new ToolbarSpacer();
            spacer.style.flexGrow = 1;
            toolbar.Add(spacer);


            // Add buttons to toolbar
            var refreshBtn = new ToolbarButton(() => { InitializeData(); PopulateLeftPane(); }) { text = "Refresh" };
            refreshBtn.style.width = 80;
            refreshBtn.AddToClassList("TinyToolbarButton");
            refreshBtn.style.borderRightWidth = 0;
            toolbar.Add(refreshBtn);

            var clearSearchBtn = new ToolbarButton(() => { leftSearchQuery = string.Empty; leftSearchField.value = string.Empty; PopulateLeftPane(); }) { text = "Clear Search" };
            clearSearchBtn.style.width = 100;
            clearSearchBtn.AddToClassList("TinyToolbarButton");
            toolbar.Add(clearSearchBtn);


            var styleBox = new VisualElement();
            styleBox.style.flexDirection = FlexDirection.Row;
            styleBox.style.alignItems = Align.Stretch;
            styleBox.style.flexGrow = 1;

            // Left pane
            leftScroll = new ScrollView(ScrollViewMode.Vertical);
            leftScroll.style.width = 280;
            leftScroll.style.minWidth = 200;
            leftScroll.style.borderRightWidth = 1;
            leftScroll.style.borderRightColor = new StyleColor(new Color(0, 0, 0, 0.08f));
            // darker background
            leftScroll.style.backgroundColor = new StyleColor(new Color(0.08f, 0.08f, 0.08f, .2f));

            // persistent header
            //var leftHeader = new Label("Attributes") { transform = { } };
            //leftHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            //leftHeader.style.marginTop = 6;
            //leftHeader.style.marginLeft = 6;
            //leftHeader.style.marginBottom = 0;
            //leftScroll.contentContainer.Add(leftHeader);

            leftScroll.style.marginBottom = 6;


            // container for list items (we will rebuild only this)
            leftListContainer = new VisualElement();
            leftScroll.contentContainer.Add(leftListContainer);

            // Populate the list container
            PopulateLeftPane();

            // Right pane
            rightScroll = new ScrollView(ScrollViewMode.Vertical);
            rightScroll.style.flexGrow = 1;
            rightScroll.style.paddingLeft = 8;
            rightScroll.style.paddingTop = 6;

            styleBox.Add(leftScroll);
            styleBox.Add(rightScroll);

            rootVisualElement.Add(styleBox);

            // Initial selection
            if (categories.Count > 0)
            {
                var firstCat = categories.Values.FirstOrDefault();
                var firstInfo = firstCat?.FirstOrDefault();
                if (firstInfo != null) OnAttributeSelected(firstInfo);
            }
        }

        private void OnAttributeSelected(AttributeInfo info)
        {
            //string code = info.ExampleType != null
    //? BuildExampleClassCode(info.ExampleType, activeExampleInstance)
    //: string.Empty;



            // Clean up any previous holder
            if (currentHolder != null)
            {
                DestroyImmediate(currentHolder);
                currentHolder = null;
            }
            if (currentHolderEditor != null)
            {
                DestroyImmediate(currentHolderEditor);
                currentHolderEditor = null;
            }
            // restore any modified MonoScript names
            RestoreOriginalMonoScriptNames();
            if (currentTempScriptable != null)
            {
                DestroyImmediate(currentTempScriptable);
                currentTempScriptable = null;
            }

            rightScroll.Clear();

            // Header row: title on the left, version + doc button on the right
            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.alignItems = Align.Center;
            headerRow.style.justifyContent = Justify.FlexStart;

            var title = new Label(info.Name) { style = { unityFontStyleAndWeight = FontStyle.Bold } };
            title.style.fontSize = 30;
            //.title.style.marginBottom = 6;
            title.style.flexGrow = 1;
            title.style.opacity = 0.8f;
            headerRow.Add(title);

            var rightHeader = new VisualElement();
            rightHeader.style.flexDirection = FlexDirection.Column;
            rightHeader.style.alignItems = Align.Center;
            rightHeader.style.alignItems = Align.Stretch;
            rightHeader.style.justifyContent = Justify.FlexEnd;

            // Version label (displayed to the right of the title)
            if (!string.IsNullOrEmpty(info.Version))
            {
                var ver = new Label("Updated: " + info.Version) { style = { unityFontStyleAndWeight = FontStyle.Bold } };
                ver.style.unityTextAlign = TextAnchor.MiddleRight;
                ver.style.fontSize = 10;
                ver.style.opacity = 0.6f;
                ver.style.flexGrow = 1;
                ver.style.marginRight = 3;
                //rightHeader.Add(ver);
            }

            // Documentation button (to the far right)
            if (!string.IsNullOrEmpty(info.LinkSuffix))
            {
                var docBtn = new Button(() => Application.OpenURL("https://tiny-slime-studio.gitbook.io/tiny-inspector/attributes/" + info.LinkSuffix)) { text = "Online Documentation" };
                docBtn.style.marginBottom = 0;
                docBtn.style.height = 28;
                docBtn.style.paddingLeft = 12;
                docBtn.style.paddingRight = 12;
                docBtn.style.fontSize = 10;


                rightHeader.Add(docBtn);
            }

            headerRow.Add(rightHeader);
            rightScroll.Add(headerRow);

            // Description (below exclusive types)
            var desc = new Label(info.Description);
            desc.style.whiteSpace = WhiteSpace.Normal;
            desc.style.marginBottom = 2;
            desc.style.fontSize = 12;
            desc.style.marginRight = 8;
            desc.style.marginBottom = 24;
            desc.style.opacity = 0.8f;
            rightScroll.Add(desc);

            // Exclusive types (below header)
            if (info.ExclusiveTypes != null && info.ExclusiveTypes.Length > 0)
            {
                var typesList = string.Join(", ", info.ExclusiveTypes);
                var exclusiveContainer = new VisualElement();
                exclusiveContainer.style.flexDirection = FlexDirection.Row;
                exclusiveContainer.style.alignItems = Align.Center;
                exclusiveContainer.style.marginTop = 2;
                exclusiveContainer.style.marginBottom = 8;
                exclusiveContainer.style.flexGrow = 1;

                //var prefixLabel = new Label("Only applicable to: ") { style = { unityFontStyleAndWeight = FontStyle.Normal } };
                //prefixLabel.style.marginRight = 2;

                //var typesLabel = new Label(typesList) { style = { unityFontStyleAndWeight = FontStyle.Bold } };

                var InfoBox = InfoBoxElement.Create("This Attribute will not work with all types!", "This attribute can be used only with: <b>" + typesList, InfoBoxType.Warning);
                InfoBox.style.flexGrow = 1;
                InfoBox.style.marginRight = 8;

                exclusiveContainer.Add(InfoBox);
                //exclusiveContainer.Add(typesLabel);
                rightScroll.Add(exclusiveContainer);
            }

            var exampleLabel = new Label("Example:");
            exampleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            //rightScroll.Add(exampleLabel);

            // create a simple instance to get default field values
            if (info.ExampleType != null)
                activeExampleInstance = ScriptableObject.CreateInstance(info.ExampleType);
            else
                activeExampleInstance = null;

            var exampleTitle = new Label("Interactive Preview:") { style = { unityFontStyleAndWeight = FontStyle.Bold } };
            exampleTitle.style.borderRightWidth = 1;
            exampleTitle.style.borderRightColor = new StyleColor(new Color(25f/255,25f/255,25f/255,0.6f));
            exampleTitle.style.borderLeftWidth = 1;
            exampleTitle.style.borderLeftColor = new StyleColor(new Color(25f/255,25f/255,25f/255,0.6f));
            exampleTitle.style.borderTopWidth = 1;
            exampleTitle.style.borderTopColor = new StyleColor(new Color(25f/255,25f/255,25f/255,0.6f));

            exampleTitle.style.backgroundColor = new StyleColor(new Color(25f/255,25f/255,25f/255,0.2f));
            exampleTitle.style.borderTopLeftRadius = 3;
            exampleTitle.style.borderTopRightRadius = 3;

            exampleTitle.style.paddingLeft = 8;
            exampleTitle.style.paddingRight = 8;
            exampleTitle.style.paddingTop = 8;
            exampleTitle.style.paddingBottom = 8;
            exampleTitle.style.marginTop = 0;
            exampleTitle.style.marginLeft = 3;
            exampleTitle.style.marginRight = 8;
            rightScroll.Add(exampleTitle);


            // Container with max width and centered margins
            var exampleContainer = new VisualElement();
            exampleContainer.name = "PreviewContainer";
            // Make the container fill available width and never exceed it
            exampleContainer.style.width = new StyleLength(StyleKeyword.Auto);
            exampleContainer.style.maxWidth = new StyleLength(StyleKeyword.Auto);

            exampleContainer.style.paddingLeft = 8;
            exampleContainer.style.paddingRight = 8;
            exampleContainer.style.paddingTop = 8;
            exampleContainer.style.paddingBottom = 8;
            exampleContainer.style.marginTop = 0;
            exampleContainer.style.marginBottom = 16;
            exampleContainer.style.marginLeft = 3;
            exampleContainer.style.marginRight = 8;

            exampleContainer.style.borderRightWidth = 1;
            exampleContainer.style.borderRightColor = new StyleColor(new Color(25f/255,25f/255,25f/255,0.6f));
            exampleContainer.style.borderLeftWidth = 1;
            exampleContainer.style.borderLeftColor = new StyleColor(new Color(25f/255,25f/255,25f/255,0.6f));
            exampleContainer.style.borderBottomWidth = 1;
            exampleContainer.style.borderBottomColor = new StyleColor(new Color(25f/255,25f/255,25f/255,0.6f));
            exampleContainer.style.borderTopWidth = 1;
            exampleContainer.style.borderTopColor = new StyleColor(new Color(25f/255,25f/255,25f/255,0.6f));

            exampleContainer.style.borderBottomLeftRadius = 3;
            exampleContainer.style.borderBottomRightRadius = 3;


            // Conditional Example Object display based on CanUseInEditor flag
            if (info.ExampleType != null && info.CanUseInEditor)
            {
                var exTitle = new Label("Example object:") { style = { unityFontStyleAndWeight = FontStyle.Bold } };
                exTitle.style.marginTop = 8;
                //exampleContainer.Add(exTitle);
                // If ExampleType is a ScriptableObject, create a temporary instance (does not touch the Hierarchy)
                if (typeof(ScriptableObject).IsAssignableFrom(info.ExampleType))
                {
                    var scr = ScriptableObject.CreateInstance(info.ExampleType) as ScriptableObject;
                    currentTempScriptable = scr;
                    // try to find MonoScript asset for this scriptable type and adjust its name
                    try
                    {
                        var ms = MonoScript.FromScriptableObject(scr);
                        if (ms != null)
                        {
                            if (!originalMonoScriptNames.ContainsKey(ms))
                                originalMonoScriptNames[ms] = ms.name;
                            ms.name = info.ExampleType.Name;
                        }
                    }
                    catch { }
                    try { currentHolderEditor = UnityEditor.Editor.CreateEditor(scr); } catch { currentHolderEditor = null; }

                    if (currentHolderEditor != null)
                    {
                        VisualElement editorVE = null;
                        try
                        {
                            var mi = currentHolderEditor.GetType().GetMethod("CreateInspectorGUI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            if (mi != null) editorVE = mi.Invoke(currentHolderEditor, null) as VisualElement;
                        }
                        catch { editorVE = null; }

                        // DOCS
                        //editorVE.style.flexGrow = 1;
                        //editorVE.style.justifyContent = Justify.Center;

                        if (editorVE != null)
                        {
                            if (info.ForceShowingMonoscript)
                            {
                                try
                                {
                                    var monos = editorVE.Query<VisualElement>(className: "Monoscript").ToList();
                                    foreach (var m in monos) m.style.display = DisplayStyle.None;
                                }
                                catch { }
                            }
                            exampleContainer.Add(editorVE);
                        }
                        else
                        {
                            var wrap = new VisualElement();
                            //wrap.style.flexGrow = 1;
                            //wrap.style.justifyContent = Justify.Center;
                            var im = new IMGUIContainer(() => { if (currentHolderEditor == null) return; currentHolderEditor.OnInspectorGUI(); });
                            wrap.Add(im);
                            if (info.ForceShowingMonoscript)
                            {
                                try
                                {
                                    var monos = wrap.Query<VisualElement>(className: "Monoscript").ToList();
                                    foreach (var m in monos) m.style.display = DisplayStyle.None;
                                }
                                catch { }
                            }
                            exampleContainer.Add(wrap);
                        }
                    }
                    else
                    {
                        var warningLabel = new Label("No custom editor available for this ScriptableObject example.") { style = { unityFontStyleAndWeight = FontStyle.Bold } };
                        exampleContainer.Add(warningLabel);
                    }
                }
                else
                {
                    // Use generic holder with SerializeReference to hold arbitrary instance
                    var holder = ScriptableObject.CreateInstance<GenericHolder>();
                    holder.item = activeExampleInstance;
                    // for inline UI, keep inlineRender true (default)
                    currentHolder = holder;
                    // Destroy any previous editor instance
                    if (currentHolderEditor != null)
                    {
                        DestroyImmediate(currentHolderEditor);
                        currentHolderEditor = null;
                    }

                    // Try creating a custom editor for the holder so property drawers (ShowIf/EnableIf) evaluate live
                    try
                    {
                        currentHolderEditor = UnityEditor.Editor.CreateEditor(holder);
                    }
                    catch
                    {
                        currentHolderEditor = null;
                    }

                    if (currentHolderEditor != null)
                    {
                        // If the editor supports UI Toolkit, use its CreateInspectorGUI. Otherwise, embed IMGUI using OnInspectorGUI.
                        VisualElement editorVE = null;
                        try
                        {
                            var mi = currentHolderEditor.GetType().GetMethod("CreateInspectorGUI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            if (mi != null)
                            {
                                editorVE = mi.Invoke(currentHolderEditor, null) as VisualElement;
                            }
                        }
                        catch { editorVE = null; }

                        if (editorVE != null)
                        {
                            // Bind the editor's visual element to the holder's serialized object if possible
                            try { editorVE.Bind(new SerializedObject(holder)); } catch { }
                            if (info.ForceShowingMonoscript)
                            {
                                try
                                {
                                    var monos = editorVE.Query<VisualElement>(className: "Monoscript").ToList();
                                    foreach (var m in monos) m.style.display = DisplayStyle.None;
                                }
                                catch { }
                            }
                            exampleContainer.Add(editorVE);
                        }
                        else
                        {
                            // Fallback to IMGUI container that calls the editor's OnInspectorGUI
                            var wrap = new VisualElement();
                            var imgi = new IMGUIContainer(() =>
                            {
                                if (currentHolderEditor == null) return;
                                try
                                {
                                    currentHolderEditor.serializedObject.Update();
                                    currentHolderEditor.OnInspectorGUI();
                                    currentHolderEditor.serializedObject.ApplyModifiedProperties();
                                }
                                catch { }
                            });
                            wrap.Add(imgi);
                            if (info.ForceShowingMonoscript)
                            {
                                try
                                {
                                    var monos = wrap.Query<VisualElement>(className: "Monoscript").ToList();
                                    foreach (var m in monos) m.style.display = DisplayStyle.None;
                                }
                                catch { }
                            }
                            exampleContainer.Add(wrap);
                        }
                    }
                }
                
            }
            else if (info.ExampleType != null && !info.CanUseInEditor)
            {
                // Show message instead of example object
                var warningLabel = new Label("This attribute is not currently supported in the Custom Editor window. Please refer to the documentation for more information.") { style = { unityFontStyleAndWeight = FontStyle.Bold } };
                //warningLabel.style.marginTop = 8;
                warningLabel.style.color = new Color(1, 0.5f, 0);
                exampleContainer.Add(warningLabel);
            }


            rightScroll.Add(exampleContainer);


            // Header row for the code area with Copy button on the right
            var codeHeaderRow = new VisualElement();
            codeHeaderRow.style.flexDirection = FlexDirection.Row;
            codeHeaderRow.style.alignItems = Align.Center;
            codeHeaderRow.style.justifyContent = Justify.SpaceBetween;
            codeHeaderRow.style.alignSelf = Align.Stretch;
            codeHeaderRow.style.paddingLeft = 8;
            codeHeaderRow.style.paddingRight = 8;
            codeHeaderRow.style.paddingTop = 8;
            codeHeaderRow.style.paddingBottom = 8;
            codeHeaderRow.style.marginTop = 4;
            codeHeaderRow.style.marginLeft = 3;
            codeHeaderRow.style.marginRight = 8;
            codeHeaderRow.style.borderTopWidth = 1;
            codeHeaderRow.style.borderTopColor = new StyleColor(new Color(25f/255,25f/255,25f/255,0.6f));
            codeHeaderRow.style.borderLeftWidth = 1;
            codeHeaderRow.style.borderLeftColor = new StyleColor(new Color(25f/255,25f/255,25f/255,0.6f));
            codeHeaderRow.style.borderRightWidth = 1;
            codeHeaderRow.style.borderRightColor = new StyleColor(new Color(25f/255,25f/255,25f/255,0.6f));

            codeHeaderRow.style.backgroundColor = new StyleColor(new Color(25f / 255, 25f / 255, 25f / 255, 0.2f));
            codeHeaderRow.style.borderTopLeftRadius = 3;
            codeHeaderRow.style.borderTopRightRadius = 3;

            var codeHeaderLabel = new Label("Source Code:") { style = { unityFontStyleAndWeight = FontStyle.Bold } };
            codeHeaderLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            codeHeaderRow.Add(codeHeaderLabel);

            var copyBtn = new Button(() =>
            {
                GUIUtility.systemCopyBuffer = info.Code ?? string.Empty;
                // show brief notification
                this.ShowNotification(new GUIContent("Code copied to clipboard"));
            })
            { text = "Copy Code" };
            copyBtn.style.marginLeft = 6;
            copyBtn.style.marginRight = 6;
            copyBtn.style.height = 20;
            copyBtn.style.paddingLeft = 8;
            copyBtn.style.paddingRight = 8;
            codeHeaderRow.Add(copyBtn);

            rightScroll.Add(codeHeaderRow);

            // Create highlighted code element instead of plain TextField
            var highlightedCode = CreateHighlightedCodeElement(info.Code ?? string.Empty);
            rightScroll.Add(highlightedCode);
        }

        private void ValidateAttributorField(PropertyField field, SerializedProperty property)
        {
            // Special handling to hide attribute fields for primitive types
            if (property.propertyType == SerializedPropertyType.Integer ||
                property.propertyType == SerializedPropertyType.Boolean ||
                property.propertyType == SerializedPropertyType.Float)
            {
                // Hide the whole field
                field.style.display = DisplayStyle.None;
            }
            else
            {
                field.BindProperty(property);
            }
        }

        private string BuildExampleClassCode(Type t, object instance)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"public class {t.Name}");
            sb.AppendLine("{");

            var fields = t.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var f in fields)
            {
                // prefer to read attribute constructor args as they appear in source using CustomAttributeData
                var cadList = CustomAttributeData.GetCustomAttributes(f);
                if (cadList != null && cadList.Count > 0)
                {
                    foreach (var cad in cadList)
                    {
                        var at = cad.AttributeType;
                        var name = at.Name.Replace("Attribute", "");
                        var argStrings = new List<string>();

                        // positional constructor args
                        foreach (var ca in cad.ConstructorArguments)
                        {
                            argStrings.Add(FormatCustomAttributeTypedArgument(ca));
                        }

                        // named args (properties/fields)
                        foreach (var na in cad.NamedArguments)
                        {
                            var valStr = FormatCustomAttributeTypedArgument(na.TypedValue);
                            argStrings.Add(na.MemberName + " = " + valStr);
                        }

                        if (argStrings.Count > 0)
                            sb.AppendLine($"    [{name}({string.Join(", ", argStrings)})]");
                        else
                            sb.AppendLine($"    [{name}]");
                    }
                }
                else
                {
                    // fallback: no CustomAttributeData available, try previous method
                    var attrs = f.GetCustomAttributes(true).Cast<object>().ToArray();
                    foreach (var a in attrs)
                    {
                        var at = a.GetType();
                        var args = new List<string>();

                        var ctors = at.GetConstructors(BindingFlags.Public | BindingFlags.Instance).OrderBy(c => c.GetParameters().Length).ToArray();
                        ConstructorInfo chosenCtor = ctors.FirstOrDefault();
                        if (chosenCtor != null && chosenCtor.GetParameters().Length > 0)
                        {
                            foreach (var param in chosenCtor.GetParameters())
                            {
                                string pname = param.Name;
                                object val = null;
                                var prop = at.GetProperty(pname, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                                if (prop != null && prop.CanRead) { try { val = prop.GetValue(a); } catch { } }
                                else
                                {
                                    var pascal = char.ToUpperInvariant(pname[0]) + pname.Substring(1);
                                    prop = at.GetProperty(pascal, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                                    if (prop != null && prop.CanRead) { try { val = prop.GetValue(a); } catch { } }
                                    else
                                    {
                                        var fld = at.GetField(pname, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                                        if (fld != null) { try { val = fld.GetValue(a); } catch { } }
                                        else { fld = at.GetField(pascal, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase); if (fld != null) try { val = fld.GetValue(a); } catch { } }
                                    }
                                }

                                if (val != null) args.Add(FormatValue(val));
                            }
                        }

                        var name = at.Name.Replace("Attribute", "");
                        if (args.Count > 0) sb.AppendLine($"    [{name}({string.Join(", ", args)})]"); else sb.AppendLine($"    [{name}]");
                    }
                }

                // field declaration with default value if instance provided
                var ftype = f.FieldType;
                string defaultVal = "";
                if (instance != null)
                {
                    try
                    {
                        var v = f.GetValue(instance);
                        if (v != null)
                            defaultVal = " = " + FormatValue(v);
                    }
                    catch { }
                }

                sb.AppendLine($"    public {GetTypeName(ftype)} {f.Name}{defaultVal};");
                sb.AppendLine();
            }

            sb.AppendLine("}");
            return sb.ToString();
        }

        private string FormatValue(object val)
        {
            if (val == null) return "null";
            if (val is string) return "\"" + val.ToString() + "\"";
            if (val is char) return "'" + val.ToString() + "'";
            if (val is bool) return ((bool)val) ? "true" : "false";
            if (val.GetType().IsEnum) return val.GetType().Name + "." + val.ToString();
            if (val is float f)
            {
                // format floats: if whole number, omit decimal point and suffix
                if (Math.Abs(f - Math.Round(f)) < 0.000001f)
                    return ((int)Math.Round(f)).ToString();
                return f.ToString(System.Globalization.CultureInfo.InvariantCulture) + "f";
            }
            if (val is double) return ((double)val).ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (val is Vector2 v2) return $"new Vector2({v2.x}f, {v2.y}f)";
            if (val is Vector2Int vi2) return $"new Vector2Int({vi2.x}, {vi2.y})";
            if (val is Vector3 v3) return $"new Vector3({v3.x}f, {v3.y}f, {v3.z}f)";
            if (val is int || val is long || val is short || val is byte) return val.ToString();
            if (val is Array arr)
            {
                var items = new List<string>();
                foreach (var o in arr)
                    items.Add(FormatValue(o));
                return "new[] { " + string.Join(", ", items) + " }";
            }
            return val.ToString();
        }

        private string FormatCustomAttributeTypedArgument(CustomAttributeTypedArgument ca)
        {
            var val = ca.Value;
            if (val == null) return "null";
            // handle enums wrapped in System.Reflection.CustomAttributeTypedArgument
            if (ca.ArgumentType.IsEnum)
            {
                return ca.ArgumentType.Name + "." + val.ToString();
            }
            // arrays
            if (val is IList<CustomAttributeTypedArgument> list)
            {
                var items = list.Select(i => FormatCustomAttributeTypedArgument(i)).ToArray();
                return "new[] { " + string.Join(", ", items) + " }";
            }
            // primitive fallback
            return FormatValue(val);
        }

        private string GetTypeName(Type t)
        {
            if (t == typeof(int)) return "int";
            if (t == typeof(float)) return "float";
            if (t == typeof(double)) return "double";
            if (t == typeof(string)) return "string";
            if (t == typeof(bool)) return "bool";
            if (t == typeof(Vector2)) return "Vector2";
            if (t == typeof(Vector2Int)) return "Vector2Int";
            if (t == typeof(Vector3)) return "Vector3";
            if (t.IsArray) return GetTypeName(t.GetElementType()) + "[]";
            return t.Name;
        }

        private class AttributeInfo
        {
            public string Name;
            public string Description;
            public string LinkSuffix;

            public Type ExampleType;

            public string Code;
            public string Version;
            public bool CanUseInEditor;
            public bool ForceShowingMonoscript = false;
            public string[] ExclusiveTypes; 

            public AttributeInfo(string name, string desc, string linkSuffix, 
                Type exampleType, string code,
                string version = null, bool canUseInEditor = true, bool forceShowingMonoscript = true, string[] exclusiveTypes = null)
            {
                Name = name; 
                Description = desc; 
                LinkSuffix = linkSuffix;

                // Demo Class
                ExampleType = exampleType;

                // Code
                Code = code;

                // Settings
                Version = version;
                CanUseInEditor = canUseInEditor; 
                ForceShowingMonoscript = forceShowingMonoscript; 
                ExclusiveTypes = exclusiveTypes;
            }
        }

        // Helper: basic syntax highlighter for display purposes
        private VisualElement CreateHighlightedCodeElement(string code)
        {
            var container = new VisualElement();
            container.style.alignSelf = Align.Stretch;
            container.style.paddingLeft = 8;
            container.style.paddingRight = 8;
            container.style.paddingTop = 8;
            container.style.paddingBottom = 8;
            container.style.marginTop = 0;
            container.style.marginBottom = 8;
            container.style.marginLeft = 3;
            container.style.marginRight = 8;
            container.style.borderRightWidth = 1;
            container.style.borderRightColor = new StyleColor(new Color(25f/255,25f/255,25f/255,0.6f));
            container.style.borderLeftWidth = 1;
            container.style.borderLeftColor = new StyleColor(new Color(25f/255,25f/255,25f/255,0.6f));
            container.style.borderBottomWidth = 1;
            container.style.borderBottomColor = new StyleColor(new Color(25f/255,25f/255,25f/255,0.6f));
            container.style.borderTopWidth = 1;
            container.style.borderTopColor = new StyleColor(new Color(25f/255,25f/255,25f/255,0.6f));
            container.style.backgroundColor = new Color(.14f, .14f, .14f, .5f);

            container.style.borderBottomLeftRadius = 3;
            container.style.borderBottomRightRadius = 3;

            var lines = Regex.Split(code, "\r\n|\r|\n");
            var keywordPattern = "\\b(public|private|protected|internal|class|void|int|float|double|string|bool|new|return|if|else|for|while|var|const|using|namespace|static|readonly|enum|abstract|virtual|override)\\b";
            var numberPattern = "\\b\\d+\\.?\\d*\\b";
            var attributePattern = "\\[[^\\]]+\\]";
            var typePattern = "\\b[A-Z][A-Za-z0-9_\\<\\>]*\\b";

            foreach (var line in lines)
            {
                var lineContainer = new VisualElement();
                lineContainer.style.flexDirection = FlexDirection.Row;
                lineContainer.style.whiteSpace = WhiteSpace.Normal;

                if (string.IsNullOrEmpty(line))
                {
                    var empty = new Label("\u00A0"); // non-breaking space to preserve empty line
                    empty.style.whiteSpace = WhiteSpace.Normal;
                    lineContainer.Add(empty);
                    container.Add(lineContainer);
                    continue;
                }

                // Tokenize: attributes, strings, comments, keywords, numbers, types, others
                var pattern = "(" + attributePattern + "|\\\".*?\\\"|'.*?'|//.*$|/\\*[\\s\\S]*?\\*/|" + keywordPattern + "|" + numberPattern + "|" + typePattern + ")";
                var matches = Regex.Matches(line, pattern);

                int lastIndex = 0;
                foreach (Match m in matches)
                {
                    if (m.Index > lastIndex)
                    {
                        var between = line.Substring(lastIndex, m.Index - lastIndex);
                        var lblBetween = new Label(between) { style = { unityFontStyleAndWeight = FontStyle.Normal } };
                        lblBetween.style.whiteSpace = WhiteSpace.Normal;
                        lineContainer.Add(lblBetween);
                    }

                    var token = m.Value;
                    var tokenLabel = new Label(token);
                    tokenLabel.style.whiteSpace = WhiteSpace.Normal;
                    tokenLabel.AddToClassList("CodePreviewText");

                    // Attributes
                    if (Regex.IsMatch(token, "^" + attributePattern + "$"))
                    {
                        // token looks like: [Name] or [Name(arg1, arg2)]
                        var inner = token.Substring(1, token.Length - 2); // strip [ and ]
                        var openParen = inner.IndexOf('(');
                        string attrName;
                        string argsStr = null;
                        if (openParen >= 0)
                        {
                            attrName = inner.Substring(0, openParen);
                            if (inner.EndsWith(")"))
                                argsStr = inner.Substring(openParen + 1, inner.Length - openParen - 2);
                            else
                                argsStr = inner.Substring(openParen + 1);
                        }
                        else
                        {
                            attrName = inner;
                        }

                        // '['
                        var lb = new Label("[") { style = { unityFontStyleAndWeight = FontStyle.Normal } };
                        lb.AddToClassList("CodePreviewText");
                        lb.style.whiteSpace = WhiteSpace.Normal;
                        lineContainer.Add(lb);

                        // attribute name (distinct color)
                        var nameLabel = new Label(attrName) { style = { unityFontStyleAndWeight = FontStyle.Bold } };
                        nameLabel.style.color = new StyleColor(new Color(0.8f, 0.45f, 0.05f));
                        nameLabel.style.whiteSpace = WhiteSpace.Normal;
                        nameLabel.AddToClassList("CodePreviewText");
                        lineContainer.Add(nameLabel);

                        if (argsStr != null)
                        {
                            var lp = new Label("(") { style = { unityFontStyleAndWeight = FontStyle.Normal } };
                            lp.style.whiteSpace = WhiteSpace.Normal;
                            lp.AddToClassList("CodePreviewText");
                            lineContainer.Add(lp);

                            // Split args while preserving quoted strings and commas
                            var argMatches = Regex.Matches(argsStr, "(\".*?\"|'.*?'|,|[^,]+)");
                            foreach (Match am in argMatches)
                            {
                                var a = am.Value;
                                if (a == ",")
                                {
                                    var comma = new Label(",") { style = { unityFontStyleAndWeight = FontStyle.Normal } };
                                    comma.style.whiteSpace = WhiteSpace.Normal;
                                    comma.AddToClassList("CodePreviewText");
                                    lineContainer.Add(comma);
                                    continue;
                                }

                                // preserve whitespace-only segments
                                if (string.IsNullOrWhiteSpace(a))
                                {
                                    var ws = new Label(a) { style = { unityFontStyleAndWeight = FontStyle.Normal } };
                                    ws.style.whiteSpace = WhiteSpace.Normal;
                                    ws.AddToClassList("CodePreviewText");
                                    lineContainer.Add(ws);
                                    continue;
                                }

                                var atok = a.Trim();
                                var atokLabel = new Label(atok) { style = { unityFontStyleAndWeight = FontStyle.Normal } };
                                atokLabel.AddToClassList("CodePreviewText");
                                atokLabel.style.whiteSpace = WhiteSpace.Normal;

                                // color strings differently
                                if ((atok.StartsWith("\"") && atok.EndsWith("\"")) || (atok.StartsWith("'") && atok.EndsWith("'")))
                                {
                                    atokLabel.style.color = new StyleColor(new Color(126f / 255, 185f / 255, 100f / 255)); // string 
                                }
                                else if (Regex.IsMatch(atok, "^\\d+\\.?\\d*$"))
                                {
                                    atokLabel.style.color = new StyleColor(new Color(227f / 255, 147f / 255, 20f / 255));  // number 
                                }
                                else
                                {
                                    // attribute argument identifiers (different from attribute name)
                                    atokLabel.style.color = new StyleColor(new Color(1f / 255, 222f / 255, 190f / 255));
                                    atokLabel.style.unityFontStyleAndWeight = FontStyle.Normal;
                                }

                                lineContainer.Add(atokLabel);
                            }

                            var rp = new Label(")") { style = { unityFontStyleAndWeight = FontStyle.Normal } };
                            rp.style.whiteSpace = WhiteSpace.Normal;
                            rp.AddToClassList("CodePreviewText");
                            lineContainer.Add(rp);
                        }

                        // ']'
                        var rb = new Label("]") { style = { unityFontStyleAndWeight = FontStyle.Normal } };
                        rb.style.whiteSpace = WhiteSpace.Normal;
                        rb.AddToClassList("CodePreviewText");
                        lineContainer.Add(rb);

                        // we've handled the attribute token, skip default token handling
                        lastIndex = m.Index + m.Length;
                        continue;
                    }
                    // Comments
                    else if (token.StartsWith("//") || token.StartsWith("/*"))
                    {
                        tokenLabel.style.color = new StyleColor(new Color(0.3f, 0.6f, 0.3f)); // comment green
                    }
                    // Strings
                    else if ((token.StartsWith("\"") && token.EndsWith("\"")) || (token.StartsWith("'") && token.EndsWith("'")))
                    {
                        tokenLabel.style.color = new StyleColor(new Color(244f / 255, 222f / 255, 106f / 255)); // string red
                    }
                    // Keywords (including access modifiers)
                    else if (Regex.IsMatch(token, "^" + keywordPattern + "$"))
                    {
                        tokenLabel.style.color = new StyleColor(new Color(1f / 255, 222f / 255, 190f / 255));  // keyword blue
                        tokenLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                    }
                    // Numbers
                    else if (Regex.IsMatch(token, "^" + numberPattern + "$"))
                    {
                        tokenLabel.style.color = new StyleColor(new Color(0.6f, 0.2f, 0.6f)); // number purple
                    }
                    // Types (capitalized identifiers)
                    else if (Regex.IsMatch(token, "^" + typePattern + "$"))
                    {
                        tokenLabel.style.color = new StyleColor(new Color(0.05f, 0.5f, 0.45f)); // type teal
                        tokenLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                    }
                    else
                    {
                        tokenLabel.style.color = new StyleColor(new Color(0f, 0f, 0f));
                    }

                    lineContainer.Add(tokenLabel);
                    lastIndex = m.Index + m.Length;
                }

                if (lastIndex < line.Length)
                {
                    var tail = line.Substring(lastIndex);
                    var lblTail = new Label(tail) { style = { unityFontStyleAndWeight = FontStyle.Normal } };
                    lblTail.style.whiteSpace = WhiteSpace.Normal;
                    lblTail.AddToClassList("CodePreviewText");
                    lineContainer.Add(lblTail);
                }

                container.Add(lineContainer);
            }

            return container;
        }
    }
}
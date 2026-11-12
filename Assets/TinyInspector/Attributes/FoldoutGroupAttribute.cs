using UnityEngine;

namespace TinyInspector
{
    public class FoldoutGroupAttribute : PropertyAttribute
    {
        public string GroupName;
        public TinyIcon IconName;
        public bool DefaultExpanded;
        public TinyColor Color = TinyColor.Default;


        /// <summary>
        /// Organizes properties into a collapsible foldout section,
        /// allowing users to hide values that are not currently needed.
        /// </summary>
        /// <param name="GroupName">Name of the foldout group.</param>
        /// <param name="ExpandedByDefualt">Whether the foldout is expanded by default.</param>
        public FoldoutGroupAttribute(string GroupName, bool ExpandedByDefualt = false)
        {
            this.GroupName = GroupName;
            this.DefaultExpanded = ExpandedByDefualt;
        }


        /// <summary>
        /// Organizes properties into a collapsible foldout section,
        /// allowing users to hide values that are not currently needed.
        /// </summary>
        /// <param name="GroupName">Name of the foldout group.</param>
        /// <param name="Icon">Optional icon displayed in the foldout header.</param>
        /// <param name="ExpandedByDefualt">Whether the foldout is expanded by default.</param>
        public FoldoutGroupAttribute(string GroupName, TinyIcon Icon, bool ExpandedByDefualt = false)
        {
            this.GroupName = GroupName;
            this.IconName = Icon;
            this.DefaultExpanded = ExpandedByDefualt;
        }


        /// <summary>
        /// Organizes properties into a collapsible foldout section,
        /// allowing users to hide values that are not currently needed.
        /// </summary>
        /// <param name="GroupName">Name of the foldout group.</param>
        /// <param name="Color">Optional color of the foldout section.</param>
        /// <param name="ExpandedByDefualt">Whether the foldout is expanded by default.</param>
        public FoldoutGroupAttribute(string GroupName, TinyColor Color, bool ExpandedByDefualt = false)
        {
            this.GroupName = GroupName;
            this.Color = Color;
            this.DefaultExpanded = ExpandedByDefualt;
        }


        /// <summary>
        /// Organizes properties into a collapsible foldout section,
        /// allowing users to hide values that are not currently needed.
        /// </summary>
        /// <param name="GroupName">Name of the foldout group.</param>
        /// <param name="Icon">Optional icon displayed in the foldout header.</param>
        /// <param name="Color">Optional color of the foldout section.</param>
        /// <param name="ExpandedByDefualt">Whether the foldout is expanded by default.</param>
        public FoldoutGroupAttribute(string GroupName, TinyIcon Icon, TinyColor Color, bool ExpandedByDefualt = false)
        {
            this.GroupName = GroupName;
            this.IconName = Icon;
            this.Color = Color;
            this.DefaultExpanded = ExpandedByDefualt;
        }
    }
} 

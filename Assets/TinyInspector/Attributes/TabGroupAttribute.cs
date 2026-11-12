using UnityEngine;

namespace TinyInspector
{
    public class TabGroupAttribute : PropertyAttribute
    {
        public string GroupName; // box name
        public string TabName;
        public TinyIcon Icon = TinyIcon.None;
        public TinyColor Color = TinyColor.Default;

        /// <summary>
        /// Splits properties into tabs within a group, allowing large
        /// sets of values to be organized into clear, logical sections.
        /// </summary>
        /// <param name="GroupName">Name of the parent group.</param>
        /// <param name="TabName">Name of the tab this property belongs to.</param>
        public TabGroupAttribute(string GroupName, string TabName)
        {
            this.GroupName = GroupName;
            this.TabName = TabName;
        }

        /// <summary>
        /// Splits properties into tabs within a group, allowing large
        /// sets of values to be organized into clear, logical sections.
        /// </summary>
        /// <param name="GroupName">Name of the parent group.</param>
        /// <param name="TabName">Name of the tab this property belongs to.</param>
        /// <param name="Icon">Optional icon displayed on the tab.</param>
        public TabGroupAttribute(string GroupName, string TabName, TinyIcon Icon)
        {
            this.GroupName = GroupName;
            this.TabName = TabName;
            this.Icon = Icon;
        }

        /// <summary>
        /// Splits properties into tabs within a group, allowing large
        /// sets of values to be organized into clear, logical sections.
        /// </summary>
        /// <param name="GroupName">Name of the parent group.</param>
        /// <param name="TabName">Name of the tab this property belongs to.</param>
        /// <param name="Color">Optional color assigned to the tab.</param>
        public TabGroupAttribute(string GroupName, string TabName, TinyColor Color)
        {
            this.GroupName = GroupName;
            this.TabName = TabName;
            this.Color = Color;
        }

        /// <summary>
        /// Splits properties into tabs within a group, allowing large
        /// sets of values to be organized into clear, logical sections.
        /// </summary>
        /// <param name="GroupName">Name of the parent group.</param>
        /// <param name="TabName">Name of the tab this property belongs to.</param>
        /// <param name="Icon">Optional icon displayed on the tab.</param>
        /// <param name="Color">Optional color assigned to the tab.</param>
        public TabGroupAttribute(string GroupName, string TabName, TinyIcon Icon, TinyColor Color)
        {
            this.GroupName = GroupName;
            this.TabName = TabName;
            this.Icon = Icon;
            this.Color = Color; 
        }
    }
}

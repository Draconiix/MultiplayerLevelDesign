using UnityEngine;

namespace TinyInspector
{
    public class BoxGroupAttribute : PropertyAttribute
    {
        public string GroupName;
        public TinyIcon Icon = TinyIcon.None;
        public TinyColor Color = TinyColor.Default;

        /// <summary>
        /// Places properties inside a boxed section in the Inspector,
        /// grouping related values into a clear and visually separated block.
        /// </summary>
        /// <param name="GroupName">Name of the group and box header.</param>
        public BoxGroupAttribute(string GroupName)
        {
            this.GroupName = GroupName;
        }

        /// <summary>
        /// Places properties inside a boxed section in the Inspector,
        /// grouping related values into a clear and visually separated block.
        /// </summary>
        /// <param name="GroupName">Name of the group and box header.</param>
        /// <param name="Icon">Optional icon displayed in the box header.</param>
        public BoxGroupAttribute(string GroupName, TinyIcon Icon)
        {
            this.GroupName = GroupName;
            this.Icon = Icon;
        }

        /// <summary>
        /// Places properties inside a boxed section in the Inspector,
        /// grouping related values into a clear and visually separated block.
        /// </summary>
        /// <param name="GroupName">Name of the group and box header.</param>
        /// <param name="Color">Optional background color of the box.</param>
        public BoxGroupAttribute(string GroupName, TinyColor Color)
        {
            this.GroupName = GroupName;
            this.Color = Color;
        }

        /// <summary>
        /// Places properties inside a boxed section in the Inspector,
        /// grouping related values into a clear and visually separated block.
        /// </summary>
        /// <param name="GroupName">Name of the group and box header.</param>
        /// <param name="Icon">Optional icon displayed in the box header.</param>
        /// <param name="Color">Optional background color of the box.</param>
        public BoxGroupAttribute(string GroupName, TinyIcon Icon, TinyColor Color)
        {
            this.GroupName = GroupName;
            this.Icon = Icon;
            this.Color = Color;
        } 
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TinyInspector
{
    public class TitleAttribute : PropertyAttribute
    {
        public readonly string title = "";
        public readonly string desc = "";

        public readonly TinyIcon icon = TinyIcon.None;

        public readonly bool horizontal = true;
        public readonly bool drawLine = false;


        /// <summary>
        /// Extends a section header with a title, description, and optional icon,
        /// allowing clear visual separation and documentation of Inspector sections.
        /// </summary>
        /// <param name="Title">Main header title text.</param>
        /// <param name="RowDirection">If true, renders title and description in a horizontal layout.</param>
        /// <param name="Separator">If true, draws a separator line under the header.</param>
        public TitleAttribute(string Title, bool RowDirection = true, bool Separator = false)
        {
            this.title = Title;
            this.horizontal = RowDirection;
            this.drawLine = Separator;
        }

        /// <summary>
        /// Extends a section header with a title, description, and optional icon,
        /// allowing clear visual separation and documentation of Inspector sections.
        /// </summary>
        /// <param name="Title">Main header title text.</param>
        /// <param name="Description">Optional descriptive text shown under the title.</param>
        /// <param name="RowDirection">If true, renders title and description in a horizontal layout.</param>
        /// <param name="Separator">If true, draws a separator line under the header.</param>
        public TitleAttribute(string Title, string Description, bool RowDirection = true, bool Separator = false)
        {
            this.title = Title;
            this.desc = Description;
            this.horizontal = RowDirection;
            this.drawLine = Separator;
        }

        /// <summary>
        /// Extends a section header with a title, description, and optional icon,
        /// allowing clear visual separation and documentation of Inspector sections.
        /// </summary>
        /// <param name="Title">Main header title text.</param>
        /// <param name="Icon">Icon displayed next to the title.</param>
        /// <param name="RowDirection">If true, renders title and description in a horizontal layout.</param>
        /// <param name="Separator">If true, draws a separator line under the header.</param>
        public TitleAttribute(string Title, TinyIcon Icon, bool RowDirection = true, bool Separator = false)
        {
            this.title = Title;
            this.icon = Icon;
            this.horizontal = RowDirection;
            this.drawLine = Separator;
        }

        /// <summary>
        /// Extends a section header with a title, description, and optional icon,
        /// allowing clear visual separation and documentation of Inspector sections.
        /// </summary>
        /// <param name="Title">Main header title text.</param>
        /// <param name="Description">Optional descriptive text shown under the title.</param>
        /// <param name="Icon">Icon displayed next to the title.</param>
        /// <param name="RowDirection">If true, renders title and description in a horizontal layout.</param>
        /// <param name="Separator">If true, draws a separator line under the header.</param>
        public TitleAttribute(string Title, string Description, TinyIcon Icon, bool RowDirection = true, bool Separator = false)
        {
            this.title = Title;
            this.desc = Description;
            this.icon = Icon;
            this.horizontal = RowDirection;
            this.drawLine = Separator;
        }
    }
}

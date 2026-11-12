using System;
using UnityEngine;

namespace TinyInspector
{
    /// <summary>
    /// Displays a live preview of the assigned asset directly in the Inspector,
    /// allowing quick inspection without opening separate preview windows.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class PreviewFieldAttribute : PropertyAttribute
    {
        public int GridSize = 3;
        public bool ShowField = true;

        /// <summary>
        /// Displays a live preview of the assigned asset directly in the Inspector,
        /// allowing quick inspection without opening separate preview windows.
        /// </summary>
        /// <param name="GridSize">Size of the preview grid in Inspector rows.</param>
        /// <param name="ShowField">If true, also renders the default object field.</param>
        public PreviewFieldAttribute(int GridSize = 3, bool ShowField = true)
        {
            this.GridSize = Mathf.Max(1, GridSize);
            this.ShowField = ShowField;
        }

        /// <summary>
        /// Displays a live preview of the assigned asset directly in the Inspector,
        /// allowing quick inspection without opening separate preview windows.
        /// </summary>
        /// <param name="GridSize">Size of the preview grid in Inspector rows.</param>
        public PreviewFieldAttribute(int GridSize)
        {
            this.GridSize = Mathf.Max(1, GridSize);
        }

        /// <summary>
        /// Displays a live preview of the assigned asset directly in the Inspector,
        /// allowing quick inspection without opening separate preview windows.
        /// </summary>
        /// <param name="ShowField">If true, also renders the default object field.</param>
        public PreviewFieldAttribute(bool ShowField)
        {
            this.ShowField = ShowField;
        }
    }
}

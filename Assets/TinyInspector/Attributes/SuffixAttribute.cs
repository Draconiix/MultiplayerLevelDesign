using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TinyInspector
{
    public class SuffixAttribute : PropertyAttribute
    {
        public readonly string suffix;
        public readonly TinyIcon icon = TinyIcon.None;
        public bool overlay = true;

        /// <summary>
        /// Adds a text or icon suffix to a field, providing additional
        /// context or units directly in the Inspector UI.
        /// </summary>
        /// <param name="Suffix">Text displayed as the field suffix.</param>
        /// <param name="Icon">Icon displayed alongside the suffix.</param>
        /// <param name="IsOverlay">If true, the suffix is rendered as an overlay.</param>
        public SuffixAttribute(string Suffix = null, TinyIcon Icon = TinyIcon.None, bool IsOverlay = true)
        {
            this.suffix = Suffix ?? string.Empty;
            this.icon = Icon;
            this.overlay = IsOverlay;
        }
    }
}

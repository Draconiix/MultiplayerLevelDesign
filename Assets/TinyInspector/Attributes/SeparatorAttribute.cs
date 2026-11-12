using System;
using UnityEngine;

namespace TinyInspector
{
    /// <summary>
    /// SeparatorAttribute allows decorating a field with a visual separator in the inspector.
    /// It always renders a single horizontal line and allows controlling the line height and vertical spacing.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class SeparatorAttribute : PropertyAttribute
    {
        public float LineHeight = 1f;
        public float SpacingTop = 4f;
        public float SpacingBottom = 4f;
        public TinyColor Color  = TinyColor.Default;

        /// <summary>
        /// Draws a visual separator line between Inspector fields,
        /// improving readability and logical grouping of properties.
        /// </summary>
        /// <param name="Color">Color of the separator line.</param>
        public SeparatorAttribute(TinyColor Color = TinyColor.Default)
        {
            this.Color = Color;
        }

        /// <summary>
        /// Draws a visual separator line between Inspector fields,
        /// improving readability and logical grouping of properties.
        /// </summary>
        /// <param name="SpacingTop">Vertical spacing above the separator.</param>
        /// <param name="SpacingBottom">Vertical spacing below the separator.</param>
        /// <param name="Color">Color of the separator line.</param>
        public SeparatorAttribute(float SpacingTop, float SpacingBottom, TinyColor Color = TinyColor.Default)
        {
            this.SpacingTop = Mathf.Max(0f, SpacingTop);
            this.SpacingBottom = Mathf.Max(0f, SpacingBottom);
            this.Color = Color;
        }

        /// <summary>
        /// Draws a visual separator line between Inspector fields,
        /// improving readability and logical grouping of properties.
        /// </summary>
        /// <param name="LineHeight">Thickness of the separator line.</param>
        /// <param name="Color">Color of the separator line.</param>
        public SeparatorAttribute(float LineHeight, TinyColor Color = TinyColor.Default)
        {
            this.LineHeight = Mathf.Max(0f, LineHeight);
            this.Color = Color;
        }

        /// <summary>
        /// Draws a visual separator line between Inspector fields,
        /// improving readability and logical grouping of properties.
        /// </summary>
        /// <param name="LineHeight">Thickness of the separator line.</param>
        /// <param name="SpacingTop">Vertical spacing above the separator.</param>
        /// <param name="SpacingBottom">Vertical spacing below the separator.</param>
        /// <param name="Color">Color of the separator line.</param>
        public SeparatorAttribute(float LineHeight, float SpacingTop, float SpacingBottom, TinyColor Color = TinyColor.Default)
        {
            this.LineHeight = Mathf.Max(0f, LineHeight);
            this.SpacingTop = Mathf.Max(0f, SpacingTop);
            this.SpacingBottom = Mathf.Max(0f, SpacingBottom);
            this.Color = Color;
        }
    }
}

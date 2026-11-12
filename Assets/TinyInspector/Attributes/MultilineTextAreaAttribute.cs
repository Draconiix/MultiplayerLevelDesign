using System;
using UnityEngine;

namespace TinyInspector
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class MultilineTextAreaAttribute : PropertyAttribute
    {
        public int MaxCharacter { get; }
        public bool FullWidth { get; }

        public int StartLines { get; }

        /// <summary>
        /// Creates a multiline text area with configurable height and optional character limit for flexible inspector input.
        /// </summary>
        /// <param name="Lines">
        /// Number of visible text lines. Minimum value is 1.
        /// </param>
        /// <param name="MaxCharacter">
        /// Optional maximum number of allowed characters. 
        /// Set to -1 to disable the character limit.
        /// </param>
        /// <param name="FullWidth">
        /// If true, the text area expands to use the full available inspector width.
        /// </param>
        public MultilineTextAreaAttribute(int Lines = 3, int MaxCharacter = -1, bool FullWidth = false)
        {
            this.MaxCharacter = MaxCharacter;
            this.FullWidth = FullWidth;
            StartLines = Mathf.Max(1, Lines);
        }
    }
}

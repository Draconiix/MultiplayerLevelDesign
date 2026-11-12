using System;
using UnityEngine;
using TinyInspector;

namespace TinyInspector
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class CustomLabelAttribute : PropertyAttribute
    {
        public string Label { get; }
        public TinyIcon Icon { get; }

        /// <summary>
        /// Overrides the default field label and optionally displays
        /// an icon next to it in the Inspector.
        /// </summary>
        /// <param name="Label">Custom label text.</param>
        /// <param name="Icon">Optional icon displayed next to the label.</param>
        public CustomLabelAttribute(string Label, TinyIcon Icon = TinyIcon.None)
        {
            this.Label = Label ?? string.Empty;
            this.Icon = Icon;
        }
    }
}

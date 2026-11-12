using System;
using UnityEngine;

namespace TinyInspector
{
    /// <summary>
    /// Hides the label for a field when drawing in the inspector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class HideLabelAttribute : PropertyAttribute
    {
        /// <summary>
        /// Hides the default label of a field in the Inspector,
        /// leaving only the field control visible.
        /// </summary>
        public HideLabelAttribute() { }
    }
}

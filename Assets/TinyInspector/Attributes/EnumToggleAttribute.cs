using System;
using UnityEngine;

namespace TinyInspector
{
    /// <summary>
    /// Renders enum values as a toggle grid rather than a dropdown menu,
    /// improving visibility and speeding up value selection in the Inspector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class EnumToggleAttribute : PropertyAttribute
    {
        /// <summary>
        /// Renders enum values as a toggle grid rather than a dropdown menu,
        /// improving visibility and speeding up value selection in the Inspector.
        /// </summary>
        public EnumToggleAttribute() { }
    }
}

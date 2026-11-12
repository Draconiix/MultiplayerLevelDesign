using System;
using UnityEngine;

namespace TinyInspector
{
    /// <summary>
    /// Displays a dropdown list of all scenes included in the Build Settings,
    /// allowing safe and convenient scene selection directly from the Inspector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class SceneDropdownAttribute : PropertyAttribute
    {
        /// <summary>
        /// Displays a dropdown list of all scenes included in the Build Settings,
        /// allowing safe and convenient scene selection directly from the Inspector.
        /// </summary>
        public SceneDropdownAttribute() { } 
    }
}

using System;
using UnityEngine;

namespace TinyInspector
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class PropertySpaceAttribute : PropertyAttribute
    {
        public float Height { get; }

        /// <summary>
        /// Adds vertical spacing between Inspector fields, similar to Unity's
        /// default space, but with explicit height control for precise layout.
        /// </summary>
        public PropertySpaceAttribute()
        {
            Height = 8f; // match Unity's default Space
        }

        /// <summary>
        /// Adds vertical spacing between Inspector fields, similar to Unity's
        /// default space, but with explicit height control for precise layout.
        /// </summary>
        /// <param name="spaceBefore">Spacing in pixels before the field. Default is 8 (matches Unity's default Space).</param>
        /// <param name="spaceAfter">Spacing in pixels after the field. Default is 0.</param>
        public PropertySpaceAttribute(float spaceBefore)
        {
            Height = spaceBefore;
        }
    }
}

using System;
using UnityEngine;

namespace TinyInspector
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class HorizontalGroupAttribute : PropertyAttribute
    {
        public string GroupName { get; }

        public HorizontalGroupAttribute(string groupName)
        {
            GroupName = groupName; 
        }
        /// <summary>
        /// Arranges multiple properties horizontally in a single row,
        /// enabling compact and space-efficient Inspector layouts.
        /// </summary>
        /// <param name="groupName">Name of the horizontal group.</param>
        public HorizontalGroupAttribute() : this(string.Empty) { }
    }
}

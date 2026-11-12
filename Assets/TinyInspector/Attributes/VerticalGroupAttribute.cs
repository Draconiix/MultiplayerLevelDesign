using System;
using UnityEngine;

namespace TinyInspector
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class VerticalGroupAttribute : PropertyAttribute
    {
        public string GroupName { get; }


        /// <summary>
        /// Collects properties into a vertical group in the Inspector.
        /// Most useful when combined with other group attributes to
        /// precisely control layout and alignment.
        /// </summary>
        /// <param name="groupName">Name of the vertical group.</param>
        public VerticalGroupAttribute(string groupName)
        {
            GroupName = groupName; 
        }
    }
}

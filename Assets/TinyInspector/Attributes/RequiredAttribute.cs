using System;
using UnityEngine;

namespace TinyInspector
{
    // Runtime attribute to mark fields as required. When optional=true it displays a warning; when optional=false it displays an error.
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class RequiredAttribute : PropertyAttribute
    {
        public readonly bool isRequired;

        /// <summary>
        /// Displays a validation warning in the Inspector when the field
        /// value is null or empty, indicating that the value is required.
        /// </summary>
        public RequiredAttribute()
        {
            this.isRequired = true;
        }

        /// <summary>
        /// Displays a validation warning in the Inspector when the field
        /// value is null or empty, indicating that the value is required.
        /// </summary>
        /// <param name="IsRequired">
        /// Enables or disables the required validation check.
        /// </param>
        public RequiredAttribute(bool IsRequired = true)
        {
            this.isRequired = IsRequired;
        }
    }
}


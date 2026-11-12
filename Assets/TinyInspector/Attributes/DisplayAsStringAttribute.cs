using System;
using UnityEngine;

namespace TinyInspector
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class DisplayAsStringAttribute : PropertyAttribute
    {
        public bool EnableLabel = false;
        public TinyIcon Icon = TinyIcon.None;



        /// <summary>
        /// Displays a property value as read-only text in the Inspector,
        /// without rendering an editable field.
        /// </summary>
        public DisplayAsStringAttribute()
        {
        }

        /// <summary>
        /// Displays a property value as read-only text in the Inspector,
        /// without rendering an editable field.
        /// </summary>
        /// <param name="ShowPropertyLabel">If true, shows the property label.</param>
        public DisplayAsStringAttribute(bool ShowPropertyLabel)
        {
            this.EnableLabel = ShowPropertyLabel;
        }

        /// <summary>
        /// Displays a property value as read-only text in the Inspector,
        /// without rendering an editable field.
        /// </summary>
        /// <param name="Icon">Optional icon displayed next to the value.</param>
        public DisplayAsStringAttribute(TinyIcon Icon)
        {
            this.Icon = Icon;
        }

        /// <summary>
        /// Displays a property value as read-only text in the Inspector,
        /// without rendering an editable field.
        /// </summary>
        /// <param name="ShowPropertyLabel">If true, shows the property label.</param>
        /// <param name="Icon">Optional icon displayed next to the value.</param>
        public DisplayAsStringAttribute(bool ShowPropertyLabel, TinyIcon Icon)
        {
            this.EnableLabel = ShowPropertyLabel;
            this.Icon = Icon;
        }
    }
}

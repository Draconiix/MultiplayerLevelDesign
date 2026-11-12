using System;
using UnityEngine;

namespace TinyInspector
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class SwitchAttribute : PropertyAttribute
    {
        public string OffLabel { get; }
        public string OnLabel { get; }
        public bool Expand { get; }
        public TinyColor Color { get; } = TinyColor.Default;

        /// <summary>
        /// Replaces the default boolean field with a switch-style control
        /// featuring custom on/off labels and optional color styling.
        /// </summary>
        /// <param name="Color">Optional color applied to the switch.</param>
        /// <param name="Expand">If true, the switch expands to full width.</param>
        public SwitchAttribute(TinyColor Color = TinyColor.Default,bool Expand = false)
        {
            OffLabel = "OFF";
            OnLabel = "ON";
            this.Expand = Expand;
            this.Color = Color;
        }
        /// <summary>
        /// Replaces the default boolean field with a switch-style control
        /// featuring custom on/off labels and optional color styling.
        /// </summary>
        /// <param name="LabelWhenOn">Label displayed when the value is true.</param>
        /// <param name="LabelWhenOff">Label displayed when the value is false.</param>
        /// <param name="Color">Optional color applied to the switch.</param>
        /// <param name="Expand">If true, the switch expands to full width.</param>
        public SwitchAttribute(string LabelWhenOn, string LabelWhenOff, TinyColor Color = TinyColor.Default, bool Expand = false)
        {
            OffLabel = LabelWhenOn;
            OnLabel = LabelWhenOff;
            this.Expand = Expand;
            this.Color = Color;
        }
    }
}

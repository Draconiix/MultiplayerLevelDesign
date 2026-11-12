using System;
using UnityEngine;

namespace TinyInspector
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class ProgressBarAttribute : PropertyAttribute
    {
        public float Min = 0;
        public float Max = 100;
        public float Height = 16f;
        public string BarText = "";

        public TinyColor FillColor = TinyColor.Default;
        public bool ShowValue = true;
        public bool FullWidth = false;
        public bool ShowValueField = true;

        /// <summary>
        /// Displays a horizontal progress bar instead of the default field,
        /// visually representing the property's value within a defined range.
        /// </summary>
        /// <param name="MinValue">Minimum value represented by the bar.</param>
        /// <param name="MaxValue">Maximum value represented by the bar.</param>
        /// <param name="Color">Fill color of the progress bar.</param>
        /// <param name="ShowValueText">Show the current value as text on the bar.</param>
        /// <param name="FullWidth">Expand the bar to fill available width.</param>
        /// <param name="ShowValueField">Show an editable value field next to the bar.</param>
        public ProgressBarAttribute(float MinValue, float MaxValue, TinyColor Color = TinyColor.Default, bool ShowValueText = true, bool FullWidth = false, bool ShowValueField = true)
        {
            this.Min = MinValue;
            this.Max = MaxValue;

            this.FillColor = Color;
            this.ShowValue = ShowValueText;
            this.FullWidth = FullWidth;
            this.ShowValueField = ShowValueField;
        }

        /// <summary>
        /// Displays a horizontal progress bar instead of the default field,
        /// visually representing the property's value within a defined range.
        /// </summary>
        /// <param name="MinValue">Minimum value represented by the bar.</param>
        /// <param name="MaxValue">Maximum value represented by the bar.</param>
        /// <param name="BarHeight">Height of the progress bar in pixels.</param>
        /// <param name="Color">Fill color of the progress bar.</param>
        /// <param name="ShowValueText">Show the current value as text on the bar.</param>
        /// <param name="FullWidth">Expand the bar to fill available width.</param>
        /// <param name="ShowValueField">Show an editable value field next to the bar.</param>
        public ProgressBarAttribute(float MinValue, float MaxValue, float BarHeight, TinyColor Color = TinyColor.Default, bool ShowValueText = true, bool FullWidth = false, bool ShowValueField = true)
        {
            this.Min = MinValue;
            this.Max = MaxValue;
            this.Height = BarHeight;

            this.FillColor = Color;
            this.ShowValue = ShowValueText;
            this.FullWidth = FullWidth;
            this.ShowValueField = ShowValueField;
        }

        /// <summary>
        /// Displays a horizontal progress bar instead of the default field,
        /// visually representing the property's value within a defined range.
        /// </summary>
        /// <param name="MinValue">Minimum value represented by the bar.</param>
        /// <param name="MaxValue">Maximum value represented by the bar.</param>
        /// <param name="BarText">Optional text displayed inside the bar.</param>
        /// <param name="Color">Fill color of the progress bar.</param>
        /// <param name="ShowValueText">Show the current value as text on the bar.</param>
        /// <param name="FullWidth">Expand the bar to fill available width.</param>
        /// <param name="ShowValueField">Show an editable value field next to the bar.</param>
        public ProgressBarAttribute(float MinValue, float MaxValue, string BarText, TinyColor Color = TinyColor.Default, bool ShowValueText = true, bool FullWidth = false, bool ShowValueField = true)
        {
            this.Min = MinValue;
            this.Max = MaxValue;
            this.BarText = BarText;

            this.FillColor = Color;
            this.ShowValue = ShowValueText;
            this.FullWidth = FullWidth;
            this.ShowValueField = ShowValueField;
        }

        /// <summary>
        /// Displays a horizontal progress bar instead of the default field,
        /// visually representing the property's value within a defined range.
        /// </summary>
        /// <param name="MinValue">Minimum value represented by the bar.</param>
        /// <param name="MaxValue">Maximum value represented by the bar.</param>
        /// <param name="BarHeight">Height of the progress bar in pixels.</param>
        /// <param name="BarText">Optional text displayed inside the bar.</param>
        /// <param name="Color">Fill color of the progress bar.</param>
        /// <param name="ShowValueText">Show the current value as text on the bar.</param>
        /// <param name="FullWidth">Expand the bar to fill available width.</param>
        /// <param name="ShowValueField">Show an editable value field next to the bar.</param>
        public ProgressBarAttribute(float MinValue, float MaxValue, float BarHeight, string BarText, TinyColor Color = TinyColor.Default, bool ShowValueText = true, bool FullWidth = false, bool ShowValueField = true)
        {
            this.Min = MinValue;
            this.Max = MaxValue;
            this.Height = BarHeight;
            this.BarText = BarText;

            this.FillColor = Color;
            this.ShowValue = ShowValueText;
            this.FullWidth = FullWidth;
            this.ShowValueField = ShowValueField;
        }


    }
}
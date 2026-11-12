using System;

namespace TinyInspector
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ButtonAttribute : Attribute
    {
        public string label;
        public float height = 32f;
        public TinyIcon icon = TinyIcon.None;
        public TinyColor color = TinyColor.Default;

        /// <summary>
        /// Displays a clickable button in the Inspector that invokes the
        /// annotated method directly, without requiring a custom editor.
        /// </summary>
        /// <param name="Label">Custom text displayed on the button.</param>
        /// <param name="Height">Button height in pixels.</param>
        /// <param name="Icon">Optional icon displayed on the button.</param>
        /// <param name="Color">Optional button color override.</param>
        public ButtonAttribute(string Label, float Height = 32, TinyIcon Icon = TinyIcon.None, TinyColor Color = TinyColor.Default)
        {
            label = Label;
            height = Height;
            icon = Icon;
            color = Color;
        }
    }
}

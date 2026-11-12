using UnityEngine;
using System;

namespace TinyInspector
{
    // Lightweight attribute (keeps it in runtime assembly) to show an info box in the inspector.
    // Use like: [InfoBox("Text...")] public int myField;
    public enum InfoBoxType { None = -1, Info = 0, Warning = 1, Error = 2, Success = 3, Test = 4 }

    //[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class InfoBoxAttribute : PropertyAttribute
    {
        public readonly string title = "";
        public readonly string message = "";
        public readonly InfoBoxType type = InfoBoxType.Info;

        /// <summary>
        /// Creates a customizable info box with a title and message,
        /// providing more flexibility than Unity's built-in help boxes.
        /// </summary>
        /// <param name="Title">Title displayed at the top of the info box.</param>
        /// <param name="Type">Visual style of the info box.</param>
        public InfoBoxAttribute(string Title, InfoBoxType Type = InfoBoxType.Info)
        {
            this.title = Title ?? "Title";
            this.type = Type;
        }

        /// <summary>
        /// Creates a customizable info box with a title and message,
        /// providing more flexibility than Unity's built-in help boxes.
        /// </summary>
        /// <param name="Title">Title displayed at the top of the info box.</param>
        /// <param name="Message">Main message content of the info box.</param>
        /// <param name="Type">Visual style of the info box.</param>
        public InfoBoxAttribute(string Title, string Message, InfoBoxType Type = InfoBoxType.Info)
        {
            this.title = Title ?? "Title";
            this.message = Message ?? string.Empty;
            this.type = Type;
        }

        /// <summary>
        /// Creates a customizable info box with a title and message,
        /// providing more flexibility than Unity's built-in help boxes.
        /// </summary>
        /// <param name="Title">Title displayed at the top of the info box.</param>
        public InfoBoxAttribute(string Title)
        {
            this.title = Title ?? "Title";
        }

        /// <summary>
        /// Creates a customizable info box with a title and message,
        /// providing more flexibility than Unity's built-in help boxes.
        /// </summary>
        /// <param name="Title">Title displayed at the top of the info box.</param>
        /// <param name="Message">Main message content of the info box.</param>
        public InfoBoxAttribute(string Title, string Message)
        {
            this.title = Title ?? "Title";
            this.message = Message ?? string.Empty;
        }
    }
}

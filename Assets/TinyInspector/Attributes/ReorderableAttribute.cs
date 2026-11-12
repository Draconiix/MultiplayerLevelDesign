using System;

namespace TinyInspector
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class ReorderableAttribute : Attribute
    {
        /// <summary>
        /// Renders lists and arrays using a clean, reorderable layout with
        /// drag-and-drop support, improving readability and editing workflow.
        /// </summary>
        public ReorderableAttribute() { }
    }
}

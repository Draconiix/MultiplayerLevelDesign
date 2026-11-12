using System;

namespace TinyInspector
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class TableListAttribute : Attribute
    {
        /// <summary>
        /// Renders lists and arrays as tables in the Inspector, providing
        /// a structured and readable layout for editing structured data.
        /// </summary>
        public TableListAttribute() { }
    }
}

using System;

namespace TinyInspector
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ShowDictionaryDisplayAttribute : Attribute
    {
        // marker attribute - no additional data for now
    }
}

using System;

namespace TinyInspector
{
    // Put attributes that must be available to runtime assemblies in a non-Editor folder.
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class MonoscriptInfoAttribute : Attribute
    {
        public string Url { get; }
        public string Desc { get; }

        /// <summary>
        /// Adds an info section to the script header in the Inspector, allowing you to display
        /// a short description and an optional documentation URL for the MonoBehaviour or ScriptableObject.
        /// </summary>
        /// <param name="Description">Text shown under the script name in the Inspector.</param>
        /// <param name="URLLink">Optional URL opened from the documentation button.</param>
        public MonoscriptInfoAttribute(string Description = null, string URLLink = null)
        {
            Url = URLLink;
            Desc = Description;
        }
    }
}

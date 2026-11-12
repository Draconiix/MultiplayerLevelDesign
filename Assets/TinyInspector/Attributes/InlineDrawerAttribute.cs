using UnityEngine;
namespace TinyInspector
{
    public class InlineDrawerAttribute : PropertyAttribute
    {
        /// <summary>
        /// Forces custom class fields to be rendered inline in the Inspector,
        /// without a foldout and without requiring a custom PropertyDrawer.
        /// </summary>
        public InlineDrawerAttribute()
        {

        }
    }
}
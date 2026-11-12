#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace TinyInspector
{
    [CustomPropertyDrawer(typeof(PropertySpaceAttribute), true)]
    public class PropertySpaceDecoratorDrawer : DecoratorDrawer
    {
        public override float GetHeight()
        {
            var attr = (PropertySpaceAttribute)attribute;
            return attr != null ? attr.Height : base.GetHeight();
        }

        public override void OnGUI(Rect position)
        {
        }
    }
}
#endif

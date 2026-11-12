
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor.UIElements;
#endif

namespace TinyInspector
{
    [CustomPropertyDrawer(typeof(ConditionalAttribute), true)]
    public class ConditionalDrawer : PropertyDrawer
    {
        internal const string InternalConditionInPlayMode = "__TI_INTERNAL_IN_PLAY_MODE";
        internal const string InternalConditionInEditMode = "__TI_INTERNAL_IN_EDIT_MODE";
        internal const string InternalConditionInPrefabStage = "__TI_INTERNAL_IN_PREFAB_STAGE";
#if UNITY_EDITOR
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var attr = (ConditionalAttribute)attribute;

            var root = new VisualElement();
            var field = new TinyPropertyField(property);
            root.Add(field);

            void UpdateState()
            {
                bool condition = IsConditionMet(property, attr);
                bool result = attr.showIfTrue ? condition : !condition;

                switch (attr.action)
                {
                    case ConditionalAction.Show:
                        root.style.display = result
                            ? DisplayStyle.Flex
                            : DisplayStyle.None;
                        break;

                    case ConditionalAction.Enable:
                        field.SetEnabled(result);
                        break;
                }
            }

            // initial
            UpdateState();

            // update on inspector refresh / value change
            root.TrackSerializedObjectValue(property.serializedObject, _ =>
            {
                UpdateState();
            });

            return root;
        }

        private bool IsConditionMet(SerializedProperty property, ConditionalAttribute attr)
        {
            object target = property.serializedObject.targetObject;
            if (target == null || string.IsNullOrEmpty(attr.conditionMember))
                return true;

            // Internal editor-state conditions (no reflection required)
            switch (attr.conditionMember)
            {
                case InternalConditionInPlayMode:
                    return Application.isPlaying;
                case InternalConditionInEditMode:
                    return !Application.isPlaying;
                case InternalConditionInPrefabStage:
                    return IsInPrefabStage();
            }

            var type = target.GetType();
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            // FIELD
            var field = type.GetField(attr.conditionMember, flags);
            if (field != null)
            {
                object value = field.GetValue(target);
                return EvaluateValue(value, attr);
            }

            // PROPERTY
            var prop = type.GetProperty(attr.conditionMember, flags);
            if (prop != null && prop.GetIndexParameters().Length == 0)
            {
                object value = prop.GetValue(target);
                return EvaluateValue(value, attr);
            }

            // METHOD
            var method = type.GetMethod(attr.conditionMember, flags);
            if (method != null && method.GetParameters().Length == 0)
            {
                object value = method.Invoke(target, null);
                return EvaluateValue(value, attr);
            }

            return true;
        }

        private static bool IsInPrefabStage()
        {
#if UNITY_2018_3_OR_NEWER
            var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            return stage != null;
#else
            return false;
#endif
        }

        private bool EvaluateValue(object value, ConditionalAttribute attr)
        {
            if (value == null) return false;

            if (value is bool b)
                return b;

            // Unity object references (GameObject, Transform, Component, ScriptableObject, etc.)
            // should be treated as "true" when assigned.
            if (value is UnityEngine.Object uo)
                return uo != null;

            if (!string.IsNullOrEmpty(attr.conditionValue) && value.GetType().IsEnum)
            {
                // Support Odin-like usage: [ShowIf("EnumField", MyEnum.Value)]
                // and keep string-based usage working.
                try
                {
                    var parsed = System.Enum.Parse(value.GetType(), attr.conditionValue, ignoreCase: true);
                    return value.Equals(parsed);
                }
                catch
                {
                    return string.Equals(value.ToString(), attr.conditionValue, System.StringComparison.Ordinal);
                }
            }

            return true;
        }
#endif
    }
}

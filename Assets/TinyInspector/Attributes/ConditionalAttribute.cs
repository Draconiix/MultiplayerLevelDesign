using System;
using UnityEngine;

namespace TinyInspector
{
    // Base attribute representing a conditional behavior for drawing inspector fields.
    public enum ConditionalAction
    {
        Show, // Show or hide the field
        Enable // Enable or disable the field
    }

    public class ConditionalAttribute : PropertyAttribute
    {
        public readonly string conditionMember;
        public readonly string conditionValue;
        public readonly bool showIfTrue;
        public readonly ConditionalAction action;

        public ConditionalAttribute(string conditionMember, ConditionalAction action, string conditionValue = null, bool showIfTrue = true)
        {
            this.conditionMember = conditionMember;
            this.conditionValue = conditionValue;
            this.showIfTrue = showIfTrue;
            this.action = action;
        }

        public ConditionalAttribute(string conditionMember, ConditionalAction action, object conditionValue, bool showIfTrue = true)
            : this(conditionMember, action, conditionValue?.ToString(), showIfTrue)
        {
        }
    }






    public class ShowIfAttribute : ConditionalAttribute
    {
        public ShowIfAttribute(string conditionMember, bool showIfTrue = true) : base(conditionMember, ConditionalAction.Show, null, showIfTrue) { }
        public ShowIfAttribute(string conditionMember, string conditionValue, bool showIfTrue = true) : base(conditionMember, ConditionalAction.Show, conditionValue, showIfTrue) { }
        public ShowIfAttribute(string conditionMember, object conditionValue, bool showIfTrue = true) : base(conditionMember, ConditionalAction.Show, conditionValue, showIfTrue) { }
    }

    // Convenience derived attributes
    public class HideIfAttribute : ConditionalAttribute
    {
        public HideIfAttribute(string conditionMember) : base(conditionMember, ConditionalAction.Show, null, false) { }
        public HideIfAttribute(string conditionMember, string conditionValue) : base(conditionMember, ConditionalAction.Show, conditionValue, false) { }
        public HideIfAttribute(string conditionMember, object conditionValue) : base(conditionMember, ConditionalAction.Show, conditionValue, false) { }
    }

    public class EnableIfAttribute : ConditionalAttribute
    {
        public EnableIfAttribute(string conditionMember) : base(conditionMember, ConditionalAction.Enable, null, true) { }
        public EnableIfAttribute(string conditionMember, string conditionValue) : base(conditionMember, ConditionalAction.Enable, conditionValue, true) { }
        public EnableIfAttribute(string conditionMember, object conditionValue) : base(conditionMember, ConditionalAction.Enable, conditionValue, true) { }
    }

    public class DisableIfAttribute : ConditionalAttribute
    {
        public DisableIfAttribute(string conditionMember) : base(conditionMember, ConditionalAction.Enable, null, false) { }
        public DisableIfAttribute(string conditionMember, string conditionValue) : base(conditionMember, ConditionalAction.Enable, conditionValue, false) { }
        public DisableIfAttribute(string conditionMember, object conditionValue) : base(conditionMember, ConditionalAction.Enable, conditionValue, false) { }
    }

    // Editor-state convenience attributes (no explicit condition member required by the user)
    // These are evaluated by ConditionalDrawer using internal condition member names.
    public class ShowInPlayModeAttribute : ConditionalAttribute
    {
        public ShowInPlayModeAttribute() : base("__TI_INTERNAL_IN_PLAY_MODE", ConditionalAction.Show, null, true) { }
    }

    public class HideInPlayModeAttribute : ConditionalAttribute
    {
        public HideInPlayModeAttribute() : base("__TI_INTERNAL_IN_PLAY_MODE", ConditionalAction.Show, null, false) { }
    }

    public class EnableInPlayModeAttribute : ConditionalAttribute
    {
        public EnableInPlayModeAttribute() : base("__TI_INTERNAL_IN_PLAY_MODE", ConditionalAction.Enable, null, true) { }
    }

    public class DisableInPlayModeAttribute : ConditionalAttribute
    {
        public DisableInPlayModeAttribute() : base("__TI_INTERNAL_IN_PLAY_MODE", ConditionalAction.Enable, null, false) { }
    }

    public class ShowInEditModeAttribute : ConditionalAttribute
    {
        public ShowInEditModeAttribute() : base("__TI_INTERNAL_IN_EDIT_MODE", ConditionalAction.Show, null, true) { }
    }

    public class HideInEditModeAttribute : ConditionalAttribute
    {
        public HideInEditModeAttribute() : base("__TI_INTERNAL_IN_EDIT_MODE", ConditionalAction.Show, null, false) { }
    }

    public class EnableInEditModeAttribute : ConditionalAttribute
    {
        public EnableInEditModeAttribute() : base("__TI_INTERNAL_IN_EDIT_MODE", ConditionalAction.Enable, null, true) { }
    }

    public class DisableInEditModeAttribute : ConditionalAttribute
    {
        public DisableInEditModeAttribute() : base("__TI_INTERNAL_IN_EDIT_MODE", ConditionalAction.Enable, null, false) { }
    }

    public class ShowInPrefabAttribute : ConditionalAttribute
    {
        public ShowInPrefabAttribute() : base("__TI_INTERNAL_IN_PREFAB_STAGE", ConditionalAction.Show, null, true) { }
    }

    public class HideInPrefabAttribute : ConditionalAttribute
    {
        public HideInPrefabAttribute() : base("__TI_INTERNAL_IN_PREFAB_STAGE", ConditionalAction.Show, null, false) { }
    }

    public class EnableInPrefabAttribute : ConditionalAttribute
    {
        public EnableInPrefabAttribute() : base("__TI_INTERNAL_IN_PREFAB_STAGE", ConditionalAction.Enable, null, true) { }
    }

    public class DisableInPrefabAttribute : ConditionalAttribute
    {
        public DisableInPrefabAttribute() : base("__TI_INTERNAL_IN_PREFAB_STAGE", ConditionalAction.Enable, null, false) { }
    }
}
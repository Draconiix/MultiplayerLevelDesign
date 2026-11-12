#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyInspector
{
    public class TinyPropertyField : VisualElement
    {
        private readonly PropertyField propertyField;

        public TinyPropertyField(
            UnityEditor.SerializedProperty property,
            string labelText = null,
            Texture2D labelIcon = null,
            TinyColor color = TinyColor.Default)
        {
            style.flexDirection = FlexDirection.Column;

            // Keep Unity's PropertyField label column behavior, but replace the label text
            // and optionally inject an icon into the label container.
            propertyField = new PropertyField(property);

            if (!string.IsNullOrEmpty(labelText))
                propertyField.label = labelText;

            propertyField.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                InjectIconIntoUnityLabel(propertyField, labelIcon);
            });

            Add(propertyField);
        }

        private static void InjectIconIntoUnityLabel(PropertyField field, Texture2D icon)
        {
            if (field == null)
                return;

            var unityLabel = field.Q<Label>(className: "unity-base-field__label");
            if (unityLabel == null)
                return;

            var existing = unityLabel.Q<Image>(name: "TinyInspectorLabelIcon");

            if (icon == null)
            {
                existing?.RemoveFromHierarchy();
                unityLabel.style.marginLeft = StyleKeyword.Null;
                return;
            }

            // Requested: when there is an icon, shift label content to the right.
            unityLabel.style.paddingLeft = 19;

            if (existing != null)
            {
                existing.image = icon;
                return;
            }

            var image = new VisualElement
            {
                name = "TinyInspectorLabelIcon",
                pickingMode = PickingMode.Ignore

            };

            image.style.backgroundImage = icon;
            image.style.position = Position.Absolute;
            image.style.left = 0;
            image.style.top = 2;
            image.style.bottom = 0;
            image.style.width = 16;
            image.style.height = 16;
            image.style.alignSelf = Align.Center;
            image.style.flexShrink = 0;
            image.style.unityBackgroundImageTintColor = TinyInspectorStyles.LabelColor;

            unityLabel.style.position = Position.Relative;
            unityLabel.Add(image);
        }
    }

    public class TinyLabel : VisualElement
    {
        private const float DefaultLabelMinWidth = 123f;
        private const float DefaultOffset = 37f;

        private const string UnityPropertyFieldUssClassName = "unity-property-field";
        private const string UnityPropertyFieldInspectorUssClassName = "unity-property-field__inspector-property";
        private const string UnityInspectorElementUssClassName = "unity-inspector-element";
        private const string UnityInspectorMainContainerClassName = "unity-inspector-main-container";

        private VisualElement contextWidthElement;
        private VisualElement inspectorElement;
        private VisualElement childFieldElement;

        private IVisualElementScheduledItem widthUpdateItem;
        private float lastContextWidth = -1f;
        private float lastComputedWidth = -1f;

        public TinyLabel(
            string text,
            Texture2D icon = null,
            TinyColor color = TinyColor.Default)
        {
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.overflow = Overflow.Hidden;
            style.marginRight = -1;

            tooltip = text;

            if (icon != null)
            {
                var image = new Image
                {
                    image = icon,
                    scaleMode = ScaleMode.ScaleToFit
                };

                image.style.width = 16;
                image.style.height = 16;
                image.style.minWidth = 16;
                image.style.minHeight = 16;
                image.style.marginRight = 2;

                Add(image);
            }

            if (!string.IsNullOrEmpty(text))
            {
                var label = new Label(text);
                label.style.flexGrow = 1;
                label.style.minWidth = 0;
                label.style.whiteSpace = WhiteSpace.NoWrap;
                label.style.overflow = Overflow.Hidden;
                label.style.textOverflow = TextOverflow.Ellipsis;
                Add(label);
            }

            AddToClassList("unity-text-element");
            AddToClassList("unity-label");
            AddToClassList("unity-base-field__label");
            AddToClassList("unity-property-field__label");

            

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                UpdateInspectorRefs();
                schedule.Execute(ApplyComputedWidth);
                StartWidthUpdater();
            });

            RegisterCallback<DetachFromPanelEvent>(_ => StopWidthUpdater());

            RegisterCallback<GeometryChangedEvent>(_ =>
            {
                // On resize, the visual tree can be rebuilt/reflowed; refresh references.
                UpdateInspectorRefs();
                ApplyComputedWidth();
                // One more pass next tick to match Unity's timing and resolvedStyle updates.
                schedule.Execute(ApplyComputedWidth);
            });
        }

        private void UpdateInspectorRefs()
        {
            inspectorElement = GetFirstAncestorWithClass(this, UnityInspectorElementUssClassName) ?? parent;
            contextWidthElement = GetFirstAncestorWithClass(this, UnityInspectorMainContainerClassName) ?? inspectorElement;

            // Unity computes `num` from the IMGUI-child-field x-position relative to inspector element.
            // For our UIE-only label, we approximate that using the closest property-field row container.
            childFieldElement = GetFirstAncestorWithClass(this, UnityPropertyFieldUssClassName) ?? parent;
        }

        private void StartWidthUpdater()
        {
            StopWidthUpdater();

            // UI Toolkit doesn't update like IMGUI per-frame for label widths.
            // Keep a lightweight periodic updater while attached to follow inspector resizing.
            widthUpdateItem = schedule.Execute(() =>
            {
                if (panel == null)
                    return;

                var baseElement = inspectorElement ?? parent;
                var widthElement = contextWidthElement ?? baseElement;
                if (baseElement == null || widthElement == null)
                    return;

                // Only update when the driving width changes.
                var w = widthElement.resolvedStyle.width;
                if (Mathf.Approximately(w, lastContextWidth))
                    return;

                lastContextWidth = w;

                UpdateInspectorRefs();
                ApplyComputedWidth();
            }).Every(16);
        }

        private void StopWidthUpdater()
        {
            widthUpdateItem?.Pause();
            widthUpdateItem = null;
            lastContextWidth = -1f;
            lastComputedWidth = -1f;
        }

        private void ApplyComputedWidth()
        {
            if (panel == null)
                return;

            if (inspectorElement == null)
                UpdateInspectorRefs();

            var baseElement = inspectorElement ?? parent;
            var widthElement = contextWidthElement ?? baseElement;
            if (baseElement == null || widthElement == null)
                return;

            // Match Unity behavior: only apply this calculation when used as an inspector property row.
            var inInspectorPropertyRow = ClassListContains(UnityPropertyFieldInspectorUssClassName) || GetFirstAncestorWithClass(this, UnityPropertyFieldInspectorUssClassName) != null;
            if (!inInspectorPropertyRow)
            {
                style.width = StyleKeyword.Null;
                return;
            }

            float num = 0f;
            if (childFieldElement != null)
            {
                num = childFieldElement.worldBound.x - baseElement.worldBound.x - baseElement.resolvedStyle.paddingLeft;
            }

            float width = widthElement.resolvedStyle.width;
            float a = width * 0.45f - DefaultOffset - num;
            float b = Mathf.Max(DefaultLabelMinWidth - num, 0f);
            float computed = Mathf.Max(a, b);

            if (float.IsNaN(computed) || computed < 0f)
                return;

            computed = Mathf.Round(computed);

            if (Mathf.Approximately(computed, lastComputedWidth))
                return;

            lastComputedWidth = computed;
            style.width = computed;
        }

        private static VisualElement GetFirstAncestorWithClass(VisualElement element, string className)
        {
            for (var p = element?.parent; p != null; p = p.parent)
            {
                if (p.ClassListContains(className))
                    return p;
            }

            return null;
        }
    }
}

#endif
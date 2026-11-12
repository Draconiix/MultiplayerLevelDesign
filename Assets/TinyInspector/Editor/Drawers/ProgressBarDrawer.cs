using System.Reflection;
using UnityEditor;

using UnityEngine;
using UnityEngine.UIElements;

namespace TinyInspector
{
    [CustomPropertyDrawer(typeof(ProgressBarAttribute))]
    public class ProgressBarDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var attr = (ProgressBarAttribute)attribute;

            // Root container (horizontal)
            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;
            root.style.alignItems = Align.Center;
            root.style.marginLeft = 2;

            string BarText = string.IsNullOrEmpty(attr.BarText) ? string.Empty : attr.BarText + " ";

            // Force Hide & Custom Label support
            var hideLabel = fieldInfo.GetCustomAttributes(typeof(HideLabelAttribute), true);
            var customlabel = fieldInfo.GetCustomAttribute<CustomLabelAttribute>();
            var tinyLabel = customlabel != null
                ? new TinyLabel(customlabel.Label, icon: (Texture2D)TinyIcons.GetIcon(customlabel.Icon))
                : new TinyLabel(property.displayName);

            tinyLabel.style.marginRight = 1;

            if (hideLabel.Length == 0 && !attr.FullWidth) root.Add(tinyLabel);

            // Bar container
            var barContainer = new VisualElement();
            barContainer.style.flexGrow = 1;
            barContainer.style.height = EditorGUIUtility.singleLineHeight;
            barContainer.style.backgroundColor = EditorGUIUtility.isProSkin ? new Color(0.16f, 0.16f, 0.16f) : new Color(0.9f, 0.9f, 0.9f);
            barContainer.style.position = Position.Relative;
            barContainer.style.borderBottomLeftRadius = 3;
            barContainer.style.borderBottomRightRadius = 3;
            barContainer.style.borderTopLeftRadius = 3;
            barContainer.style.borderTopRightRadius = 3;
            barContainer.style.height = attr.Height > 16 ? attr.Height : 16;
            
            barContainer.AddToClassList("tinyinspector-progressbar");

            var fill = new VisualElement();
            fill.style.height = Length.Percent(100);
            fill.style.width = Length.Percent(0);
            var fillColor = TinyInspectorStyles.Instance.GetAccentColor(attr.FillColor);
            fill.style.backgroundColor = fillColor;
            fill.style.position = Position.Absolute;
            fill.style.left = 0;
            fill.style.top = 0;
            fill.style.bottom = 0;
            fill.style.borderBottomLeftRadius = 3;
            fill.style.borderBottomRightRadius = 3;
            fill.style.borderTopLeftRadius = 3;
            fill.style.borderTopRightRadius = 3;
            barContainer.Add(fill);

            var barLabel = new Label("");
            barLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            barLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            barLabel.style.color = TinyInspectorStyles.Instance.GetAccentTextColor(attr.FillColor);
            barLabel.style.position = Position.Absolute;
            barLabel.style.left = 0;
            barLabel.style.right = 0;
            barLabel.style.top = 0;
            barLabel.style.bottom = 0;
            barLabel.style.fontSize = 10;
            // Always show the label; when ShowValue is false we'll hide only the numeric portion
            barContainer.Add(barLabel);

            root.Add(barContainer);

            // Small numeric field
            VisualElement numberField;
            if (property.propertyType == SerializedPropertyType.Float)
            {
                var f = new FloatField();
                f.formatString = "0.##";
                numberField = f;
            }
            else
            {
                var i = new IntegerField();
                numberField = i;
            }

            numberField.style.width = 60;
            numberField.style.marginLeft = 4;
            numberField.style.height = attr.Height > 16 ? attr.Height : 16;
            if(attr.ShowValueField) root.Add(numberField);

            bool isDragging = false;
            Vector2 dragStart = Vector2.zero;
            float dragStartValue = 0f;

            // Pointer down on label starts dragging
            tinyLabel.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != (int)MouseButton.LeftMouse) return;
                isDragging = true;
                dragStart = evt.position;
                if (property.propertyType == SerializedPropertyType.Float) dragStartValue = property.floatValue;
                else dragStartValue = property.intValue;
                tinyLabel.CaptureMouse();
                evt.StopImmediatePropagation();
            });

            // Pointer move updates value while dragging
            tinyLabel.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!isDragging) return;

                // compute drag delta relative to available width (root width)
                float delta = evt.position.x - dragStart.x;
                float dragArea = root.layout.width;
                if (dragArea <= 0) dragArea = barContainer.layout.width + tinyLabel.layout.width;
                if (dragArea <= 0) dragArea = 200f; // fallback

                float ratio = delta / dragArea;
                float range = attr.Max - attr.Min;
                float newValFloat = dragStartValue + ratio * range;
                newValFloat = Mathf.Clamp(newValFloat, attr.Min, attr.Max);

                property.serializedObject.Update();
                if (property.propertyType == SerializedPropertyType.Float)
                {
                    property.floatValue = newValFloat;
                }
                else
                {
                    property.intValue = Mathf.RoundToInt(newValFloat);
                }
                property.serializedObject.ApplyModifiedProperties();

                // update visuals
                float normalized = (attr.Max - attr.Min) != 0 ? Mathf.InverseLerp(attr.Min, attr.Max, property.propertyType == SerializedPropertyType.Float ? property.floatValue : property.intValue) : 0f;
                fill.style.width = Length.Percent(normalized * 100f);
                // show bar text always; include numeric value only when requested
                if (property.propertyType == SerializedPropertyType.Float)
                    barLabel.text = attr.ShowValue ? $"{BarText}[{property.floatValue:0.##} / {attr.Max:0.##}]" : BarText.TrimEnd();
                else
                    barLabel.text = attr.ShowValue ? $"{BarText}[{property.intValue} / {(int)attr.Max}]" : BarText.TrimEnd();

                if (numberField is FloatField ff) ff.SetValueWithoutNotify(property.propertyType == SerializedPropertyType.Float ? property.floatValue : (float)property.intValue);
                if (numberField is IntegerField ii) ii.SetValueWithoutNotify(property.intValue);

                evt.StopImmediatePropagation();
            });

            // Pointer up ends dragging
            tinyLabel.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (!isDragging) return;
                isDragging = false;
                tinyLabel.ReleaseMouse();
                evt.StopImmediatePropagation();
            });

            // Also cancel on mouse leave (in case)
            tinyLabel.RegisterCallback<PointerOutEvent>(evt =>
            {
                if (!isDragging) return;
                // optional: end drag when pointer leaves label
                // keep dragging so user can move outside label � so do nothing here
            });

            // Initialize values once attached
            root.RegisterCallback<AttachToPanelEvent>(evt =>
            {
                property.serializedObject.Update();
                float val = property.propertyType == SerializedPropertyType.Float ? property.floatValue : property.intValue;
                float min = attr.Min;
                float max = attr.Max;
                float normalized = (max - min) != 0 ? Mathf.InverseLerp(min, max, val) : 0f;

                fill.style.width = Length.Percent(normalized * 100f);
                // show bar text always; include numeric value only when requested
                if (property.propertyType == SerializedPropertyType.Float)
                    barLabel.text = attr.ShowValue ? $"{BarText}[{val:0.##} / {max:0.##}]" : BarText.TrimEnd();
                else
                    barLabel.text = attr.ShowValue ? $"{BarText}[{(int)val} / {(int)max}]" : BarText.TrimEnd();

                if (numberField is FloatField ff) ff.SetValueWithoutNotify((float)val);
                if (numberField is IntegerField ii) ii.SetValueWithoutNotify((int)val);

                fill.style.backgroundColor = TinyInspectorStyles.Instance.GetAccentColor(attr.FillColor);
            });

            // Handle numeric changes
            if (numberField is FloatField nf)
            {
                nf.RegisterValueChangedCallback(evt =>
                {
                    property.serializedObject.Update();
                    float newVal = Mathf.Clamp(evt.newValue, attr.Min, attr.Max);
                    property.floatValue = newVal;
                    property.serializedObject.ApplyModifiedProperties();

                    float normalized = (attr.Max - attr.Min) != 0 ? Mathf.InverseLerp(attr.Min, attr.Max, newVal) : 0f;
                    fill.style.width = Length.Percent(normalized * 100f);
                    barLabel.text = attr.ShowValue ? $"{BarText}[{newVal:0.##} / {attr.Max:0.##}]" : BarText.TrimEnd();
                });
            }
            else if (numberField is IntegerField ni)
            {
                ni.RegisterValueChangedCallback(evt =>
                {
                    property.serializedObject.Update();
                    int newVal = Mathf.Clamp(evt.newValue, (int)attr.Min, (int)attr.Max);
                    property.intValue = newVal;
                    property.serializedObject.ApplyModifiedProperties();

                    float normalized = (attr.Max - attr.Min) != 0 ? Mathf.InverseLerp(attr.Min, attr.Max, newVal) : 0f;
                    fill.style.width = Length.Percent(normalized * 100f);
                    barLabel.text = attr.ShowValue ? $"{BarText}[{newVal} / {(int)attr.Max}]" : BarText.TrimEnd();
                });
            }

            return root;
        }

    }
}

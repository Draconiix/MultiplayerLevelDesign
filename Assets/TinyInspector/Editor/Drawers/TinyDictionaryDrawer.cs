using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using TinyInspector;

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(TinyDictionary<,>), true)]
public class TinyDictionaryDrawer : PropertyDrawer
{
    private static readonly HashSet<string> foldoutStates = new HashSet<string>();

    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var root = new VisualElement();

        var keysProp = property.FindPropertyRelative("_keys");
        var valuesProp = property.FindPropertyRelative("_values");

        if (keysProp == null || valuesProp == null)
        {
            root.Add(new Label("Invalid TinyDictionary (missing _keys/_values)."));
            return root;
        }

        var header = new VisualElement();
        header.AddToClassList("BoxHeader");
        header.style.flexDirection = FlexDirection.Row;
        header.style.alignItems = Align.Center;

        var title = new Label(property.displayName) { style = { unityFontStyleAndWeight = FontStyle.Bold } };
        title.style.flexGrow = 1;
        title.style.marginLeft = 4;

        var count = new Label($"COUNT: {Math.Min(keysProp.arraySize, valuesProp.arraySize)}");
        count.AddToClassList("FoldoutCountLabel");

        var chevron = new Image();
        chevron.AddToClassList("BoxFoldoutIcon");
        chevron.style.borderLeftWidth = 0;
        chevron.style.borderRightWidth = 1;

        var path = property.propertyPath;
        var expanded = foldoutStates.Contains(path);
        chevron.image = TinyIcons.GetIcon(expanded ? TinyIcon.ChevronUp : TinyIcon.ChevronDown);

        var container = new VisualElement();
        container.style.flexDirection = FlexDirection.Column;
        container.AddToClassList("reorderableContainer");
        container.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;

        // Determine key type from the generic TinyDictionary field when possible
        Type keyType = null;
        try
        {
            var t = fieldInfo.FieldType;
            if (t.IsGenericType)
                keyType = t.GetGenericArguments()[0];
        }
        catch { keyType = null; }

        Button headerAddBtn = null;

        void Rebuild()
        {
            container.Clear();
            property.serializedObject.Update();

            // Header row for columns
            var HeaderRow = new VisualElement();
            HeaderRow.style.flexDirection = FlexDirection.Row;
            HeaderRow.style.alignItems = Align.Center;
            HeaderRow.AddToClassList("TableListHeader");
            container.Add(HeaderRow);

            // Spacer for handle
            var handleSpacer = new VisualElement();
            handleSpacer.style.width = 26;
            handleSpacer.style.height = 25;
            handleSpacer.style.flexShrink = 0;
            handleSpacer.AddToClassList("TableListBorderHelper");
            HeaderRow.Add(handleSpacer);

            // Value column header (left)
            var valHeader = new Label("Value");
            valHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            valHeader.style.flexGrow = 1;
            valHeader.style.flexShrink = 1;
            valHeader.style.flexBasis = new StyleLength(new Length(0f));
            valHeader.style.marginLeft = 4;
            valHeader.style.unityTextAlign = TextAnchor.MiddleCenter;
            valHeader.style.height = 25;
            valHeader.style.opacity = .7f;
            valHeader.AddToClassList("TableListBorderHelper");
            HeaderRow.Add(valHeader);

            // Key column header (right)
            var keyHeader = new Label("Key");
            keyHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            keyHeader.style.flexGrow = 1;
            keyHeader.style.flexShrink = 1;
            keyHeader.style.flexBasis = new StyleLength(new Length(0f));
            keyHeader.style.marginLeft = 4;
            keyHeader.style.unityTextAlign = TextAnchor.MiddleCenter;
            keyHeader.style.height = 25;
            keyHeader.style.opacity = .7f;
            keyHeader.AddToClassList("TableListBorderHelper");
            HeaderRow.Add(keyHeader);

            // Delete spacer
            var deleteSpacer = new VisualElement();
            deleteSpacer.style.width = 26;
            deleteSpacer.style.height = 25;
            deleteSpacer.style.flexShrink = 0;
            HeaderRow.Add(deleteSpacer);

            var countItems = Math.Min(keysProp.arraySize, valuesProp.arraySize);

            for (var i = 0; i < countItems; i++)
            {
                var idx = i;
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.AddToClassList("ReorderableRow");

                // Handle (up/down)
                var handle = new VisualElement();
                handle.style.width = 26;
                handle.style.height = 25;
                handle.style.flexShrink = 0;
                handle.AddToClassList("DragHandle");

                var upBtn = new Button(() =>
                {
                    if (idx <= 0) return;
                    property.serializedObject.Update();
                    var k = property.FindPropertyRelative("_keys");
                    var v = property.FindPropertyRelative("_values");
                    k.MoveArrayElement(idx, idx - 1);
                    v.MoveArrayElement(idx, idx - 1);
                    property.serializedObject.ApplyModifiedProperties();
                    Rebuild();
                }) { text = "▲" };
                upBtn.AddToClassList("SmallButton");
                handle.Add(upBtn);

                var downBtn = new Button(() =>
                {
                    if (idx >= countItems - 1) return;
                    property.serializedObject.Update();
                    var k = property.FindPropertyRelative("_keys");
                    var v = property.FindPropertyRelative("_values");
                    k.MoveArrayElement(idx, idx + 1);
                    v.MoveArrayElement(idx, idx + 1);
                    property.serializedObject.ApplyModifiedProperties();
                    Rebuild();
                }) { text = "▼" };
                downBtn.AddToClassList("SmallButton");
                handle.Add(downBtn);

                row.Add(handle);

                // Content: Value (left) then Key (right)
                var content = new VisualElement();
                content.style.flexDirection = FlexDirection.Row;
                content.style.flexGrow = 1;

                var valProp = valuesProp.GetArrayElementAtIndex(i);
                var keyProp = keysProp.GetArrayElementAtIndex(i);

                var valField = new PropertyField(valProp);
                valField.AddToClassList("ReorderablePropertyField");
                valField.label = string.Empty;
                valField.style.flexGrow = 1;
                valField.style.flexShrink = 1;
                valField.style.flexBasis = new StyleLength(new Length(0f));
                try { valField.Bind(property.serializedObject); } catch { }
                HidePropertyFieldLabel(valField);
                content.Add(valField);

                var keyField = new PropertyField(keyProp);
                keyField.AddToClassList("ReorderablePropertyField");
                keyField.label = string.Empty;
                keyField.style.flexGrow = 1;
                keyField.style.flexShrink = 1;
                keyField.style.flexBasis = new StyleLength(new Length(0f));
                try { keyField.Bind(property.serializedObject); } catch { }
                HidePropertyFieldLabel(keyField);
                content.Add(keyField);

                row.Add(content);

                // Delete button
                var btns = new VisualElement();
                btns.style.flexDirection = FlexDirection.Row;
                btns.style.alignItems = Align.Center;
                btns.style.flexShrink = 0;

                var deleteBtn = new Button(() =>
                {
                    property.serializedObject.Update();
                    var k = property.FindPropertyRelative("_keys");
                    var v = property.FindPropertyRelative("_values");
                    k.DeleteArrayElementAtIndex(idx);
                    v.DeleteArrayElementAtIndex(idx);
                    property.serializedObject.ApplyModifiedProperties();
                    Rebuild();
                    count.text = $"COUNT: {Math.Min(k.arraySize, v.arraySize)}";
                }) { text = "" };
                deleteBtn.AddToClassList("ReorderDeleteButton");
                deleteBtn.tooltip = "Delete";
                deleteBtn.style.width = 26;

                var delTex = TinyIcons.GetIcon(TinyIcon.Delete) as Texture2D;
                if (delTex != null)
                {
                    var delImg = new Image { image = delTex };
                    delImg.style.width = 20;
                    delImg.style.height = 20;
                    deleteBtn.Add(delImg);
                }

                btns.Add(deleteBtn);
                row.Add(btns);

                container.Add(row);
            }

            // No restrictions on adding entries
        }

        // Add button
        headerAddBtn = new Button(() =>
        {
            property.serializedObject.Update();
            var k = property.FindPropertyRelative("_keys");
            var v = property.FindPropertyRelative("_values");

            // Do not prefill keys; user requested empty/default keys

            var idx = Mathf.Max(k.arraySize, v.arraySize);
            k.InsertArrayElementAtIndex(idx);
            v.InsertArrayElementAtIndex(idx);
            // Newly inserted elements
            var newK = k.GetArrayElementAtIndex(idx);
            var newV = v.GetArrayElementAtIndex(idx);
            ClearSerializedProperty(newV);

            // Initialize key as default/empty
            ClearSerializedProperty(newK);

            property.serializedObject.ApplyModifiedProperties();
            Rebuild();
            count.text = $"COUNT: {Math.Min(k.arraySize, v.arraySize)}";
        }) { text = "" };
        headerAddBtn.AddToClassList("FoldoutAddButton");
        headerAddBtn.tooltip = "Add";
        var addTex = TinyIcons.GetIcon(TinyIcon.Add) as Texture2D;
        if (addTex != null)
        {
            var addImg = new Image { image = addTex };
            addImg.style.width = 20;
            addImg.style.height = 20;
            headerAddBtn.Add(addImg);
        }

        header.Add(chevron);
        header.Add(title);
        header.Add(count);
        header.Add(headerAddBtn);

        header.AddManipulator(new Clickable(() =>
        {
            var cur = !foldoutStates.Contains(path);
            if (cur) foldoutStates.Add(path); else foldoutStates.Remove(path);
            container.style.display = cur ? DisplayStyle.Flex : DisplayStyle.None;
            var tx = TinyIcons.GetIcon(cur ? TinyIcon.ChevronUp : TinyIcon.ChevronDown);
            if (tx != null) chevron.image = tx;
        }));

        root.Add(header);
        root.Add(container);

        Rebuild();
        return root;
    }

    private static void HidePropertyFieldLabel(PropertyField pf)
    {
        if (pf == null) return;
        pf.RegisterCallback<GeometryChangedEvent>(_ =>
        {
            var labelEl = pf.Q<Label>(className: "unity-property-field__label");
            if (labelEl != null)
                labelEl.style.display = DisplayStyle.None;
        });
    }

    private static void ClearSerializedProperty(SerializedProperty prop)
    {
        if (prop == null) return;
        switch (prop.propertyType)
        {
            case SerializedPropertyType.Integer: prop.intValue = 0; break;
            case SerializedPropertyType.Boolean: prop.boolValue = false; break;
            case SerializedPropertyType.Float: prop.floatValue = 0f; break;
            case SerializedPropertyType.String: prop.stringValue = string.Empty; break;
            case SerializedPropertyType.ObjectReference: prop.objectReferenceValue = null; break;
            case SerializedPropertyType.LayerMask: prop.intValue = 0; break;
            case SerializedPropertyType.Enum: prop.enumValueIndex = 0; break;
            case SerializedPropertyType.Vector2: prop.vector2Value = default; break;
            case SerializedPropertyType.Vector3: prop.vector3Value = default; break;
            case SerializedPropertyType.Vector4: prop.vector4Value = default; break;
            case SerializedPropertyType.Color: prop.colorValue = default; break;
            case SerializedPropertyType.Rect: prop.rectValue = default; break;
            case SerializedPropertyType.ArraySize: break;
            case SerializedPropertyType.Generic: break;
            default: break;
        }
    }
}
#endif

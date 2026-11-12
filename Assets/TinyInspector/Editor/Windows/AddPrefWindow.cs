using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
public class AddPrefWindow : EditorWindow
{
    private TextField _keyField;
    private TextField _valueField;
    private PopupField<string> _typeField;
    private Action _onAdded;

    public static void Show(Action onAdded)
    {
        var w = GetWindow<AddPrefWindow>(true, "Add New Player Prefs");
        w._onAdded = onAdded;
        w.minSize = new Vector2(  400, 100);
        w.maxSize = new Vector2( 400, 100);
    }

    private void CreateGUI()
    {
        var root = rootVisualElement;
        root.style.paddingLeft = 6;
        root.style.paddingTop = 6;

        _keyField = new TextField("Key");
        root.Add(_keyField);

        _typeField = new PopupField<string>(new List<string> { "String", "Int", "Float" }, 0)
        {
            label = "Type"
        };
        root.Add(_typeField);

        _valueField = new TextField("Value");
        root.Add(_valueField);

        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.marginTop = 6;

        var addBtn = new Button(() => OnAdd()) { text = "Add" };
        addBtn.style.marginRight = 6;
        row.Add(addBtn);

        var cancelBtn = new Button(() => Close()) { text = "Cancel" };
        row.Add(cancelBtn);

        root.Add(row);
    }

    private void OnAdd()
    {
        var key = _keyField.value?.Trim();
        if (string.IsNullOrEmpty(key))
        {
            EditorUtility.DisplayDialog("Add PlayerPref", "Key cannot be empty.", "OK");
            return;
        }

        var type = _typeField.value;
        var val = _valueField.value ?? string.Empty;

        try
        {
            switch (type)
            {
                case "String":
                    PlayerPrefs.SetString(key, val);
                    break;
                case "Int":
                    if (!int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                    {
                        EditorUtility.DisplayDialog("Add PlayerPref", "Invalid int value.", "OK");
                        return;
                    }
                    PlayerPrefs.SetInt(key, i);
                    break;
                case "Float":
                    if (!float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
                    {
                        EditorUtility.DisplayDialog("Add PlayerPref", "Invalid float value.", "OK");
                        return;
                    }
                    PlayerPrefs.SetFloat(key, f);
                    break;
            }

            PlayerPrefs.Save();
            _onAdded?.Invoke();
            Close();
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("Add PlayerPref", $"Error: {ex.Message}", "OK");
        }
    }
}
#endif

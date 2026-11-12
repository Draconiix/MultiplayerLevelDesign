#if UNITY_EDITOR

using System;
using System.Numerics;
using System.Reflection;
using TinyInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public static class TinyBigIntegerDrawer
{
    public static VisualElement Create(
        FieldInfo field,
        UnityEngine.Object target)
    {
        var value = (BigInteger)field.GetValue(target);

        var root = new VisualElement();
        root.style.flexDirection = FlexDirection.Row;
        root.AddToClassList("unity-property-field__inspector-property");


        var label = new TinyLabel(field.Name);
        root.Add(label);




        var textField = new TextField()
        {
            value = value.ToString()
        };
        textField.style.flexGrow = 1;

        textField.isDelayed = true; // commit po Enter / focus out

        textField.RegisterValueChangedCallback(evt =>
        {
            if (!BigInteger.TryParse(evt.newValue, out var parsed))
            {
                //label.
                return;
            }

            textField.RemoveFromClassList("unity-invalid");

            Undo.RecordObject(target, field.Name);
            field.SetValue(target, parsed);
            EditorUtility.SetDirty(target);
        });

        root.Add(textField);
        return root;
    }
}

public static class TinyGuidDrawer
{
    public static VisualElement Create(FieldInfo field, UnityEngine.Object target)
    {
        var value = (Guid)field.GetValue(target);

        var root = new VisualElement();
        root.style.flexDirection = FlexDirection.Row;
        root.AddToClassList("unity-property-field__inspector-property");

        root.Add(new TinyLabel(field.Name));

        //var Warning = new Label("Guid format: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx");
        //root.Add(Warning);



        var textField = new TextField()
        {
            value = value == Guid.Empty ? string.Empty : value.ToString()
        };
        textField.style.flexGrow = 1;


        textField.isDelayed = true;

        textField.RegisterValueChangedCallback(evt =>
        {
            if (!Guid.TryParse(evt.newValue, out var parsed))
            {
                //textField.AddToClassList("unity-invalid");
                //Warning.SetEnabled(true);
                return;
            }

            //textField.RemoveFromClassList("unity-invalid");
            //Warning.SetEnabled(false);

            Undo.RecordObject(target, field.Name);
            field.SetValue(target, parsed);
            EditorUtility.SetDirty(target);
        });

        root.Add(textField);


        var RerollButton = new Button(() =>
        {
            if (!EditorUtility.DisplayDialog("Reroll GUID", $"Generate new GUID for '{field.Name}'?", "Yes", "No"))
                return;

            var newGuid = Guid.NewGuid();
            Undo.RecordObject(target, field.Name);
            field.SetValue(target, newGuid);
            EditorUtility.SetDirty(target);
            textField.value = newGuid.ToString();
        })
        {
            tooltip = "Reroll GUID"
        };

        // Try to add icon from TinyIcons; fall back to text if icon is missing
        var iconTex = TinyIcons.GetIcon(TinyIcon.Reload);
        if (iconTex != null)
        {
            var img = new Image { image = iconTex };
            img.style.width = img.style.height = 10;
            //img.style.marginLeft = 2;
            RerollButton.Add(img);
            // ensure button is sized to icon
            RerollButton.style.width = 18;
        }
        else
        {
            RerollButton.text = "R";
        }

        RerollButton.style.marginRight = -1;
        


        root.Add(RerollButton);


        return root;
    }
}


public static class TinyTimeSpanDrawer
{
    public static VisualElement Create(FieldInfo field, UnityEngine.Object target)
    {
        var value = (TimeSpan)field.GetValue(target);

        var root = new VisualElement();
        root.style.flexDirection = FlexDirection.Row;
        root.AddToClassList("unity-property-field__inspector-property");

        var label = new TinyLabel(field.Name);
        root.Add(label);

        var row = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                flexGrow = 1
            }
        };

        var days = CreateInt(value.Days);
        var hours = CreateInt(value.Hours);
        var minutes = CreateInt(value.Minutes);
        var seconds = CreateInt(value.Seconds);

        row.Add(days);
        row.Add(hours);
        row.Add(minutes);
        row.Add(seconds);

        root.Add(row);

        void Apply()
        {
            var ts = new TimeSpan(
                days.value,
                hours.value,
                minutes.value,
                seconds.value
            );

            Undo.RecordObject(target, field.Name);
            field.SetValue(target, ts);
            EditorUtility.SetDirty(target);
        }

        foreach (var f in new[] { days, hours, minutes, seconds })
            f.RegisterValueChangedCallback(_ => Apply());

        return root;
    }

    static IntegerField CreateInt(int value)
    {
        var f = new IntegerField
        {
            value = value
        };

        f.style.flexGrow = 1;
        f.isDelayed = true;
        return f;
    }
}

public static class TinyDateTimeDrawer
{
    public static VisualElement Create(FieldInfo field, UnityEngine.Object target)
    {
        var value = (DateTime)field.GetValue(target);
        if (value == default)
            value = DateTime.Now;

        var root = new VisualElement();
        root.style.flexDirection = FlexDirection.Column;
        root.AddToClassList("unity-property-field__inspector-property");

        // ===== ROW 1: LABEL + DATE =====
        var row1 = new VisualElement { style = { flexDirection = FlexDirection.Row } };

        var label = new TinyLabel(field.Name);
        row1.Add(label);

        var dateRow = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                flexGrow = 1,
                alignSelf = Align.Stretch
            }
        };

        var year = CreateInt("Y", value.Year, 1, 9999);
        var month = CreateInt("M", value.Month, 1, 12);
        var day = CreateInt("D", value.Day, 1, 31);

        dateRow.Add(year);
        dateRow.Add(month);
        dateRow.Add(day);

        row1.Add(dateRow);
        root.Add(row1);

        // ===== ROW 2: EMPTY LABEL + TIME =====
        var row2 = new VisualElement { style = { flexDirection = FlexDirection.Row } };

        var newSpacer = new TinyLabel("   ");
        row2.Add(newSpacer);

        //var spacer = new VisualElement();
        //spacer.style.width = ((int)label.style.width.value.value + 4);
        //row2.Add(spacer);

        var timeRow = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                flexGrow = 1,
                alignSelf = Align.Stretch
            }
        };

        var hour = CreateInt("h", value.Hour, 0, 23);
        var minute = CreateInt("m", value.Minute, 0, 59);
        var second = CreateInt("s", value.Second, 0, 59);

        timeRow.Add(hour);
        timeRow.Add(minute);
        timeRow.Add(second);

        row2.Add(timeRow);
        root.Add(row2);

        void Apply()
        {
            try
            {
                var dt = new DateTime(
                    year.value,
                    month.value,
                    day.value,
                    hour.value,
                    minute.value,
                    second.value
                );

                Undo.RecordObject(target, field.Name);
                field.SetValue(target, dt);
                EditorUtility.SetDirty(target);
            }
            catch
            {
                root.AddToClassList("unity-invalid");
            }
        }

        foreach (var f in new[] { year, month, day, hour, minute, second })
            f.RegisterValueChangedCallback(_ => Apply());

        return root;
    }

    static IntegerField CreateInt(string labelText, int value, int min, int max)
    {
        var f = new IntegerField
        {
            value = value,
            style =
        {
            flexBasis = 0,
            flexGrow = 1
        },
            isDelayed = true
        };

        if (!string.IsNullOrEmpty(labelText))
        {
            var label = new Label(labelText)
            {
                tooltip = labelText
            };
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.alignSelf = Align.FlexEnd; // na dole
            label.style.fontSize = 10;
            label.style.height = 18;
            label.style.position = Position.Absolute;
            label.style.right = 2; 
            label.style.opacity = 0.6f;


            label.style.marginTop = 2; // odstêp od pola

            f.Add(label);
        }

        f.RegisterValueChangedCallback(evt =>
        {
            if (evt.newValue < min || evt.newValue > max)
                f.AddToClassList("unity-invalid");
            else
                f.RemoveFromClassList("unity-invalid");
        });

        return f;
    }
}

public static class TinyVersionDrawer
{
    public static VisualElement Create(FieldInfo field, UnityEngine.Object target)
    {
        var version = (Version)field.GetValue(target) ?? new Version(0, 0);

        var root = new VisualElement();
        root.style.flexDirection = FlexDirection.Row;
        root.AddToClassList("unity-property-field__inspector-property");

        var label = new TinyLabel(field.Name);
        root.Add(label);

        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.flexGrow = 1;
        //row.style.gap = 4;
        root.Add(row);

        IntegerField CreatePart(string name, int value, Action<int> setter)
        {
            var f = new IntegerField()
            {
                value = value < 0 ? 0 : value
            };

            f.style.flexGrow = 1;
            f.isDelayed = true;

            f.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue < 0)
                {
                    f.AddToClassList("unity-invalid");
                    return;
                }

                f.RemoveFromClassList("unity-invalid");
                setter(evt.newValue);

                Undo.RecordObject(target, field.Name);
                field.SetValue(target, version);
                EditorUtility.SetDirty(target);
            });

            return f;
        }

        row.Add(CreatePart("Major", version.Major, v => version = new Version(v, version.Minor)));
        row.Add(CreatePart("Minor", version.Minor, v => version = new Version(version.Major, v)));
        row.Add(CreatePart("Build", version.Build, v => version = new Version(version.Major, version.Minor, v)));
        row.Add(CreatePart("Revision", version.Revision, v => version = new Version(
            version.Major,
            version.Minor,
            version.Build < 0 ? 0 : version.Build,
            v)));

        return root;
    }
}

#endif
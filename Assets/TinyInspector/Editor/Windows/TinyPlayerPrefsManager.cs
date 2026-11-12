using Codice.CM.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
public sealed class TinyPlayerPrefsManager : EditorWindow
{
    private enum PrefValueKind
    {
        String,
        Int,
        Float,
        Unknown,
    }

    [Serializable]
    private sealed class PrefEntry
    {
        public string Key;
        public string RawKey;
        public PrefValueKind Kind;
        public string Value;

        public string EditValue;
        public bool IsEditing;
    }

    private readonly List<PrefEntry> _entries = new();
    private string _filter = string.Empty;
    private bool _showUnknown;
    private bool _hideEditorKeys = false;
    
    private ToolbarSearchField _filterField;
    private ScrollView _scrollView;
    private VisualElement _contentContainer;

    [MenuItem("Tools/Tiny Inspector/Tools/PlayerPrefs Manager")]
    private static void Open()
    {
        GetWindow<TinyPlayerPrefsManager>("Player Prefs Manager");
    }

    private void OnEnable()
    {
        titleContent = new GUIContent("Player Prefs Manager", EditorGUIUtility.IconContent("d_Folder Icon").image);
        Refresh();
    }

    private void CreateGUI()
    {
        StyleSheet sheet = Resources.Load<StyleSheet>("TinyInspector/Editor/TinyInspector");
        rootVisualElement.styleSheets.Add(sheet);

        var root = rootVisualElement;
        root.style.flexDirection = FlexDirection.Column;

        // Top toolbar
        var toolbar = new Toolbar();
        toolbar.AddToClassList("TinyToolbar");

        var refreshBtn = new ToolbarButton(() => Refresh()) { text = "Refresh" };
        refreshBtn.AddToClassList("TinyToolbarButton");
        refreshBtn.style.width = 70;
        toolbar.Add(refreshBtn);


        _filterField = new ToolbarSearchField();
        _filterField.AddToClassList("TinyToolbarField");
        _filterField.style.minWidth = 200;
        _filterField.RegisterValueChangedCallback(evt =>
        {
            _filter = evt.newValue;
            RefreshContent();
        });
        toolbar.Add(_filterField);

        var unknownToggle = new ToolbarToggle();
        unknownToggle.text = "Show unknown";
        unknownToggle.AddToClassList("unity-toolbar-toggle");
        unknownToggle.style.width = 110;
        unknownToggle.RegisterValueChangedCallback(evt =>
        {
            _showUnknown = evt.newValue;
            RefreshContent();
        });
        toolbar.Add(unknownToggle);

        var hideEditorToggle = new ToolbarToggle();
        hideEditorToggle.text = "Hide editor keys";
        hideEditorToggle.AddToClassList("unity-toolbar-toggle");
        hideEditorToggle.style.width = 120;
        hideEditorToggle.value = _hideEditorKeys;
        hideEditorToggle.RegisterValueChangedCallback(evt =>
        {
            _hideEditorKeys = evt.newValue;
            RefreshContent();
        });
        toolbar.Add(hideEditorToggle);

        var spacer2 = new VisualElement();
        spacer2.style.flexGrow = 1;
        toolbar.Add(spacer2);

        var addBtn = new ToolbarButton(() =>
        {
            AddPrefWindow.Show(() => Refresh());
        }) { text = "Add" };
        addBtn.AddToClassList("TinyToolbarButton");
        addBtn.style.width = 70;
        toolbar.Add(addBtn);

        var deleteAllBtn = new ToolbarButton(() =>
        {
            if (EditorUtility.DisplayDialog("PlayerPrefs", "Delete all PlayerPrefs?", "Delete", "Cancel"))
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                Refresh();
            }
        }) { text = "Delete All" };
        deleteAllBtn.AddToClassList("TinyToolbarButton");
        deleteAllBtn.style.width = 80;
        toolbar.Add(deleteAllBtn);

        root.Add(toolbar);

        // Main content area
        _contentContainer = new VisualElement();
        _contentContainer.style.flexGrow = 1;

        // Scroll view
        _scrollView = new ScrollView();
        _scrollView.style.flexGrow = 1;
        _contentContainer.Add(_scrollView);

        root.Add(_contentContainer);

        RefreshContent();
    }

    private IEnumerable<PrefEntry> FilteredEntries()
    {
        IEnumerable<PrefEntry> q = _entries;

        if (!_showUnknown)
            q = q.Where(e => e.Kind != PrefValueKind.Unknown);

        // don't filter editor keys here, grouping is handled in RefreshContent

        var f = _filter?.Trim();
        if (!string.IsNullOrEmpty(f))
            q = q.Where(e => e.Key.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0);

        return q;
    }

    private void RefreshContent()
    {
        if (_scrollView == null)
            return;

        _scrollView.Clear();

        if (_entries.Count == 0)
        {
            var helpBox = new HelpBox("No PlayerPrefs found for the current project/application.", HelpBoxMessageType.Info);
            _scrollView.Add(helpBox);
            return;
        }

        var filtered = FilteredEntries().ToList();

        // split into user keys and editor/vendor keys
        var userKeys = filtered.Where(e => !IsEditorKey(e.Key) && !IsEditorKey(e.RawKey ?? string.Empty)).ToList();
        var editorKeys = filtered.Except(userKeys).ToList();

        // show user keys first
        foreach (var entry in userKeys)
            _scrollView.Add(CreateEntryRow(entry));

        if (!_hideEditorKeys && editorKeys.Count > 0)
        {
            // separator label
            var section = new Label($"Unity Defined ({editorKeys.Count})");
            section.style.unityFontStyleAndWeight = FontStyle.Bold;
            section.style.marginTop = 6;
            section.style.marginBottom = 4;
            section.style.paddingLeft = 4;
            _scrollView.Add(section);

            foreach (var entry in editorKeys)
                _scrollView.Add(CreateEntryRow(entry, true));
        }
    }

    private VisualElement CreateEntryRow(PrefEntry entry, bool editorKey = false)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.marginBottom = 2;
        row.style.marginLeft = 0;
        row.style.marginRight = 0;
        row.style.paddingTop = 2;
        row.style.paddingBottom = 3;
        row.style.borderBottomWidth = 1;
        row.style.borderBottomColor = new StyleColor(new Color(1,1,1,0.1f));


        // Kind label
        var kindLabel = new Label(entry.Kind.ToString().ToUpper());
        kindLabel.style.width = 70;
        kindLabel.style.flexShrink = 0;
        kindLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        kindLabel.style.color = entry.Kind switch
        {
            PrefValueKind.String => new Color(0.8f, 0.9f, 1.0f, 1.0f),
            PrefValueKind.Int => new Color(0.8f, 1.0f, 0.8f, 1.0f),
            PrefValueKind.Float => new Color(1.0f, 0.9f, 0.6f, 1.0f),
            PrefValueKind.Unknown => new Color(1.0f, 0.2f, 0.2f, 1.0f),
            _ => Color.white,
        };
        kindLabel.style.backgroundColor = entry.Kind switch
        {
            PrefValueKind.String => new Color(0.8f, 0.9f, 1.0f, .1f),
            PrefValueKind.Int => new Color(0.8f, 1.0f, 0.8f, .1f),
            PrefValueKind.Float => new Color(1.0f, 0.9f, 0.6f, .1f),
            PrefValueKind.Unknown => new Color(1.0f, 0.2f, 0.2f, .1f),
            _ => Color.white,
        };
        kindLabel.style.marginTop = 2;
        kindLabel.style.marginBottom = 2;
        kindLabel.style.fontSize = 10;
        kindLabel.style.borderBottomLeftRadius = 3;
        kindLabel.style.borderBottomRightRadius = 3;
        kindLabel.style.borderTopLeftRadius = 3;
        kindLabel.style.borderTopRightRadius = 3;
        kindLabel.style.marginLeft = 4;
        row.Add(kindLabel);

        // Key label
        var keyLabel = new Label(entry.Key);
        keyLabel.style.minWidth = 180;
        keyLabel.style.flexShrink = 0;
        keyLabel.style.marginLeft = 4;
        keyLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
        row.Add(keyLabel);


        // Value field
        var current = entry.Value ?? string.Empty;
        entry.EditValue ??= current;

        var valueField = new TextField();
        valueField.style.flexGrow = 1;
        valueField.value = entry.IsEditing ? entry.EditValue : current;
        valueField.isReadOnly = !entry.IsEditing || entry.Kind == PrefValueKind.Unknown;
        
        if (!entry.IsEditing || entry.Kind == PrefValueKind.Unknown)
        {
            valueField.SetEnabled(false);
        }
        else
        {
            valueField.RegisterValueChangedCallback(evt => entry.EditValue = evt.newValue);
        }
        
        row.Add(valueField);

        // Copy button
        var copyBtn = new Button(() =>
        {
            EditorGUIUtility.systemCopyBuffer = entry.Value ?? string.Empty;
        }) { text = "Copy" };
        copyBtn.style.width = 50;
        copyBtn.style.marginLeft = 0;
        row.Add(copyBtn);

        // Edit/Save/Cancel buttons
        if (entry.IsEditing)
        {
            var saveBtn = new Button(() =>
            {
                if (TryApplyEdit(entry))
                {
                    PlayerPrefs.Save();
                    Refresh();
                }
            }) { text = "Save" };
            saveBtn.style.width = 55;
            saveBtn.style.marginLeft = 0;
            row.Add(saveBtn);

            var cancelBtn = new Button(() =>
            {
                entry.EditValue = entry.Value;
                entry.IsEditing = false;
                RefreshContent();
            }) { text = "Cancel" };
            cancelBtn.style.width = 60;
            cancelBtn.style.marginLeft = 0;
            row.Add(cancelBtn);
        }
        else
        {
            var editBtn = new Button(() =>
            {
                if (entry.Kind != PrefValueKind.Unknown)
                {
                    entry.IsEditing = true;
                    entry.EditValue = entry.Value ?? string.Empty;
                    RefreshContent();
                }
            }) { text = "Edit" };
            editBtn.style.width = 55;
            editBtn.style.marginLeft = 0;
            editBtn.SetEnabled((entry.Kind != PrefValueKind.Unknown) && !editorKey);
            row.Add(editBtn);

            var deleteBtn = new Button(() =>
            {
                if (EditorUtility.DisplayDialog("PlayerPrefs", $"Delete key: {entry.Key}?", "Delete", "Cancel"))
                {
                    if (TryDeleteKey(entry))
                    {
                        PlayerPrefs.Save();
                        Refresh();
                    }
                }
            }) { text = "Delete" };
            deleteBtn.style.width = 60;
            deleteBtn.style.marginLeft = 0;
deleteBtn.SetEnabled(!editorKey); 
            row.Add(deleteBtn);
        }

        return row;
    }

    private static bool TryApplyEdit(PrefEntry entry)
    {
        try
        {
            switch (entry.Kind)
            {
                case PrefValueKind.String:
                    PlayerPrefs.SetString(entry.Key, entry.EditValue ?? string.Empty);
                    entry.Value = entry.EditValue ?? string.Empty;
                    return true;
                case PrefValueKind.Int:
                {
                    if (!int.TryParse(entry.EditValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                    {
                    EditorUtility.DisplayDialog("PlayerPrefs", "Invalid int value.", "OK");
                        return false;
                    }
                    PlayerPrefs.SetInt(entry.Key, i);
                    entry.Value = i.ToString(CultureInfo.InvariantCulture);
                    return true;
                }
                case PrefValueKind.Float:
                {
                    if (!float.TryParse(entry.EditValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
                    {
                    EditorUtility.DisplayDialog("PlayerPrefs", "Invalid float value (use dot as decimal separator).", "OK");
                        return false;
                    }
                    PlayerPrefs.SetFloat(entry.Key, f);
                    entry.Value = f.ToString(CultureInfo.InvariantCulture);
                    return true;
                }
                default:
                    return false;
            }
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("PlayerPrefs", $"Save error: {ex.Message}", "OK");
            return false;
        }
    }

    private static bool TryDeleteKey(PrefEntry entry)
    {
        // If key comes from registry, delete using raw name.
        if (!string.IsNullOrEmpty(entry.RawKey) && !string.Equals(entry.RawKey, entry.Key, StringComparison.Ordinal))
        {
#if UNITY_EDITOR_WIN
            try
            {
                var regPath = $"Software\\Unity\\UnityEditor\\{Application.companyName}\\{Application.productName}";
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(regPath, writable: true);
                if (key == null)
                    return false;
                key.DeleteValue(entry.RawKey, throwOnMissingValue: false);
                return true;
            }
            catch
            {
                return false;
            }
#else
            return false;
#endif
        }

        PlayerPrefs.DeleteKey(entry.Key);
        return true;
    }

    private void Refresh()
    {
        _entries.Clear();

        // Try to read PlayerPrefs from platform storage (Windows/macOS).
        if (TryReadFromWindowsRegistry(out var winEntries))
        {
            _entries.AddRange(winEntries);
        }
        else if (TryReadFromMacPlist(out var macEntries))
        {
            _entries.AddRange(macEntries);
        }
        else
        {
            // Fallback: brak danych.
        }

        _entries.Sort((a, b) => string.Compare(a.Key, b.Key, StringComparison.OrdinalIgnoreCase));
        RefreshContent();
    }

    private static bool TryReadFromWindowsRegistry(out List<PrefEntry> entries)
    {
        entries = new List<PrefEntry>();

#if UNITY_EDITOR_WIN
        try
        {
            // Unity stores prefs at: HKCU\Software\Unity\UnityEditor\<CompanyName>\<ProductName>
            var regPath = $"Software\\Unity\\UnityEditor\\{Application.companyName}\\{Application.productName}";
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(regPath);
            if (key == null)
                return false;

            foreach (var valueName in key.GetValueNames())
            {
                var raw = key.GetValue(valueName);
                if (raw == null)
                    continue;

                var decodedKey = DecodeWindowsRegistryKey(valueName, out _);
                var registryKind = GetRegistryValueKindSafe(key, valueName);

                // Prefer reading value via PlayerPrefs to determine type.
                var (kind, valueString) = TryReadViaPlayerPrefs(decodedKey);
                if (kind == PrefValueKind.Unknown)
                {
                    // Fallback: poka¿ surow¹ wartoœæ z rejestru.
                    (kind, valueString) = RegistryValueToString(raw, PrefValueKind.Unknown, registryKind);
                }

                entries.Add(new PrefEntry
                {
                    Key = decodedKey,
                    RawKey = valueName,
                    Kind = kind,
                    Value = valueString,
                    EditValue = valueString,
                });
            }

            return true;
        }
        catch
        {
            return false;
        }
#else
        return false;
#endif
    }

    private static (PrefValueKind kind, string value) TryReadViaPlayerPrefs(string decodedKey)
    {
        if (string.IsNullOrEmpty(decodedKey))
            return (PrefValueKind.Unknown, string.Empty);

        // Heuristic: use GetString/GetInt/GetFloat to guess stored type.

        const string defaultString = "__TINY_PP_DEFAULT__";
        var s = PlayerPrefs.GetString(decodedKey, defaultString);
        if (!string.Equals(s, defaultString, StringComparison.Ordinal))
            return (PrefValueKind.String, s);

        const int defaultInt = int.MinValue;
        var i = PlayerPrefs.GetInt(decodedKey, defaultInt);
        if (i != defaultInt)
            return (PrefValueKind.Int, i.ToString(CultureInfo.InvariantCulture));

        const float defaultFloat = -1234567.125f;
        var f = PlayerPrefs.GetFloat(decodedKey, defaultFloat);
        if (Math.Abs(f - defaultFloat) > 0.00001f)
            return (PrefValueKind.Float, f.ToString(CultureInfo.InvariantCulture));

        // Jeœli klucz istnieje, ale wartoœæ równa siê domyœlnej, nie da siê odró¿niæ.
        return (PrefValueKind.Unknown, string.Empty);
    }

    private static PrefValueKind PreferNonUnknown(PrefValueKind a, PrefValueKind b)
        => a != PrefValueKind.Unknown ? a : b;

    private static Microsoft.Win32.RegistryValueKind GetRegistryValueKindSafe(Microsoft.Win32.RegistryKey key, string valueName)
    {
#if UNITY_EDITOR_WIN
        try
        {
            return key.GetValueKind(valueName);
        }
        catch
        {
            return Microsoft.Win32.RegistryValueKind.Unknown;
        }
#else
        return Microsoft.Win32.RegistryValueKind.Unknown;
#endif
    }

    private static PrefValueKind GetKindFromRegistryValue(Microsoft.Win32.RegistryValueKind kind)
    {
        return kind switch
        {
            Microsoft.Win32.RegistryValueKind.String => PrefValueKind.String,
            Microsoft.Win32.RegistryValueKind.ExpandString => PrefValueKind.String,
            Microsoft.Win32.RegistryValueKind.DWord => PrefValueKind.Int,
            Microsoft.Win32.RegistryValueKind.QWord => PrefValueKind.Int,
            Microsoft.Win32.RegistryValueKind.Binary => PrefValueKind.Unknown,
            _ => PrefValueKind.Unknown,
        };
    }

    private static string DecodeWindowsRegistryKey(string valueName, out PrefValueKind kind)
    {
        kind = PrefValueKind.Unknown;
        if (string.IsNullOrEmpty(valueName))
            return valueName;

        // Key suffixes: _i, _f, _s; Unity may append _h<hash>.
        var hIdx = valueName.LastIndexOf("_h", StringComparison.Ordinal);
        var trimmed = hIdx > 0 ? valueName.Substring(0, hIdx) : valueName;

        if (trimmed.EndsWith("_i", StringComparison.Ordinal))
        {
            kind = PrefValueKind.Int;
            return trimmed.Substring(0, trimmed.Length - 2);
        }

        if (trimmed.EndsWith("_f", StringComparison.Ordinal))
        {
            kind = PrefValueKind.Float;
            return trimmed.Substring(0, trimmed.Length - 2);
        }

        if (trimmed.EndsWith("_s", StringComparison.Ordinal))
        {
            kind = PrefValueKind.String;
            return trimmed.Substring(0, trimmed.Length - 2);
        }

        return trimmed;
    }

    // Simple heuristic to detect keys likely created by Unity editor or built-in packages.
    private static bool IsEditorKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        var k = key.Trim().ToLowerInvariant();

        // explicit known editor keys
        var known = new[] { "unitygraphicsquality", "unitygraphicsqualityv2" };
        foreach (var kk in known)
            if (k.Contains(kk))
                return true;

        // common editor/vendor prefixes
        if (k.StartsWith("unity") || k.StartsWith("editor.") || k.StartsWith("unityeditor") )
            return true;

        if (k.StartsWith("com.unity3d.") || k.StartsWith("com.unity."))
            return true;

        // other hints
        if (k.Contains("unityeditor") || k.Contains("unity_") || k.Contains("unityads") || k.Contains("collab"))
            return true;
        

        return false;
    }

    private static (PrefValueKind kind, string value) RegistryValueToString(object raw, PrefValueKind kindFromKey, Microsoft.Win32.RegistryValueKind registryKind)
    {
        // Registry values may store types differently (int, float, string).
        switch (raw)
        {
            case int i:
                return (kindFromKey == PrefValueKind.Unknown ? PrefValueKind.Int : kindFromKey,
                    i.ToString(CultureInfo.InvariantCulture));
            case long l:
                // Niektóre œrodowiska potrafi¹ trzymaæ float w QWORD (double) lub int w QWORD.
                if (kindFromKey == PrefValueKind.Float)
                {
                    var asDouble = BitConverter.Int64BitsToDouble(l);
                    return (PrefValueKind.Float, ((float)asDouble).ToString(CultureInfo.InvariantCulture));
                }

                return (kindFromKey == PrefValueKind.Unknown ? PrefValueKind.Int : kindFromKey,
                    l.ToString(CultureInfo.InvariantCulture));
            case byte[] bytes:
                if (kindFromKey == PrefValueKind.String)
                {
                    var s = TryDecodeBinaryString(bytes);
                    if (s != null)
                        return (PrefValueKind.String, s);
                }

                if (bytes.Length == 4)
                {
                    var asFloat = BitConverter.ToSingle(bytes, 0);
                    if (kindFromKey == PrefValueKind.Float)
                        return (PrefValueKind.Float, asFloat.ToString(CultureInfo.InvariantCulture));

                    var asInt = BitConverter.ToInt32(bytes, 0);
                    if (kindFromKey == PrefValueKind.Int)
                        return (PrefValueKind.Int, asInt.ToString(CultureInfo.InvariantCulture));

                    // uncertain
                    return (PrefValueKind.Unknown, $"bytes(4) int={asInt} float={asFloat.ToString(CultureInfo.InvariantCulture)}");
                }

                if (bytes.Length == 8 && kindFromKey == PrefValueKind.Float)
                {
                    var asDouble = BitConverter.ToDouble(bytes, 0);
                    return (PrefValueKind.Float, ((float)asDouble).ToString(CultureInfo.InvariantCulture));
                }

                return (PrefValueKind.Unknown, $"bytes({bytes.Length}) {BitConverter.ToString(bytes)}");
            case string s:
                return (PrefValueKind.String, s);
            default:
                return (PrefValueKind.Unknown, raw.ToString());
        }
    }

    private static string TryDecodeBinaryString(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return string.Empty;

        // Try UTF-8 then UTF-16LE when binary may contain strings.
        try
        {
            var len = bytes.Length;
            while (len > 0 && bytes[len - 1] == 0)
                len--;

            if (len <= 0)
                return string.Empty;

            var utf8 = Encoding.UTF8.GetString(bytes, 0, len);
            if (IsMostlyPrintable(utf8))
                return utf8;

            if (len % 2 == 0)
            {
                var utf16 = Encoding.Unicode.GetString(bytes, 0, len);
                if (IsMostlyPrintable(utf16))
                    return utf16;
            }
        }
        catch
        {
            // ignore
        }

        return null;
    }

    private static bool IsMostlyPrintable(string s)
    {
        if (string.IsNullOrEmpty(s))
            return true;

        var printable = 0;
        foreach (var ch in s)
        {
            if (!char.IsControl(ch) || ch == '\r' || ch == '\n' || ch == '\t')
                printable++;
        }
        return printable >= (int)(s.Length * 0.9f);
    }

    private static bool TryReadFromMacPlist(out List<PrefEntry> entries)
    {
        entries = new List<PrefEntry>();

#if UNITY_EDITOR_OSX
        try
        {
            // macOS plist path: ~/Library/Preferences/unity.<CompanyName>.<ProductName>.plist
            var home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            var plist = Path.Combine(home, "Library", "Preferences", $"unity.{Application.companyName}.{Application.productName}.plist");
            if (!File.Exists(plist))
                return false;

            // Simple parser: extract <key> and the following value tags.
            var text = File.ReadAllText(plist);
            var dictStart = text.IndexOf("<dict>", StringComparison.OrdinalIgnoreCase);
            if (dictStart < 0)
                return false;

            var i = dictStart;
            while (true)
            {
                var keyOpen = text.IndexOf("<key>", i, StringComparison.OrdinalIgnoreCase);
                if (keyOpen < 0)
                    break;
                var keyClose = text.IndexOf("</key>", keyOpen, StringComparison.OrdinalIgnoreCase);
                if (keyClose < 0)
                    break;

                var keyName = UnescapeXml(text.Substring(keyOpen + 5, keyClose - (keyOpen + 5)).Trim());

                var valStart = keyClose + 6;
                var nextTagOpen = text.IndexOf('<', valStart);
                if (nextTagOpen < 0)
                    break;

                var (kind, value, nextIndex) = ReadPlistValue(text, nextTagOpen);
                entries.Add(new PrefEntry { Key = keyName, Kind = kind, Value = value });
                i = nextIndex;
            }

            return true;
        }
        catch
        {
            return false;
        }
#else
        return false;
#endif
    }

    private static (PrefValueKind kind, string value, int nextIndex) ReadPlistValue(string text, int tagOpenIndex)
    {
        // Support basic plist types: string, integer, real, true/false
        if (StartsWithAt(text, tagOpenIndex, "<string>") || StartsWithAt(text, tagOpenIndex, "<string "))
        {
            var openEnd = text.IndexOf('>', tagOpenIndex);
            var close = text.IndexOf("</string>", openEnd, StringComparison.OrdinalIgnoreCase);
            var val = close >= 0 ? UnescapeXml(text.Substring(openEnd + 1, close - (openEnd + 1))) : string.Empty;
            return (PrefValueKind.String, val, close >= 0 ? close + 9 : openEnd + 1);
        }

        if (StartsWithAt(text, tagOpenIndex, "<integer>") || StartsWithAt(text, tagOpenIndex, "<integer "))
        {
            var openEnd = text.IndexOf('>', tagOpenIndex);
            var close = text.IndexOf("</integer>", openEnd, StringComparison.OrdinalIgnoreCase);
            var val = close >= 0 ? text.Substring(openEnd + 1, close - (openEnd + 1)).Trim() : string.Empty;
            return (PrefValueKind.Int, val, close >= 0 ? close + 10 : openEnd + 1);
        }

        if (StartsWithAt(text, tagOpenIndex, "<real>") || StartsWithAt(text, tagOpenIndex, "<real "))
        {
            var openEnd = text.IndexOf('>', tagOpenIndex);
            var close = text.IndexOf("</real>", openEnd, StringComparison.OrdinalIgnoreCase);
            var val = close >= 0 ? text.Substring(openEnd + 1, close - (openEnd + 1)).Trim() : string.Empty;
            return (PrefValueKind.Float, val, close >= 0 ? close + 7 : openEnd + 1);
        }

        if (StartsWithAt(text, tagOpenIndex, "<true/>") || StartsWithAt(text, tagOpenIndex, "<true />"))
            return (PrefValueKind.Int, "1", tagOpenIndex + 7);

        if (StartsWithAt(text, tagOpenIndex, "<false/>") || StartsWithAt(text, tagOpenIndex, "<false />"))
            return (PrefValueKind.Int, "0", tagOpenIndex + 8);

        var next = text.IndexOf('>', tagOpenIndex);
        next = next < 0 ? text.Length : next + 1;
        return (PrefValueKind.Unknown, "(unsupported plist value)", next);
    }

    private static bool StartsWithAt(string text, int index, string value)
        => index >= 0 && index + value.Length <= text.Length && string.Compare(text, index, value, 0, value.Length, StringComparison.OrdinalIgnoreCase) == 0;

    private static string UnescapeXml(string s)
    {
        if (string.IsNullOrEmpty(s))
            return s;

        return s
            .Replace("&lt;", "<", StringComparison.OrdinalIgnoreCase)
            .Replace("&gt;", ">", StringComparison.OrdinalIgnoreCase)
            .Replace("&amp;", "&", StringComparison.OrdinalIgnoreCase)
            .Replace("&quot;", "\"", StringComparison.OrdinalIgnoreCase)
            .Replace("&apos;", "'", StringComparison.OrdinalIgnoreCase);
    }
}
#endif

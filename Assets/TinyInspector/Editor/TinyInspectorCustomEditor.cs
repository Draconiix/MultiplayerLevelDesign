using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyInspector
{
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class TinyInspectorCustomEditor : UnityEditor.Editor
    {
        private readonly Dictionary<string, bool> foldoutStates = new();
        private readonly Dictionary<string, string> tabSelection = new(); // groupFullName -> selectedTabName

        private readonly Dictionary<Type, bool> customDrawerCache = new();
        private readonly Dictionary<Type, Dictionary<string, FieldInfo>> fieldInfoCache = new();

        private static readonly Dictionary<Type, bool> uiToolkitDrawerSupportCache = new();

        public override VisualElement CreateInspectorGUI()
        {
            serializedObject.Update();

            //Debug.Log($"TinyInspector: CreateInspectorGUI target={serializedObject.targetObject?.GetType().FullName}");

            // Clear reflection caches on each inspector creation to avoid stale
            // FieldInfo/Drawer type data after domain reloads which may cause
            // attribute detection or drawer selection to behave incorrectly.
            fieldInfoCache.Clear();
            customDrawerCache.Clear();
            uiToolkitDrawerSupportCache.Clear();

            if (TryCreateGenericHolderInspector(out var holderInspector))
                return holderInspector;

            var root = BuildTree();
            var rootContainer = new VisualElement();

            var sheet = LoadTinyInspectorStyleSheet();
            if (sheet != null)
                rootContainer.styleSheets.Add(sheet);
            else
                Debug.LogWarning("TinyInspector: USS stylesheet not found at Resources/TinyInspector/Editor/TinyInspector. Ensure the .uss is imported as a StyleSheet and the path is correct.");

            var scriptInfo = CreateMonoscriptInfoBox();
            if (scriptInfo != null)
                rootContainer.Add(scriptInfo);

            RenderNodeUI(root, rootContainer);
            rootContainer.Bind(serializedObject);

            return rootContainer;
        }

        private bool TryCreateGenericHolderInspector(out VisualElement rootHolderVE)
        {
            rootHolderVE = null;

            var targetObj = serializedObject.targetObject;
            if (targetObj == null)
                return false;

            var t = targetObj.GetType();
            var itemField = t.GetField("item", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (itemField == null)
                return false;

            rootHolderVE = new VisualElement();
            rootHolderVE.style.flexDirection = FlexDirection.Column;

            var itemProp = serializedObject.FindProperty("item");
            if (itemProp != null)
            {
                var iter = itemProp.Copy();
                var end = iter.GetEndProperty();
                if (iter.NextVisible(true))
                {
                    while (!SerializedProperty.EqualContents(iter, end))
                    {
                        if (iter.propertyPath != itemProp.propertyPath)
                        {
                            var pf = new PropertyField(iter.Copy());
                            rootHolderVE.Add(pf);
                        }

                        if (!iter.NextVisible(false))
                            break;
                    }
                }
            }

            rootHolderVE.Bind(serializedObject);
            return true;
        }

        private static bool HasUIToolkitDrawerForType(Type targetType)
        {
            if (targetType == null) return false;

            if (uiToolkitDrawerSupportCache.TryGetValue(targetType, out var cached))
                return cached;

            var drawerType = GetDrawerTypeForFieldType(targetType);
            if (drawerType == null)
            {
                uiToolkitDrawerSupportCache[targetType] = false;
                return false;
            }

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var mi = drawerType.GetMethod("CreatePropertyGUI", flags);
            var supports = mi != null && mi.DeclaringType != typeof(PropertyDrawer);
            uiToolkitDrawerSupportCache[targetType] = supports;
            return supports;
        }

        private static Type GetDrawerTypeForFieldType(Type targetType)
        {
            try
            {
                var editorAsm = typeof(UnityEditor.Editor).Assembly;
                var scriptAttrUtil = editorAsm.GetType("UnityEditor.ScriptAttributeUtility");
                if (scriptAttrUtil != null)
                {
                    var mi = scriptAttrUtil.GetMethod("GetDrawerTypeForType", BindingFlags.Static | BindingFlags.NonPublic);
                    if (mi != null)
                        return mi.Invoke(null, new object[] { targetType }) as Type;
                }
            }
            catch { }

            return null;
        }

        private static StyleSheet LoadTinyInspectorStyleSheet()
        {
            var sheet = Resources.Load<StyleSheet>("TinyInspector/Editor/TinyInspector");
            if (sheet != null)
            {
                //Debug.Log("TinyInspector: loaded USS via Resources: " + sheet.name);
                return sheet;
            }

#if UNITY_EDITOR
            var assetPath = "Assets/Resources/TinyInspector/Editor/TinyInspector.uss";
            Debug.Log("TinyInspector: Resources.Load returned null for StyleSheet. Trying AssetDatabase at: " + assetPath);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(assetPath);
            if (sheet != null)
                Debug.Log("TinyInspector: loaded USS via AssetDatabase: " + sheet.name);
            else
                Debug.LogWarning("TinyInspector: AssetDatabase failed to load StyleSheet at: " + assetPath + ". Check that the .uss is imported as a StyleSheet asset (not plain text) and the path is correct.");
#endif

            return sheet;
        }

        private VisualElement CreateMonoscriptInfoBox()
        {
            var scriptProp = serializedObject.FindProperty("m_Script");
            if (scriptProp == null)
                return null;

            var scriptObj = scriptProp.objectReferenceValue as MonoScript;

            var targetType = serializedObject.targetObject != null ? serializedObject.targetObject.GetType() : null;
            var (docUrl, docLabel, docDesc) = ReadMonoscriptInfo(targetType);

            var infoBox = new VisualElement();
            infoBox.AddToClassList("InfoBox");
            infoBox.AddToClassList("Monoscript");
            infoBox.style.paddingBottom = 0;
            infoBox.style.paddingTop = 0;
            infoBox.style.paddingRight = 0;
            infoBox.style.flexDirection = FlexDirection.Row;
            infoBox.style.alignItems = Align.Center;
            infoBox.style.borderBottomColor = TinyInspectorStyles.BorderColor;
            infoBox.style.borderTopColor = TinyInspectorStyles.BorderColor;
            infoBox.style.borderRightColor = TinyInspectorStyles.BorderColor;
            infoBox.style.borderLeftColor = TinyInspectorStyles.BorderColor;

            var iconBox = new VisualElement();
            iconBox.AddToClassList("InfoBoxIconBox");
            iconBox.style.width = 32;
            iconBox.style.height = 32;
            iconBox.style.flexShrink = 0;
            iconBox.style.borderBottomWidth = 0;
            iconBox.style.borderRightColor = TinyInspectorStyles.BorderColor;

            var iconTex = TinyIcons.GetIcon(TinyIcon.Script) as Texture2D;
            if (iconTex != null)
            {
                var img = new VisualElement();
                img.style.backgroundImage = iconTex;
                img.AddToClassList("InfoBoxIcon");
                img.style.width = 24;
                img.style.height = 24;
                img.style.marginLeft = 4;
                img.style.marginRight = 4;
                img.style.unityBackgroundImageTintColor = TinyInspectorStyles.LabelColor;
                iconBox.Add(img);
            }

            infoBox.Add(iconBox);

            var textContainer = new VisualElement();
            textContainer.style.flexDirection = FlexDirection.Column;
            textContainer.style.flexGrow = 1;
            textContainer.style.marginLeft = 4;

            var titleText = scriptObj != null
                ? PrettifyName(scriptObj.name)
                : (targetType != null ? PrettifyName(targetType.Name) : "Script");

            var title = new Label(titleText);
            title.AddToClassList("InfoBoxTitle");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;

            textContainer.Add(title);
            if (!string.IsNullOrEmpty(docDesc))
            {
                var message = new Label(docDesc);
                message.AddToClassList("InfoBoxMessage");
                textContainer.Add(message);
            }

            infoBox.Add(textContainer);

            var buttons = new VisualElement();
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.alignItems = Align.Center;
            buttons.style.justifyContent = Justify.FlexEnd;
            buttons.style.flexShrink = 0;

            var editBtn = new Button(() =>
            {
                var scriptRef = scriptProp.objectReferenceValue;
                if (scriptRef != null)
                    AssetDatabase.OpenAsset(scriptRef);
                else if (scriptObj != null)
                    AssetDatabase.OpenAsset(scriptObj);
            })
            { text = "" };
            editBtn.AddToClassList("ScriptEditButton");
            editBtn.tooltip = "Edit Script";
            editBtn.style.borderLeftColor = TinyInspectorStyles.BorderColor;

            var editIconTex = TinyIcons.GetIcon(TinyIcon.Edit) as Texture2D;
            if (editIconTex != null)
            {
                var editImg = new VisualElement();
                editImg.style.backgroundImage = editIconTex;
                editImg.style.width = 22;
                editImg.style.height = 22;
                editImg.style.opacity = 0.8f;
                editImg.style.unityBackgroundImageTintColor = TinyInspectorStyles.LabelColor;
                editBtn.Add(editImg);
            }

            buttons.Add(editBtn);

            if (!string.IsNullOrEmpty(docUrl))
            {
                var docBtn = new Button(() => Application.OpenURL(docUrl)) { text = "" };
                docBtn.AddToClassList("ScriptDocButton");
                docBtn.tooltip = string.IsNullOrEmpty(docLabel) ? "Documentation" : docLabel;
                docBtn.style.width = 33;
                docBtn.style.borderLeftColor = TinyInspectorStyles.BorderColor;

                var docIconTex = TinyIcons.GetIcon(TinyIcon.WWW) as Texture2D;
                if (docIconTex != null)
                {
                    var docImg = new VisualElement();
                    docImg.style.backgroundImage = docIconTex;
                    docImg.style.width = 24;
                    docImg.style.height = 24;
                    docImg.style.opacity = 0.8f;
                    docImg.style.unityBackgroundImageTintColor = TinyInspectorStyles.LabelColor;
                    docBtn.Add(docImg);
                }

                buttons.Add(docBtn);
            }

            infoBox.Add(buttons);
            return infoBox;
        }

        private static (string url, string label, string desc) ReadMonoscriptInfo(Type targetType)
        {
            if (targetType == null)
                return (null, null, null);

            foreach (var a in targetType.GetCustomAttributes(true))
            {
                if (a == null)
                    continue;

                var at = a.GetType();
                if (at.FullName != "TinyInspector.MonoscriptInfoAttribute")
                    continue;

                var propUrl = at.GetProperty("Url");
                var propLabel = at.GetProperty("Label");
                var propDesc = at.GetProperty("Desc");

                var url = propUrl?.GetValue(a) as string;
                var label = propLabel?.GetValue(a) as string;
                var desc = propDesc?.GetValue(a) as string;

                return (url, label, desc);
            }

            return (null, null, null);
        }

        private GroupNode BuildTree()
        {
            var root = new GroupNode(null, null);

            GroupNode FindOrCreate(string fullName)
            {
                if (string.IsNullOrEmpty(fullName)) return root;

                var parts = fullName.Split('/');
                var node = root;
                var pathAcc = node.FullName ?? string.Empty;

                foreach (var part in parts)
                {
                    var child = node.FindChild(part);
                    if (child == null)
                    {
                        var full = string.IsNullOrEmpty(pathAcc) ? part : pathAcc + "/" + part;
                        child = new GroupNode(part, full);
                        node.Items.Add(child);
                    }

                    node = child;
                    pathAcc = node.FullName;
                }

                return node;
            }

            if(serializedObject.targetObject == null)
                return root;
            var targetType = serializedObject.targetObject.GetType();

            var fields = targetType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var methods = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            var members = new List<MemberInfo>(fields.Length + methods.Length);
            members.AddRange(fields);
            members.AddRange(methods);

            members.Sort((a, b) =>
            {
                try { return a.MetadataToken.CompareTo(b.MetadataToken); }
                catch { return 0; }
            });

            foreach (var member in members)
            {
                if (member is FieldInfo field)
                {
                    if (field.Name == "m_Script")
                        continue;

                    var prop = serializedObject.FindProperty(field.Name);
                    var isSerialized = prop != null;

                    var boxGroup = (BoxGroupAttribute)Attribute.GetCustomAttribute(field, typeof(BoxGroupAttribute));
                    var foldoutGroup = (FoldoutGroupAttribute)Attribute.GetCustomAttribute(field, typeof(FoldoutGroupAttribute));
                    var tabGroup = (TabGroupAttribute)Attribute.GetCustomAttribute(field, typeof(TabGroupAttribute));
                    var verticalGroup = (VerticalGroupAttribute)Attribute.GetCustomAttribute(field, typeof(VerticalGroupAttribute));
                    var horizontalGroup = (HorizontalGroupAttribute)Attribute.GetCustomAttribute(field, typeof(HorizontalGroupAttribute));

                    if (foldoutGroup != null)
                    {
                        var node = FindOrCreate(foldoutGroup.GroupName);
                        node.IsFoldout = true;
                        node.DefaultExpanded = foldoutGroup.DefaultExpanded;

                        if (foldoutGroup.IconName != TinyIcon.None)
                        {
                            var tex = TinyIcons.GetIcon(foldoutGroup.IconName);
                            if (tex != null) node.IconTexture = tex;
                            else node.Icon = foldoutGroup.IconName;
                        }

                        node.Color = foldoutGroup.Color;
                    }

                    if (horizontalGroup != null)
                    {
                        var node = FindOrCreate(horizontalGroup.GroupName);
                        node.IsLayoutGroup = true;
                        node.Layout = FlexDirection.Row;
                    }

                    if (verticalGroup != null)
                    {
                        var node = FindOrCreate(verticalGroup.GroupName);
                        node.IsLayoutGroup = true;
                        node.Layout = FlexDirection.Column;
                    }

                    if (boxGroup != null)
                    {
                        var node = FindOrCreate(boxGroup.GroupName);
                        if (boxGroup.Icon != TinyIcon.None)
                            node.Icon = boxGroup.Icon;

                        node.Color = boxGroup.Color;
                    }

                    if (tabGroup != null)
                    {
                        var node = FindOrCreate(tabGroup.GroupName);
                        if (isSerialized) node.AddToTab(tabGroup.TabName, prop.propertyPath);
                        else node.AddToTab(tabGroup.TabName, new NonSerializedFieldItem(field));

                        if (tabGroup.Icon != TinyIcon.None)
                        {
                            var t = TinyIcons.GetIcon(tabGroup.Icon);
                            if (t != null) node.SetTabIconTexture(tabGroup.TabName, t);
                            else node.SetTabIconEnum(tabGroup.TabName, tabGroup.Icon);
                        }

                        node.SetTabColor(tabGroup.TabName, tabGroup.Color);
                        continue;
                    }

                    var target = boxGroup != null
                        ? FindOrCreate(boxGroup.GroupName)
                        : verticalGroup != null
                            ? FindOrCreate(verticalGroup.GroupName)
                            : horizontalGroup != null
                                ? FindOrCreate(horizontalGroup.GroupName)
                                : foldoutGroup != null
                                    ? FindOrCreate(foldoutGroup.GroupName)
                                    : root;

                    if (isSerialized)
                    {
                        target.Items.Add(prop.propertyPath);
                    }
                    else if (field.GetCustomAttribute<TinySerializationAttribute>() != null)
                    {
                        target.Items.Add(new NonSerializedFieldItem(field));
                    }

                    continue;
                }

                if (member is MethodInfo m)
                {
                    if (m.IsSpecialName)
                        continue;

                    foreach (var a in m.GetCustomAttributes(true))
                    {
                        if (a != null && a.GetType().Name == "ButtonAttribute")
                        {
                            root.Items.Add(new MethodButtonItem(m));
                            break;
                        }
                    }
                }
            }

            return root;
        }

        private void RenderNodeUI(GroupNode node, VisualElement parent)
        {
            foreach (var item in node.Items)
            {
                switch (item)
                {
                    case string path:
                        RenderSerializedPropertyPath(path, parent);
                        break;
                    case NonSerializedFieldItem nsfi:
                        RenderNonSerializedField(nsfi, parent);
                        break;
                    case MethodButtonItem mbi:
                        RenderMethodButton(mbi, parent);
                        break;
                    case GroupNode child:
                        RenderGroupNode(child, parent);
                        break;
                }
            }
        }

        private void RenderSerializedPropertyPath(string path, VisualElement parent)
        {
            var prop = serializedObject.FindProperty(path);
            if (prop == null) return;

            //Debug.Log($"TinyInspector: RenderSerializedPropertyPath path='{path}' prop.name='{prop.name}' prop.propertyPath='{prop.propertyPath}' isArray={prop.isArray}");

            var fi = GetFieldInfo(prop.name);
            if (fi == null)
            {
                // Try resolve from the root segment of the property path (handles nested properties/arrays)
                try
                {
                    var root = prop.propertyPath.Split('.')[0];
                    // strip potential array indexer
                    var bracket = root.IndexOf('[');
                    if (bracket > 0) root = root.Substring(0, bracket);
                    if (root != prop.name)
                    {
                        //Debug.Log($"TinyInspector: FieldInfo lookup by prop.name failed, trying root='{root}'");
                        fi = GetFieldInfo(root);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            void ApplyHideLabelIfNeeded(PropertyField pf)
            {
                if (pf == null || fi == null) return;
                if (!HasAttributeByName(fi, nameof(HideLabelAttribute))) return;

                pf.label = string.Empty;
                pf.RegisterCallback<GeometryChangedEvent>(_ =>
                {
                    var labelEl = pf.Q<Label>(className: "unity-property-field__label");
                    if (labelEl != null) labelEl.style.display = DisplayStyle.None;

                });
            }

            // Editor Drawer: PropertySpaceAttrbiute
            if (fi != null)
            {
                var spacerAttr = Attribute.GetCustomAttribute(fi, typeof(PropertySpaceAttribute)) as PropertySpaceAttribute;
                if (spacerAttr != null)
                {
                    var spacerElem = new VisualElement();
                    spacerElem.style.height = spacerAttr.Height;
                    spacerElem.style.minHeight = spacerAttr.Height;
                    spacerElem.style.flexShrink = 0;
                    parent.Add(spacerElem);
                }
            }

            // Dictionary-like IMGUI view
            var hasShowDict = fi != null && HasAttributeByName(fi, "ShowDictionaryDisplayAttribute");
            if (hasShowDict && prop.isArray && prop.propertyType != SerializedPropertyType.String)
            {
                var container = CreateDictionaryLikeArrayUI(path);
                parent.Add(container);
                return;
            }

            // TableList arrays (for now: same behavior as Reorderable, but via separate function)
            if (prop.isArray && prop.propertyType != SerializedPropertyType.String)
            {
                var isTableList = fi != null && HasAttributeByName(fi, nameof(TableListAttribute));
                if (isTableList)
                {
                    //Debug.Log($"TinyInspector: isTableList=true path={path} field={(fi != null ? fi.Name : "null")} fieldType={(fi != null ? fi.FieldType.Name : "null")}");
                }
                if (isTableList)
                {
                    RenderTableListArrayAsReorderable(prop, path, parent);
                    return;
                }
            }

            // Reorderable arrays (only with [Reorderable])
            if (prop.isArray && prop.propertyType != SerializedPropertyType.String)
            {
                var isReorderable = fi != null && HasAttributeByName(fi, "ReorderableAttribute");
                if (!isReorderable)
                {
                    var fieldElement = new PropertyField(prop);
                    fieldElement.AddToClassList("TinyProperty");
                    ApplyHideLabelIfNeeded(fieldElement);
                    parent.Add(fieldElement);
                    return;
                }

                RenderReorderableArray(prop, path, parent);
                return;
            }

            // If there's a custom drawer for this field type and it does NOT support UI Toolkit,
            // render via IMGUI so IMGUI-only drawers still work.
            if (fi != null && HasCustomPropertyDrawerForType(fi.FieldType) && !HasUIToolkitDrawerForType(fi.FieldType))
            {
                //parent.Add(CreateIMGUIPropertyField(path));
                return;
            }

            // Inline custom-class renderer currently disabled; keep default PropertyField.
            var field = new PropertyField(prop);
            field.AddToClassList("TinyProperty");
            ApplyHideLabelIfNeeded(field);
            parent.Add(field);
        }

        private IMGUIContainer CreateIMGUIPropertyField(string propertyPath)
        {
            var c = new IMGUIContainer(() =>
            {
                serializedObject.Update();
                var p = serializedObject.FindProperty(propertyPath);
                if (p == null) return;

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(p, true);
                if (EditorGUI.EndChangeCheck())
                    serializedObject.ApplyModifiedProperties();
            });

            c.AddToClassList("TinyProperty");
            return c;
        }

        private IMGUIContainer CreateDictionaryLikeArrayUI(string path)
        {
            return new IMGUIContainer(() =>
            {
                serializedObject.Update();
                var arrayProp = serializedObject.FindProperty(path);
                if (arrayProp != null && arrayProp.isArray)
                {
                    EditorGUILayout.BeginVertical();
                    for (var i = 0; i < arrayProp.arraySize; i++)
                    {
                        var entry = arrayProp.GetArrayElementAtIndex(i);
                        if (entry == null) continue;

                        var keyProp = entry.FindPropertyRelative("key");
                        var valueProp = entry.FindPropertyRelative("value");

                        EditorGUILayout.BeginHorizontal();
                        var label = keyProp != null ? keyProp.displayName + ":" : ("Item " + i + ":");
                        EditorGUILayout.LabelField(new GUIContent(label), GUILayout.Width(150));

                        if (valueProp != null) EditorGUILayout.PropertyField(valueProp, GUIContent.none);
                        else EditorGUILayout.LabelField("<no value>");

                        if (GUILayout.Button("-", GUILayout.Width(20)))
                        {
                            arrayProp.DeleteArrayElementAtIndex(i);
                            break;
                        }

                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Add"))
                    {
                        var idx = arrayProp.arraySize;
                        arrayProp.InsertArrayElementAtIndex(idx);
                        var newEntry = arrayProp.GetArrayElementAtIndex(idx);
                        var k = newEntry.FindPropertyRelative("key");
                        var v = newEntry.FindPropertyRelative("value");
                        if (k != null) ClearSerializedProperty(k);
                        if (v != null) ClearSerializedProperty(v);
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.EndVertical();
                }

                serializedObject.ApplyModifiedProperties();
            });
        }

        private void RenderReorderableArray(SerializedProperty prop, string path, VisualElement parent)
        {


            var header = new VisualElement();
            header.AddToClassList("BoxHeader");
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;

            var title = new Label(prop.displayName) { style = { unityFontStyleAndWeight = FontStyle.Bold } };
            title.style.flexGrow = 1;
            title.AddToClassList("ReorderableLabel");
            title.style.borderLeftColor = TinyInspectorStyles.BorderColor;
            title.style.color = TinyInspectorStyles.LabelColor;

            var count = new Label($"COUNT: {prop.arraySize}");
            count.style.color = TinyInspectorStyles.LabelColor;
            count.style.borderRightColor = TinyInspectorStyles.BorderColor;
            count.AddToClassList("FoldoutCountLabel");

            var chevron = new VisualElement();
            chevron.AddToClassList("BoxFoldoutIcon");
            chevron.style.borderLeftWidth = 0;
            chevron.style.borderRightWidth = 1;

            var foldKey = "reorderable:" + path;
            if (!foldoutStates.TryGetValue(foldKey, out var expanded))
            {
                expanded = true;
                foldoutStates[foldKey] = expanded;
            }

            chevron.style.backgroundImage = (Texture2D)TinyIcons.GetIcon(expanded ? TinyIcon.ChevronUp : TinyIcon.ChevronDown);
            chevron.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            chevron.style.borderRightWidth = 0;
            chevron.style.unityBackgroundImageTintColor = TinyInspectorStyles.LabelColor;

            var container = CreateReorderableListUI(path);
            container.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
            container.AddToClassList("reorderableContainer");

            SetHeaderRounded(header, expanded);

            header.Add(chevron);
            header.Add(title);
            header.Add(count);

            var headerAddBtn = new Button(() =>
            {
                serializedObject.Update();
                var p2 = serializedObject.FindProperty(path);
                if (p2 != null)
                {
                    var idx = p2.arraySize;
                    p2.InsertArrayElementAtIndex(idx);
                    var newEntry = p2.GetArrayElementAtIndex(idx);
                    if (newEntry != null) ClearSerializedProperty(newEntry);
                    serializedObject.ApplyModifiedProperties();
                }

                var parentIndex = parent.IndexOf(container);
                if (parentIndex >= 0)
                {
                    var newContainer = CreateReorderableListUI(path);
                    parent.Insert(parentIndex + 1, newContainer);
                    parent.Remove(container);
                    container = newContainer;
                    count.text = $"COUNT: {serializedObject.FindProperty(path).arraySize}";
                }
            })
            { text = "" };
            headerAddBtn.AddToClassList("FoldoutAddButton");
            headerAddBtn.style.borderTopLeftRadius = 0;
            headerAddBtn.style.borderBottomLeftRadius = 0;
            headerAddBtn.tooltip = "Add";

            var addTex = TinyIcons.GetIcon(TinyIcon.Add) as Texture2D;
            if (addTex != null)
            {
                var addImg = new VisualElement();
                addImg.style.width = 20;
                addImg.style.height = 20;
                addImg.style.backgroundImage = addTex;
                addImg.style.unityBackgroundImageTintColor = TinyInspectorStyles.LabelColor;
                addImg.style.borderTopLeftRadius = 0;
                addImg.style.borderBottomLeftRadius = 0;
                headerAddBtn.Add(addImg);
            }

            header.Add(headerAddBtn);

            header.AddManipulator(new Clickable(() =>
            {
                var cur = !foldoutStates[foldKey];
                foldoutStates[foldKey] = cur;

                container.style.display = cur ? DisplayStyle.Flex : DisplayStyle.None;

                var tx = TinyIcons.GetIcon(cur ? TinyIcon.ChevronUp : TinyIcon.ChevronDown);
                if (tx != null) chevron.style.backgroundImage = (Texture2D)tx;

                SetHeaderRounded(header, cur);
            }));

            parent.Add(header);
            parent.Add(container);
        }

        private void RenderTableListArrayAsReorderable(SerializedProperty prop, string path, VisualElement parent)
        {
            var header = new VisualElement();
            header.AddToClassList("BoxHeader");
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;

            var title = new Label(prop.displayName) { style = { unityFontStyleAndWeight = FontStyle.Bold } };
            title.style.flexGrow = 1;
            title.AddToClassList("ReorderableLabel");
            title.style.borderLeftColor = TinyInspectorStyles.BorderColor;
            title.style.color = TinyInspectorStyles.LabelColor;

            var count = new Label($"COUNT: {prop.arraySize}");
            count.style.color = TinyInspectorStyles.LabelColor;
            count.style.borderRightColor = TinyInspectorStyles.BorderColor;
            count.AddToClassList("FoldoutCountLabel");

            var chevron = new VisualElement();
            chevron.AddToClassList("BoxFoldoutIcon");
            chevron.style.borderLeftWidth = 0;
            chevron.style.borderRightWidth = 1;

            var foldKey = "reorderable:" + path;
            if (!foldoutStates.TryGetValue(foldKey, out var expanded))
            {
                expanded = true;
                foldoutStates[foldKey] = expanded;
            }

            chevron.style.backgroundImage = (Texture2D)TinyIcons.GetIcon(expanded ? TinyIcon.ChevronUp : TinyIcon.ChevronDown);
            chevron.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            chevron.style.borderRightWidth = 0;
            chevron.style.unityBackgroundImageTintColor = TinyInspectorStyles.LabelColor;

            var container = CreateTableListAsReorderableListUI(path);
            container.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
            container.AddToClassList("reorderableContainer");

            SetHeaderRounded(header, expanded);

            header.Add(chevron);
            header.Add(title);
            header.Add(count);

            var headerAddBtn = new Button(() =>
            {
                serializedObject.Update();
                var p2 = serializedObject.FindProperty(path);
                if (p2 != null)
                {
                    var idx = p2.arraySize;
                    p2.InsertArrayElementAtIndex(idx);
                    var newEntry = p2.GetArrayElementAtIndex(idx);
                    if (newEntry != null) ClearSerializedProperty(newEntry);
                    serializedObject.ApplyModifiedProperties();
                }

                var parentIndex = parent.IndexOf(container);
                if (parentIndex >= 0)
                {
                    var newContainer = CreateTableListAsReorderableListUI(path);
                    parent.Insert(parentIndex + 1, newContainer);
                    parent.Remove(container);
                    container = newContainer;
                    count.text = $"COUNT: {serializedObject.FindProperty(path).arraySize}";
                }
            })
            { text = "" };
            headerAddBtn.AddToClassList("FoldoutAddButton");
            headerAddBtn.tooltip = "Add";

            var addTex = TinyIcons.GetIcon(TinyIcon.Add) as Texture2D;
            if (addTex != null)
            {
                var addImg = new VisualElement();
                addImg.style.width = 20;
                addImg.style.height = 20;
                addImg.style.backgroundImage = addTex;
                addImg.style.unityBackgroundImageTintColor = TinyInspectorStyles.LabelColor;
                addImg.style.borderTopLeftRadius = 0;
                addImg.style.borderBottomLeftRadius = 0;
                headerAddBtn.Add(addImg);
            }

            header.Add(headerAddBtn);

            header.AddManipulator(new Clickable(() =>
            {
                var cur = !foldoutStates[foldKey];
                foldoutStates[foldKey] = cur;

                container.style.display = cur ? DisplayStyle.Flex : DisplayStyle.None;

                var tx = TinyIcons.GetIcon(cur ? TinyIcon.ChevronUp : TinyIcon.ChevronDown);
                if (tx != null) chevron.style.backgroundImage = (Texture2D)tx;

                SetHeaderRounded(header, cur);
            }));

            parent.Add(header);



            parent.Add(container);
        }

        private VisualElement CreateTableListAsReorderableListUI(string path)
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Column;

            var dragging = false;
            var dragFrom = -1;
            var dragTo = -1;

            void ClearDragHighlight()
            {
                foreach (var child in container.Children())
                    child.RemoveFromClassList("DragTarget");
            }

            void Rebuild()
            {
                //Debug.Log($"TinyInspector: CreateTableListAsReorderableListUI.Rebuild path={path}");
                container.Clear();
                serializedObject.Update();
                var p = serializedObject.FindProperty(path);
                if (p == null) return;

                var HeaderRow = new VisualElement();
                HeaderRow.style.flexDirection = FlexDirection.Row;
                HeaderRow.style.alignItems = Align.Center;
                HeaderRow.AddToClassList("TableListHeader");
                container.Add(HeaderRow);

                //HeaderRow.Add(new VisualElement { style = { width = 26, flexShrink = 0 } }); // Drag handle spacer

                // Header cells (based on first element's direct child properties)
                if (p.arraySize > 0)
                {
                    // Space cell for the drag handle column
                    var handleSpacer = new VisualElement();
                    handleSpacer.style.width = 26;
                    handleSpacer.style.height = 25;
                    handleSpacer.style.flexShrink = 0;
                    handleSpacer.AddToClassList("TableListBorderHelper");

                    HeaderRow.Add(handleSpacer);

                    var first = p.GetArrayElementAtIndex(0);

                    if (first != null && first.propertyType == SerializedPropertyType.Generic)
                    {
                        var it = first.Copy();
                        var end = it.GetEndProperty();

                        if (it.NextVisible(true))
                        {
                            while (!SerializedProperty.EqualContents(it, end))
                            {
                                var isDirectChild = it.depth == first.depth + 1 && it.propertyPath.StartsWith(first.propertyPath, StringComparison.Ordinal);
                                if (!isDirectChild)
                                {
                                    if (!it.NextVisible(false))
                                        break;
                                    continue;
                                }

                                var headerLabel = new Label(PrettifyName(it.displayName));
                                headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                                headerLabel.style.flexGrow = 1;
                                headerLabel.style.flexShrink = 1;
                                headerLabel.style.flexBasis = new StyleLength(new Length(0f));
                                headerLabel.style.marginLeft = 4;
                                headerLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                                headerLabel.style.height = 25;
                                headerLabel.style.opacity = .7f;
                                headerLabel.AddToClassList("TableListBorderHelper");
                                HeaderRow.Add(headerLabel);

                                if (!it.NextVisible(false))
                                    break;
                            }
                        }
                    }
                    else if (first != null)
                    {
                        var headerLabel = new Label(PrettifyName(first.displayName));
                        headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                        headerLabel.style.flexGrow = 1;
                        headerLabel.style.flexShrink = 1;
                        headerLabel.style.flexBasis = new StyleLength(new Length(0f));
                        headerLabel.style.marginLeft = 4;
                        headerLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                        headerLabel.style.height = 25;
                        headerLabel.style.opacity = .7f;
                        headerLabel.AddToClassList("TableListBorderHelper");
                        HeaderRow.Add(headerLabel);
                    }

                    // Space cell for the delete button column
                    var deleteSpacer = new VisualElement();
                    deleteSpacer.style.width = 26;
                    deleteSpacer.style.height = 25;
                    deleteSpacer.style.flexShrink = 0;
                    HeaderRow.Add(deleteSpacer);
                }

                //HeaderRow.Add(new VisualElement { style = { width = 26, flexShrink = 0 } }); // Delete handle spacer

                for (var i = 0; i < p.arraySize; i++)
                {
                    var idx = i;
                    var element = p.GetArrayElementAtIndex(i);

                    var row = new VisualElement();
                    row.style.flexDirection = FlexDirection.Row;
                    row.style.alignItems = Align.Center;
                    row.AddToClassList("ReorderableRow");

                    // Drag handle
                    var handle = new VisualElement();
                    handle.AddToClassList("DragHandle");
                    handle.tooltip = "Drag to reorder";

                    var handleTex = TinyIcons.GetIcon(TinyIcon.List) as Texture2D;
                    if (handleTex != null)
                    {
                        var handleImg = new VisualElement();
                        handleImg.AddToClassList("DragHandleImage");
                        handleImg.style.backgroundImage = handleTex;
                        handleImg.style.unityBackgroundImageTintColor = TinyInspectorStyles.LabelColor;
                        handle.Add(handleImg);
                    }

                    handle.RegisterCallback<PointerDownEvent>(evt =>
                    {
                        dragging = true;
                        dragFrom = idx;
                        dragTo = idx;
                        evt.StopPropagation();
                    });

                    row.RegisterCallback<PointerEnterEvent>(_ =>
                    {
                        if (dragFrom < 0) return;
                        if (idx == dragTo) return;

                        dragTo = idx;
                        ClearDragHighlight();
                        row.AddToClassList("DragTarget");
                    });

                    row.Add(handle);

                    // One-row, multi-column rendering: each direct child property becomes a cell
                    var content = new VisualElement();
                    content.style.flexDirection = FlexDirection.Row;
                    content.style.flexGrow = 1;

                    void HideLabelInField(PropertyField pf)
                    {
                        if (pf == null) return;
                        pf.label = string.Empty;
                        pf.RegisterCallback<GeometryChangedEvent>(_ =>
                        {
                            var labelEl = pf.Q<Label>(className: "unity-property-field__label");
                            if (labelEl != null)
                                labelEl.style.display = DisplayStyle.None;

                            foreach (var labelTinyEl in pf.Query<TinyLabel>().ToList())
                            {
                                labelTinyEl.style.display = DisplayStyle.None;
                            }
                        });
                    }

                    if (element.propertyType == SerializedPropertyType.Generic)
                    {
                        var it = element.Copy();
                        var end = it.GetEndProperty();

                        if (it.NextVisible(true))
                        {
                            while (!SerializedProperty.EqualContents(it, end))
                            {
                                var isDirectChild = it.depth == element.depth + 1 && it.propertyPath.StartsWith(element.propertyPath, StringComparison.Ordinal);
                                if (!isDirectChild)
                                {
                                    if (!it.NextVisible(false))
                                        break;
                                    continue;
                                }

                                var cellProp = it.Copy();
                                var cellField = new PropertyField(cellProp);
                                cellField.AddToClassList("ReorderablePropertyField");
                                HideLabelInField(cellField);
                                cellField.style.flexGrow = 1;
                                cellField.style.flexShrink = 1;
                                cellField.style.flexBasis = new StyleLength(new Length(0f));
                                try { cellField.Bind(serializedObject); } catch { }
                                content.Add(cellField);

                                if (!it.NextVisible(false))
                                    break;
                            }
                        }

                        // Fallback when no direct children found
                        if (content.childCount == 0)
                        {
                            element.isExpanded = true;
                            var pf = new PropertyField(element);
                            pf.AddToClassList("ReorderablePropertyField");
                            HideLabelInField(pf);
                            pf.style.flexGrow = 1;
                            pf.style.flexShrink = 1;
                            pf.style.flexBasis = new StyleLength(new Length(0f));
                            try { pf.Bind(serializedObject); } catch { }

                            var foldout = pf.Q<Foldout>();
                            if (foldout != null)
                                foldout.style.display = DisplayStyle.None;

                            content.Add(pf);
                        }
                    }
                    else
                    {
                        element.isExpanded = true;
                        var pf = new PropertyField(element);
                        pf.AddToClassList("ReorderablePropertyField");
                        HideLabelInField(pf);
                        pf.style.flexGrow = 1;
                        pf.style.flexShrink = 1;
                        pf.style.flexBasis = new StyleLength(new Length(0f));
                        try { pf.Bind(serializedObject); } catch { }

                        var foldout = pf.Q<Foldout>();
                        if (foldout != null)
                            foldout.style.display = DisplayStyle.None;

                        content.Add(pf);
                    }

                    row.Add(content);

                    // Delete button
                    var btns = new VisualElement();
                    btns.style.flexDirection = FlexDirection.Row;
                    btns.style.alignItems = Align.Center;
                    btns.style.flexShrink = 0;

                    var deleteBtn = new Button(() =>
                    {
                        serializedObject.Update();
                        p.DeleteArrayElementAtIndex(idx);
                        serializedObject.ApplyModifiedProperties();
                        Rebuild();
                    })
                    { text = "" };
                    deleteBtn.AddToClassList("ReorderDeleteButton");
                    deleteBtn.tooltip = "Delete";
                    deleteBtn.style.width = 26;

                    var delTex = TinyIcons.GetIcon(TinyIcon.Delete) as Texture2D;
                    if (delTex != null)
                    {
                        var delImg = new VisualElement();
                        delImg.style.width = 20;
                        delImg.style.height = 20;
                        delImg.style.backgroundImage = delTex;
                        delImg.style.unityBackgroundImageTintColor = TinyInspectorStyles.LabelColor;
                        deleteBtn.Add(delImg);
                    }

                    btns.Add(deleteBtn);
                    row.Add(btns);

                    container.Add(row);
                }
            }

            container.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (!dragging)
                    return;

                if (dragFrom >= 0 && dragTo >= 0 && dragFrom != dragTo)
                {
                    serializedObject.Update();
                    var p2 = serializedObject.FindProperty(path);
                    if (p2 != null)
                    {
                        p2.MoveArrayElement(dragFrom, dragTo);
                        serializedObject.ApplyModifiedProperties();
                    }
                }

                dragging = false;
                dragFrom = -1;
                dragTo = -1;
                ClearDragHighlight();
                evt.StopPropagation();
                Rebuild();
            });

            Rebuild();
            return container;
        }

        private void RenderNonSerializedField(NonSerializedFieldItem nsfi, VisualElement parent)
        {
            VisualElement ve;

            if (nsfi.Field.FieldType == typeof(BigInteger)) ve = TinyBigIntegerDrawer.Create(nsfi.Field, serializedObject.targetObject);
            else if (nsfi.Field.FieldType == typeof(DateTime)) ve = TinyDateTimeDrawer.Create(nsfi.Field, serializedObject.targetObject);
            else if (nsfi.Field.FieldType == typeof(TimeSpan)) ve = TinyTimeSpanDrawer.Create(nsfi.Field, serializedObject.targetObject);
            else if (nsfi.Field.FieldType == typeof(Guid)) ve = TinyGuidDrawer.Create(nsfi.Field, serializedObject.targetObject);
            else if (nsfi.Field.FieldType == typeof(Version)) ve = TinyVersionDrawer.Create(nsfi.Field, serializedObject.targetObject);
            else
            {
                var Warining = InfoBoxElement.Create("Drawer for this field doesn't exist!", $"No drawer for field '{nsfi.Field.Name}' of type '{nsfi.Field.FieldType.Name}'.", InfoBoxType.Warning);
                parent.Add(Warining);
                return;
            }

            ve.AddToClassList("TinyProperty");
            parent.Add(ve);
        }

        private void RenderMethodButton(MethodButtonItem item, VisualElement parent)
        {
            if (item?.Method == null)
                return;

            var targetObj = serializedObject.targetObject;
            if (targetObj == null)
                return;

            var parameters = item.Method.GetParameters();

            object buttonAttr = null;
            foreach (var a in item.Method.GetCustomAttributes(true))
            {
                if (a != null && a.GetType().Name == "ButtonAttribute")
                {
                    buttonAttr = a;
                    break;
                }
            }

            var title = TryGetButtonAttributeLabel(item.Method) ?? PrettifyName(item.Method.Name);
            float height = 32f;
            TinyIcon icon = TinyIcon.None;
            TinyColor color = TinyColor.Default;

            if (buttonAttr != null)
            {
                var attrType = buttonAttr.GetType();

                var heightField = attrType.GetField("height");
                if (heightField != null)
                {
                    var heightVal = heightField.GetValue(buttonAttr);
                    if (heightVal is float f) height = f;
                }

                var iconField = attrType.GetField("icon");
                if (iconField != null)
                {
                    var iconVal = iconField.GetValue(buttonAttr);
                    if (iconVal is TinyIcon ti) icon = ti;
                }

                var colorField = attrType.GetField("color");
                if (colorField != null)
                {
                    var colorVal = colorField.GetValue(buttonAttr);
                    if (colorVal is TinyColor tic) color = tic;
                }
            }

            var btn = new Button(() =>
            {
                try
                {
                    if (parameters.Length == 0)
                    {
                        item.Method.Invoke(targetObj, null);
                    }
                    else
                    {
                        Debug.LogWarning($"[Button] {TryGetButtonAttributeLabel(item.Method) ?? PrettifyName(item.Method.Name)} has parameters - invocation with default values not yet implemented.");
                    }
                }
                catch (TargetInvocationException tie)
                {
                    if (tie.InnerException != null) Debug.LogException(tie.InnerException);
                    else Debug.LogException(tie);
                }
                catch (Exception ex) { Debug.LogException(ex); }
            });

            btn.style.height = height;
            btn.style.justifyContent = Justify.Center;
            btn.AddToClassList("TinyProperty");

            if (color != TinyColor.Default)
            {
                var normal = TinyInspectorStyles.Instance.GetBoxHeaderColor(color);
                var hover = normal * 1.15f;

                btn.style.backgroundColor = normal;

                btn.RegisterCallback<MouseEnterEvent>(_ =>
                {
                    btn.style.backgroundColor = hover;
                });

                btn.RegisterCallback<MouseLeaveEvent>(_ =>
                {
                    btn.style.backgroundColor = normal;
                });

            }

            var btnContent = new VisualElement();
            btnContent.style.flexDirection = FlexDirection.Row;
            btnContent.style.alignItems = Align.Center;
            btnContent.style.justifyContent = Justify.Center;

            if (icon != TinyIcon.None)
            {
                var iconTex = TinyIcons.GetIcon(icon) as Texture2D;
                if (iconTex != null)
                {
                    var iconImg = new Image { image = iconTex };
                    iconImg.style.width = 16;
                    iconImg.style.height = 16;
                    iconImg.style.marginRight = 4;
                    btnContent.Add(iconImg);
                }
            }

            var lblText = new Label(title);
            if (color != TinyColor.Default)
            {
                lblText.style.color = TinyInspectorStyles.Instance.GetAccentTextColor(color);
            }
            btnContent.Add(lblText);

            btn.Add(btnContent);
            parent.Add(btn);

            if (parameters.Length > 0)
            {
                var paramsBox = new VisualElement();
                paramsBox.AddToClassList("ButtonBoxParameters");
                paramsBox.style.backgroundColor = TinyInspectorStyles.Instance.GetBoxContentColor(color);
                paramsBox.style.borderBottomColor = TinyInspectorStyles.BorderColor;
                paramsBox.style.borderRightColor = TinyInspectorStyles.BorderColor;
                paramsBox.style.borderTopColor = TinyInspectorStyles.BorderColor;
                paramsBox.style.borderLeftColor = TinyInspectorStyles.BorderColor;


                paramsBox.Add(InfoBoxElement.Create("Button parameters with parametres are currently disabled.", "Invoking methods with parameters is not yet implemented in Tiny Inspector.", InfoBoxType.Warning));
                // Disabled parameter input UI for now, as invoking with custom values is not yet implemented.
                /*
                foreach (var param in parameters)
                {
                    var paramContainer = new VisualElement();
                    paramContainer.style.flexDirection = FlexDirection.Row;
                    paramContainer.style.alignItems = Align.Center;
                    paramContainer.AddToClassList("TinyProperty");

                    var label = new Label(PrettifyName(param.Name));
                    label.style.minWidth = 120;
                    label.style.marginRight = 4;
                    paramContainer.Add(label);

                    VisualElement fieldElement = CreateParameterField(param);
                    if (fieldElement != null)
                    {
                        fieldElement.style.flexGrow = 1;
                        paramContainer.Add(fieldElement);
                    }
                    else
                    {
                        var unsupportedLabel = new Label($"(unsupported type: {param.ParameterType.Name})");
                        unsupportedLabel.style.flexGrow = 1;
                        paramContainer.Add(unsupportedLabel);
                    }

                    paramsBox.Add(paramContainer);
                }*/

                parent.Add(paramsBox);

                btn.AddToClassList("AttributeButtonWithValues");
            }
            else
            {
                btn.AddToClassList("AttributeButton");
            }
        }

        private VisualElement CreateParameterField(ParameterInfo param)
        {
            var paramType = param.ParameterType;

            if (paramType == typeof(int))
            {
                var field = new IntegerField();
                field.value = 0;
                return field;
            }
            else if (paramType == typeof(float))
            {
                var field = new FloatField();
                field.value = 0f;
                return field;
            }
            else if (paramType == typeof(string))
            {
                var field = new TextField();
                field.value = string.Empty;
                return field;
            }
            else if (paramType == typeof(bool))
            {
                var field = new Toggle();
                field.value = false;
                return field;
            }
            else if (paramType == typeof(UnityEngine.Vector2))
            {
                var field = new Vector2Field();
                field.value = UnityEngine.Vector2.zero;
                return field;
            }
            else if (paramType == typeof(UnityEngine.Vector3))
            {
                var field = new Vector3Field();
                field.value = UnityEngine.Vector3.zero;
                return field;
            }
            else if (paramType == typeof(UnityEngine.Color))
            {
                var field = new ColorField();
                field.value = UnityEngine.Color.white;
                return field;
            }
            else if (typeof(UnityEngine.Object).IsAssignableFrom(paramType))
            {
                var field = new ObjectField();
                field.objectType = paramType;
                field.value = null;
                return field;
            }

            return null;
        }
        private static string TryGetButtonAttributeLabel(MethodInfo method)
        {
            foreach (var a in method.GetCustomAttributes(true))
            {
                if (a == null) continue;

                var atype = a.GetType();
                if (atype.Name != "ButtonAttribute")
                    continue;

                var fld = atype.GetField("label", BindingFlags.Public | BindingFlags.Instance);
                var val = fld?.GetValue(a) as string;
                if (!string.IsNullOrEmpty(val))
                    return val;

                var prop = atype.GetProperty("label", BindingFlags.Public | BindingFlags.Instance);
                val = prop?.GetValue(a) as string;
                if (!string.IsNullOrEmpty(val))
                    return val;

                return null;
            }

            return null;
        }

        private void RenderGroupNode(GroupNode child, VisualElement parent)
        {
            if (child.IsLayoutGroup)
            {
                var container = new VisualElement();
                container.style.flexDirection = child.Layout;
                container.style.flexGrow = 1;
                //container.style.flexBasis = 0;
                container.style.alignItems = Align.Stretch;

                if (child.Layout == FlexDirection.Row)
                    container.AddToClassList("LayoutRow");
                else
                    container.AddToClassList("LayoutColumn");

                parent.Add(container);
                RenderNodeUI(child, container);

                if (child.Layout == FlexDirection.Row)
                {
                    var colChildren = new List<VisualElement>();
                    foreach (var c in container.Children())
                    {
                        //if (c.ClassListContains("LayoutColumn"))
                            colChildren.Add(c);
                    }

                    for (var i = 0; i < colChildren.Count; i++)
                    {
                        colChildren[i].style.marginRight = i < colChildren.Count - 1 ? 2 : 0;
                        colChildren[i].style.flexGrow = 1;
                        colChildren[i].style.flexBasis = 0;
                    }
                }

                return;
            }

            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.borderBottomColor = TinyInspectorStyles.BorderColor;
            header.style.borderRightColor = TinyInspectorStyles.BorderColor;
            header.style.borderTopColor = TinyInspectorStyles.BorderColor;
            header.style.borderLeftColor = TinyInspectorStyles.BorderColor;

            if (!child.HasTabs)
            {
                header.AddToClassList("BoxHeader");
                header.style.backgroundColor = TinyInspectorStyles.Instance.GetBoxHeaderColor(child.Color);
            }

            if (child.IconTexture == null && child.Icon != TinyIcon.None)
                child.IconTexture = TinyIcons.GetIcon(child.Icon);

            if (child.IconTexture != null)
            {
                var img = new VisualElement();
                img.AddToClassList("BoxHeaderIcon");
                img.style.backgroundImage = (Texture2D)child.IconTexture;
                img.style.unityBackgroundImageTintColor = TinyInspectorStyles.LabelColor;
                header.Add(img);
            }

            var box = new VisualElement();
            box.AddToClassList("BoxContent");
            box.style.backgroundColor = TinyInspectorStyles.Instance.GetBoxContentColor(child.Color);
            box.style.borderBottomColor = TinyInspectorStyles.BorderColor;
            box.style.borderRightColor = TinyInspectorStyles.BorderColor;
            box.style.borderTopColor = TinyInspectorStyles.BorderColor;
            box.style.borderLeftColor = TinyInspectorStyles.BorderColor;

            VisualElement tabsContainerForHeader = null;
            VisualElement tabsContentContainer = null;

            var title = new Label(child.Name) { style = { unityFontStyleAndWeight = FontStyle.Bold } };

            if (child.HasTabs)
            {
                var headerTabs = new VisualElement();
                headerTabs.style.flexDirection = FlexDirection.Row;
                headerTabs.AddToClassList("TabBar");

                tabsContentContainer = new VisualElement();
                tabsContentContainer.AddToClassList("TabsContainer");

                if (!tabSelection.ContainsKey(child.FullName))
                    tabSelection[child.FullName] = child.GetFirstTabName() ?? string.Empty;

                foreach (var tabName in child.GetTabNames())
                {
                    var tabHeader = new VisualElement();
                    tabHeader.AddToClassList("BoxHeader");
                    tabHeader.AddToClassList("TabHeader");
                    tabHeader.name = tabName;
                    tabHeader.style.borderBottomWidth = 1;
                    tabHeader.style.borderBottomColor = TinyInspectorStyles.BorderColor;
                    tabHeader.style.borderRightColor = TinyInspectorStyles.BorderColor;
                    tabHeader.style.borderTopColor = TinyInspectorStyles.BorderColor;
                    tabHeader.style.borderLeftColor = TinyInspectorStyles.BorderColor;

                    var tabColor = child.GetTabColor(tabName);
                    tabHeader.style.backgroundColor = TinyInspectorStyles.Instance.GetBoxHeaderColor(tabColor);

                    var tabTex = child.GetTabIconTexture(tabName) as Texture2D;
                    if (tabTex != null)
                    {
                        var tabIconImg = new VisualElement();
                                tabIconImg.style.backgroundImage = tabTex;
                                tabIconImg.style.unityBackgroundImageTintColor = TinyInspectorStyles.LabelColor;
                        tabIconImg.AddToClassList("BoxHeaderIcon");
                        tabHeader.Add(tabIconImg);
                    }
                    else
                    {
                        var tabEnum = child.GetTabIconEnum(tabName);
                        if (tabEnum != TinyIcon.None)
                        {
                            var tex = TinyIcons.GetIcon(tabEnum) as Texture2D;
                            if (tex != null)
                            {
                                var tabIconImg = new VisualElement();
                                tabIconImg.AddToClassList("BoxHeaderIcon");
                                tabIconImg.style.backgroundImage = tex;
                                tabIconImg.style.unityBackgroundImageTintColor = TinyInspectorStyles.LabelColor;
                                tabHeader.Add(tabIconImg);
                            }
                        }
                    }

                    var tabLabel = new Label(tabName) { style = { unityFontStyleAndWeight = FontStyle.Bold, marginLeft = 4 } };
                    tabLabel.style.color = TinyInspectorStyles.LabelColor;
                    tabHeader.Add(tabLabel);

                    if (tabSelection[child.FullName] == tabName)
                    {
                        tabHeader.AddToClassList("TabHeaderSelected");
                        tabHeader.style.backgroundColor = TinyInspectorStyles.Instance.GetBoxContentColor(tabColor);
                        box.style.backgroundColor = TinyInspectorStyles.Instance.GetBoxContentColor(tabColor);
                        tabHeader.style.borderBottomColor = TinyInspectorStyles.Instance.GetBoxContentColor(tabColor); 

                    }

                    var tabContent = new VisualElement { name = tabName };
                    tabContent.AddToClassList("TabContent");
                    tabContent.style.display = tabSelection[child.FullName] == tabName ? DisplayStyle.Flex : DisplayStyle.None;

                    foreach (var ip in child.GetTabItems(tabName))
                    {
                        if (ip is string pth)
                        {
                            RenderSerializedPropertyPath(pth, tabContent);
                        }
                        else if (ip is GroupNode gn)
                        {
                            RenderNodeUI(gn, tabContent);
                        }
                    }

                    tabHeader.AddManipulator(new Clickable(() =>
                    {
                        tabSelection[child.FullName] = tabName;

                        foreach (var h in headerTabs.Children())
                        {
                            var hName = h.name;
                            var hColor = child.GetTabColor(hName);

                            if (hName == tabName)
                            {
                                h.AddToClassList("TabHeaderSelected");
                                h.style.backgroundColor = TinyInspectorStyles.Instance.GetBoxContentColor(hColor);
                                h.style.borderBottomColor = TinyInspectorStyles.Instance.GetBoxContentColor(hColor);
                            }
                            else
                            {
                                h.RemoveFromClassList("TabHeaderSelected");
                                h.style.backgroundColor = TinyInspectorStyles.Instance.GetBoxHeaderColor(hColor);
                                h.style.borderBottomColor = TinyInspectorStyles.BorderColor;
                            }
                        }

                        foreach (var c in tabsContentContainer.Children())
                        {
                            if (c.name == tabName)
                            {
                                c.style.display = DisplayStyle.Flex;
                                box.style.backgroundColor = TinyInspectorStyles.Instance.GetBoxContentColor(child.GetTabColor(c.name));
                            }
                            else
                            {
                                c.style.display = DisplayStyle.None;
                            }
                        }
                    }));

                    headerTabs.Add(tabHeader);
                    tabsContentContainer.Add(tabContent);
                }

                tabsContainerForHeader = headerTabs;
            }
            else
            {
                
                title.style.unityTextAlign = TextAnchor.MiddleLeft;
                title.AddToClassList("ReorderableLabel");
                if(child.IconTexture != null) title.style.borderLeftColor = TinyInspectorStyles.BorderColor;
                if (child.IconTexture != null) title.style.marginLeft = 2;
                title.style.flexGrow = 1;
                title.style.color = TinyInspectorStyles.LabelColor;

                header.Add(title);
            }

            if (child.IsFoldout && !child.HasTabs)
            {
                if (!foldoutStates.TryGetValue(child.FullName, out var expanded))
                {
                    expanded = child.DefaultExpanded;
                    foldoutStates[child.FullName] = expanded;
                }
                title.style.borderRightColor = TinyInspectorStyles.BorderColor;
                title.style.borderRightWidth = 1;

                box.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;

                var chevron = new VisualElement();
                chevron.AddToClassList("BoxFoldoutIcon");
                chevron.style.backgroundImage = (Texture2D)TinyIcons.GetIcon(expanded ? TinyIcon.ChevronUp : TinyIcon.ChevronDown);
                chevron.style.unityBackgroundImageTintColor = TinyInspectorStyles.LabelColor;
                chevron.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
                header.Add(chevron);

                SetHeaderRounded(header, expanded);

                header.AddManipulator(new Clickable(() =>
                {
                    var cur = !foldoutStates[child.FullName];
                    foldoutStates[child.FullName] = cur;

                    box.style.display = cur ? DisplayStyle.Flex : DisplayStyle.None;

                    var tx = TinyIcons.GetIcon(cur ? TinyIcon.ChevronUp : TinyIcon.ChevronDown) as Texture2D;
                    if (tx != null) chevron.style.backgroundImage = tx;

                    SetHeaderRounded(header, cur);
                }));
            }

            if (tabsContainerForHeader != null)
                header.Add(tabsContainerForHeader);

            var boxFixer = new VisualElement();

            boxFixer.Add(header);
            boxFixer.Add(box);
            parent.Add(boxFixer);

            if (tabsContentContainer != null)
                box.Add(tabsContentContainer);
            else
                RenderNodeUI(child, box);
        }

        private VisualElement CreateReorderableListUI(string path)
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Column;

            var dragging = false;
            var dragFrom = -1;
            var dragTo = -1;

            void ClearDragHighlight()
            {
                foreach (var child in container.Children())
                    child.RemoveFromClassList("DragTarget");
            }

            void Rebuild()
            {
                container.Clear();
                serializedObject.Update();
                var p = serializedObject.FindProperty(path);
                if (p == null) return;

                for (var i = 0; i < p.arraySize; i++)
                {
                    var idx = i;
                    var element = p.GetArrayElementAtIndex(i);

                    var row = new VisualElement();
                    row.style.flexDirection = FlexDirection.Row;
                    row.style.alignItems = Align.Center;
                    row.AddToClassList("ReorderableRow");

                    var handle = new VisualElement();
                    handle.AddToClassList("DragHandle");
                    handle.tooltip = "Drag to reorder";

                    var handleTex = TinyIcons.GetIcon(TinyIcon.List) as Texture2D;
                    if (handleTex != null)
                    {
                        var handleImg = new VisualElement();
                        handleImg.AddToClassList("DragHandleImage");
                        handleImg.style.backgroundImage = handleTex;
                        handleImg.style.unityBackgroundImageTintColor = TinyInspectorStyles.LabelColor;
                        handle.Add(handleImg);
                    }

                    handle.RegisterCallback<PointerDownEvent>(evt =>
                    {
                        dragging = true;
                        dragFrom = idx;
                        dragTo = idx;
                        evt.StopPropagation();
                    });

                    row.RegisterCallback<PointerEnterEvent>(_ =>
                    {
                        if (dragFrom < 0) return;
                        if (idx == dragTo) return;

                        dragTo = idx;
                        ClearDragHighlight();
                        row.AddToClassList("DragTarget");
                    });

                    row.Add(handle);

                    element.isExpanded = true;
                    var field = new PropertyField(element);
                    field.label = string.Empty;
                    field.style.flexGrow = 1;
                    field.AddToClassList("ReorderablePropertyField");
                    try { field.Bind(serializedObject); } catch { }

                    var foldout = field.Q<Foldout>();
                    if (foldout != null)
                        foldout.style.display = DisplayStyle.None;

                    var btns = new VisualElement();
                    btns.style.flexDirection = FlexDirection.Row;
                    btns.style.alignItems = Align.Center;
                    btns.style.flexShrink = 0;

                    var deleteBtn = new Button(() =>
                    {
                        serializedObject.Update();
                        p.DeleteArrayElementAtIndex(idx);
                        serializedObject.ApplyModifiedProperties();
                        Rebuild();
                    })
                    { text = "" };
                    deleteBtn.AddToClassList("ReorderDeleteButton");
                    deleteBtn.tooltip = "Delete";
                    deleteBtn.style.width = 26;

                    var delTex = TinyIcons.GetIcon(TinyIcon.Delete) as Texture2D;
                    if (delTex != null)
                    {
                        var delImg = new VisualElement();
                        delImg.style.width = 20;
                        delImg.style.height = 20;
                        delImg.style.backgroundImage = delTex;
                        delImg.style.unityBackgroundImageTintColor = TinyInspectorStyles.LabelColor;
                        deleteBtn.Add(delImg);
                    }

                    btns.Add(deleteBtn);

                    row.Add(field);
                    row.Add(btns);

                    container.Add(row);
                }
            }

            container.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (!dragging)
                    return;

                if (dragFrom >= 0 && dragTo >= 0 && dragFrom != dragTo)
                {
                    serializedObject.Update();
                    var p2 = serializedObject.FindProperty(path);
                    if (p2 != null)
                    {
                        p2.MoveArrayElement(dragFrom, dragTo);
                        serializedObject.ApplyModifiedProperties();
                    }
                }

                dragging = false;
                dragFrom = -1;
                dragTo = -1;
                ClearDragHighlight();
                evt.StopPropagation();
                Rebuild();
            });

            Rebuild();
            return container;
        }

        private FieldInfo GetFieldInfo(String fieldName)
        {
            var targetObj = serializedObject?.targetObject;
            if (targetObj == null || string.IsNullOrEmpty(fieldName))
                return null;

            var type = targetObj.GetType();
            if (!fieldInfoCache.TryGetValue(type, out var map))
            {
                map = new Dictionary<string, FieldInfo>();
                fieldInfoCache[type] = map;
            }

            // If we have a cached value that's non-null, return it. If the cached
            // value is null, try to re-resolve the FieldInfo via reflection. This
            // avoids returning stale nulls from earlier lookups (which can happen
            // across domain reloads or type changes) and ensures attributes are
            // detected correctly when the editor is reloaded.
            if (map.TryGetValue(fieldName, out var fi) && fi != null)
                return fi;

            fi = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            map[fieldName] = fi; // cache the resolved value (may be null)
            return fi;
        }

        private bool HasAttributeByName(FieldInfo fieldInfo, string attributeTypeName)
        {
            if (fieldInfo == null) return false;

            foreach (var a in fieldInfo.GetCustomAttributes(false))
            {
                if (a != null && a.GetType().Name == attributeTypeName)
                    return true;
            }

            return false;
        }

        private void ClearSerializedProperty(SerializedProperty sp)
        {
            if (sp == null) return;

            switch (sp.propertyType)
            {
                case SerializedPropertyType.String:
                    sp.stringValue = string.Empty;
                    break;
                case SerializedPropertyType.Integer:
                    sp.intValue = 0;
                    break;
                case SerializedPropertyType.Boolean:
                    sp.boolValue = false;
                    break;
                case SerializedPropertyType.Float:
                    sp.floatValue = 0f;
                    break;
                case SerializedPropertyType.ObjectReference:
                    sp.objectReferenceValue = null;
                    break;
                case SerializedPropertyType.Enum:
                    sp.enumValueIndex = 0;
                    break;
                default:
                    var it = sp.Copy();
                    var end = it.GetEndProperty();
                    it.NextVisible(true);
                    while (it.NextVisible(false) && !SerializedProperty.EqualContents(it, end))
                        ClearSerializedProperty(it);
                    break;
            }
        }

        private bool HasCustomPropertyDrawerForType(Type targetType)
        {
            if (targetType == null) return false;

            if (customDrawerCache.TryGetValue(targetType, out var cached))
                return cached;

            var result = HasCustomPropertyDrawerForTypeSlow(targetType);
            customDrawerCache[targetType] = result;
            return result;
        }

        private static bool HasCustomPropertyDrawerForTypeSlow(Type targetType)
        {
            try
            {
                var editorAsm = typeof(UnityEditor.Editor).Assembly;
                var scriptAttrUtil = editorAsm.GetType("UnityEditor.ScriptAttributeUtility");
                if (scriptAttrUtil != null)
                {
                    var mi = scriptAttrUtil.GetMethod("GetDrawerTypeForType", BindingFlags.Static | BindingFlags.NonPublic);
                    if (mi != null)
                    {
                        var drawerType = mi.Invoke(null, new object[] { targetType }) as Type;
                        if (drawerType != null) return true;
                    }
                }
            }
            catch { }

            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type[] types;
                    try { types = asm.GetTypes(); }
                    catch { continue; }

                    foreach (var t in types)
                    {
                        if (!typeof(PropertyDrawer).IsAssignableFrom(t)) continue;

                        var attrs = t.GetCustomAttributes(typeof(CustomPropertyDrawer), false);
                        if (attrs == null || attrs.Length == 0) continue;

                        foreach (var a in attrs)
                        {
                            var attrType = a.GetType();
                            var field = attrType.GetField("m_Type", BindingFlags.NonPublic | BindingFlags.Instance);
                            if (field == null) continue;

                            var target = field.GetValue(a) as Type;
                            if (target == null) continue;

                            if (target == targetType)
                                return true;

                            var useForChildrenField = attrType.GetField("m_UseForChildren", BindingFlags.NonPublic | BindingFlags.Instance);
                            if (useForChildrenField != null)
                            {
                                var useForChildren = useForChildrenField.GetValue(a) as bool? ?? false;
                                if (useForChildren && target.IsAssignableFrom(targetType))
                                    return true;
                            }
                        }
                    }
                }
            }
            catch { }

            return false;
        }

        private static void SetHeaderRounded(VisualElement header, bool expanded)
        {
            if (header == null) return;
            if (!expanded) header.AddToClassList("BoxHeaderRounded");
            else header.RemoveFromClassList("BoxHeaderRounded");
        }

        private static string PrettifyName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;

            var sb = new StringBuilder();
            sb.Append(name[0]);

            for (var i = 1; i < name.Length; i++)
            {
                var c = name[i];
                var prev = name[i - 1];
                var next = i + 1 < name.Length ? name[i + 1] : '\0';

                if (c == '_')
                {
                    sb.Append(' ');
                    continue;
                }

                if (char.IsUpper(c) && (char.IsLower(prev) || (char.IsUpper(prev) && char.IsLower(next))))
                    sb.Append(' ');

                sb.Append(c);
            }

            return sb.ToString();
        }

        private class GroupNode
        {
            public string Name;
            public string FullName;
            public TinyIcon Icon = TinyIcon.None;
            public Texture IconTexture;
            public bool IsFoldout;
            public bool DefaultExpanded = true;
            public readonly List<object> Items = new();

            private readonly Dictionary<string, List<object>> tabs = new();
            private readonly Dictionary<string, TinyIcon> tabIcons = new();
            private readonly Dictionary<string, Texture> tabIconTextures = new();
            private readonly Dictionary<string, TinyColor> tabColors = new();

            public bool IsLayoutGroup;
            public FlexDirection Layout = FlexDirection.Column;
            public TinyColor Color = TinyColor.Default;

            public GroupNode(string name, string fullName)
            {
                Name = name;
                FullName = fullName;
            }

            public GroupNode FindChild(string name)
            {
                foreach (var it in Items)
                {
                    if (it is GroupNode g && g.Name == name)
                        return g;
                }
                return null;
            }

            public void AddToTab(string tabName, object item)
            {
                if (!tabs.TryGetValue(tabName, out var list))
                {
                    list = new List<object>();
                    tabs[tabName] = list;
                }

                list.Add(item);
            }

            public void SetTabIconEnum(string tabName, TinyIcon icon) => tabIcons[tabName] = icon;
            public void SetTabIconTexture(string tabName, Texture tex) => tabIconTextures[tabName] = tex;

            public TinyIcon GetTabIconEnum(string tabName) => tabIcons.TryGetValue(tabName, out var ic) ? ic : TinyIcon.None;
            public Texture GetTabIconTexture(string tabName) => tabIconTextures.TryGetValue(tabName, out var tex) ? tex : null;

            public bool HasTabs => tabs.Count > 0;
            public IEnumerable<string> GetTabNames() => tabs.Keys;

            public string GetFirstTabName()
            {
                foreach (var k in tabs.Keys) return k;
                return null;
            }

            public IEnumerable<object> GetTabItems(string tabName) => tabs.TryGetValue(tabName, out var list) ? list : Array.Empty<object>();

            public void SetTabColor(string tabName, TinyColor color) => tabColors[tabName] = color;
            public TinyColor GetTabColor(string tabName) => tabColors.TryGetValue(tabName, out var c) ? c : TinyColor.Default;

            public IEnumerable<GroupNode> Children()
            {
                foreach (var it in Items)
                {
                    if (it is GroupNode g)
                        yield return g;
                }
            }
        }

        private class MethodButtonItem
        {
            public readonly MethodInfo Method;

            public MethodButtonItem(MethodInfo method)
            {
                Method = method;
            }
        }

        private class NonSerializedFieldItem
        {
            public readonly FieldInfo Field;

            public NonSerializedFieldItem(FieldInfo field)
            {
                Field = field;
            }
        }
    }

    [CustomEditor(typeof(ScriptableObject), true)]
    public class MyCustomEditorForScriptableObject : TinyInspectorCustomEditor
    {
    }
}

namespace TinyInspector
{
    public static partial class TinyInspectorStylesProxy { }
}
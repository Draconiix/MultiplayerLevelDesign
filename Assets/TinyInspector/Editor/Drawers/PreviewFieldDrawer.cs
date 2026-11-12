#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TinyInspector
{
    [CustomPropertyDrawer(typeof(PreviewFieldAttribute))]
    public class PreviewFieldDrawer : PropertyDrawer
    {
        private const int PickerControlId = 982341;
        
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var attr = (PreviewFieldAttribute)attribute;
            var targetObject = property.serializedObject.targetObject;
            var propertyPath = property.propertyPath;
            int gridSize = Mathf.Max(1, attr.GridSize);

            float cellSize = EditorGUIUtility.singleLineHeight;
            float previewSize = cellSize * gridSize;

            var root = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center
                }
            };

            // === PREVIEW BOX (LEFT) ===
            var previewBox = new VisualElement
            {
                style =
                {
                    width = previewSize,
                    height = previewSize
                }
            };
            previewBox.AddToClassList("PreviewBox");
            previewBox.style.borderBottomColor = TinyInspectorStyles.BorderColor;
            previewBox.style.borderRightColor = TinyInspectorStyles.BorderColor;
            previewBox.style.borderTopColor = TinyInspectorStyles.BorderColor;
            previewBox.style.borderLeftColor = TinyInspectorStyles.BorderColor;

            var image = new Image
            {
                scaleMode = ScaleMode.ScaleToFit,
                style =
                {
                    width = Length.Percent(100),
                    height = Length.Percent(100)
                }
            };

            previewBox.Add(image);

            // === FIELD (RIGHT) ===
            root.Add(previewBox);

            TinyPropertyField field = null;
            if (attr.ShowField)
            {
                field = new TinyPropertyField(property)
                {
                    style =
                    {
                        flexGrow = 1
                    }
                };

                root.Add(field);
            }

            // Clicking the preview should behave like clicking the object field.
            previewBox.pickingMode = PickingMode.Position;

            bool pickerUpdateHooked = false;
            Object lastPicked = null;
            previewBox.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != (int)MouseButton.LeftMouse)
                    return;

                // If this is an object reference, open the Object Picker like the field selector.
                if (property.propertyType == SerializedPropertyType.ObjectReference)
                {
                    var current = property.objectReferenceValue;

                    // Older Unity versions only expose the generic ShowObjectPicker<T>.
                    // Use the most specific known type so the picker filters correctly.
                    var of = field?.Q<ObjectField>();
                    var objType = of?.objectType ?? current?.GetType();
                    if (objType == typeof(Sprite))
                        EditorGUIUtility.ShowObjectPicker<Sprite>(current, false, string.Empty, PickerControlId);
                    else if (typeof(Texture).IsAssignableFrom(objType))
                        EditorGUIUtility.ShowObjectPicker<Texture>(current, false, string.Empty, PickerControlId);
                    else if (objType == typeof(Material))
                        EditorGUIUtility.ShowObjectPicker<Material>(current, false, string.Empty, PickerControlId);
                    else if (objType == typeof(GameObject))
                        EditorGUIUtility.ShowObjectPicker<GameObject>(current, false, string.Empty, PickerControlId);
                    else if (objType == typeof(Component) || (objType != null && typeof(Component).IsAssignableFrom(objType)))
                        EditorGUIUtility.ShowObjectPicker<Component>(current, false, string.Empty, PickerControlId);
                    else
                        EditorGUIUtility.ShowObjectPicker<Object>(current, false, string.Empty, PickerControlId);

                    // Robust approach: poll the Object Picker via EditorApplication.update.
                    // UI Toolkit command events are not reliably routed to this VisualElement in all Unity versions.
                    if (!pickerUpdateHooked)
                    {
                        pickerUpdateHooked = true;

                        void ApplyPicked(Object picked)
                        {
                            if (targetObject == null)
                                return;

                            var so = new SerializedObject(targetObject);
                            var sp = so.FindProperty(propertyPath);
                            if (sp == null)
                                return;

                            so.Update();
                            sp.objectReferenceValue = picked;
                            so.ApplyModifiedProperties();

                            UpdatePreview();
                            EditorApplication.delayCall += UpdatePreview;
                        }

                        void Tick()
                        {
                            if (EditorGUIUtility.GetObjectPickerControlID() != PickerControlId)
                            {
                                // Picker closed or focus moved away.
                                if (lastPicked != null)
                                {
                                    ApplyPicked(lastPicked);
                                    lastPicked = null;
                                }

                                EditorApplication.update -= Tick;
                                pickerUpdateHooked = false;
                                return;
                            }

                            var picked = EditorGUIUtility.GetObjectPickerObject();
                            if (picked == lastPicked)
                                return;

                            lastPicked = picked;
                            ApplyPicked(picked);
                        }

                        EditorApplication.update -= Tick;
                        EditorApplication.update += Tick;
                    }
                    evt.StopPropagation();
                    return;
                }

                var objectField = field?.Q<ObjectField>();
                if (objectField != null)
                {
                    objectField.Focus();
                    using var forwarded = MouseDownEvent.GetPooled(evt);
                    forwarded.target = objectField;
                    objectField.SendEvent(forwarded);
                }
                else
                {
                    field?.Focus();
                }

                evt.StopPropagation();
            });

            void UpdatePreview()
            {
                // Use a fresh SerializedObject/property to avoid stale references.
                Object obj = null;
                if (targetObject != null)
                {
                    var so = new SerializedObject(targetObject);
                    var sp = so.FindProperty(propertyPath);
                    obj = sp != null ? sp.objectReferenceValue : null;
                }
                Texture tex = null;

                if (obj == null)
                {
                    image.image = null;
                    image.tooltip = "No preview";
                    return;
                }

                if (obj is Sprite sprite)
                {
                    tex = sprite.texture;
                }
                else if (obj is Texture texture)
                {
                    tex = texture;
                }
                else
                {
                    tex = AssetPreview.GetAssetPreview(obj);
                }

                image.image = tex;
            }

            UpdatePreview();

            if (field != null)
            {
                field.RegisterCallback<ChangeEvent<Object>>(evt =>
                {
                    property.serializedObject.ApplyModifiedProperties();
                    UpdatePreview();
                });
            }

            return root;
        }
    }
}
#endif

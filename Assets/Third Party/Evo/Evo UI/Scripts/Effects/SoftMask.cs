using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Evo.UI
{
    /// <summary>
    /// Applies a soft mask to child Graphic elements.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [HelpURL(Constants.HelpUrl)]
    [AddComponentMenu("Evo/UI/Effects/Soft Mask")]
    [RequireComponent(typeof(Graphic), typeof(RectTransform))]
    public class SoftMask : UIBehaviour, IMeshModifier, ICanvasRaycastFilter
    {
        [Tooltip("Should the graphic serving as the mask be drawn?")]
        [SerializeField] private bool showMaskGraphic = true;

        // Constants
        public const int MaxMaskDepth = 4;

        // Cache
        Graphic maskGraphic;
        RectTransform rectTransform;
        readonly Dictionary<MaterialKey, MaterialEntry> materialCache = new();
        readonly List<MaterialKey> staleMaterialKeys = new();
        readonly List<Graphic> graphics = new();
        readonly List<SoftMaskable> assignedMaskables = new();
        readonly List<SoftMask> nestedMasks = new();
        readonly SoftMask[] maskStack = new SoftMask[MaxMaskDepth];
        int maskCount;
        int stackVersion;
        int dataVersion;
        int lastHierarchyCount;
        bool didWarnMaskLimit;
        bool hasTransformSnapshot;
        Rect lastRect;
        Matrix4x4 lastWorldToLocal;
        Texture lastMaskTexture;

        // Shader Serialization
        [SerializeField, HideInInspector] Shader embeddedShader;
        [SerializeField, HideInInspector] Shader embeddedTMPShader;
        [SerializeField, HideInInspector] Shader embeddedTMPMobileShader;
        [SerializeField, HideInInspector] Shader embeddedTMPBitmapShader;
        const string ShaderName = "Hidden/Evo/UI/Soft Mask";
        const string TMPShaderName = "Hidden/Evo/UI/Soft Mask TMP";
        const string TMPMobileShaderName = "Hidden/Evo/UI/Soft Mask TMP Mobile";
        const string TMPBitmapShaderName = "Hidden/Evo/UI/Soft Mask TMP Bitmap";
        const string TMPBitmapMobileKeyword = "EVO_TMP_BITMAP_MOBILE";
        const string TMPSpriteKeyword = "EVO_TMP_SPRITE";

        // Shader Property IDs
        static readonly int PropsSupport = Shader.PropertyToID("_SoftMaskSupport");
        static readonly int PropsCount = Shader.PropertyToID("_SoftMask_Count");
        static readonly int PropsCanvasToLocalX = Shader.PropertyToID("_SoftMask_CanvasToLocalX");
        static readonly int PropsCanvasToLocalY = Shader.PropertyToID("_SoftMask_CanvasToLocalY");
        static readonly int PropsRect = Shader.PropertyToID("_SoftMask_Rect");
        static readonly int PropsData = Shader.PropertyToID("_SoftMask_Data");
        static readonly int PropsPRRect = Shader.PropertyToID("_SoftMask_PRRect");
        static readonly int PropsPRRadii = Shader.PropertyToID("_SoftMask_PRRadii");
        static readonly int PropsPRFillData = Shader.PropertyToID("_SoftMask_PRFillData");
        static readonly int PropsBorderData = Shader.PropertyToID("_SoftMask_BorderData");
        static readonly int PropsUVOuter = Shader.PropertyToID("_SoftMask_UVOuter");
        static readonly int PropsUVInner = Shader.PropertyToID("_SoftMask_UVInner");
        static readonly int PropsMainTex = Shader.PropertyToID("_MainTex");
        static readonly int[] PropsTextures =
        {
            Shader.PropertyToID("_SoftMaskTex0"),
            Shader.PropertyToID("_SoftMaskTex1"),
            Shader.PropertyToID("_SoftMaskTex2"),
            Shader.PropertyToID("_SoftMaskTex3")
        };

        static readonly Vector4[] CanvasToLocalX = new Vector4[MaxMaskDepth];
        static readonly Vector4[] CanvasToLocalY = new Vector4[MaxMaskDepth];
        static readonly Vector4[] MaskRects = new Vector4[MaxMaskDepth];
        static readonly Vector4[] MaskData = new Vector4[MaxMaskDepth];
        static readonly Vector4[] ProceduralRects = new Vector4[MaxMaskDepth];
        static readonly Vector4[] ProceduralRadii = new Vector4[MaxMaskDepth];
        static readonly Vector4[] ProceduralFillData = new Vector4[MaxMaskDepth];
        static readonly Vector4[] BorderData = new Vector4[MaxMaskDepth];
        static readonly Vector4[] UVOuter = new Vector4[MaxMaskDepth];
        static readonly Vector4[] UVInner = new Vector4[MaxMaskDepth];

        public bool ShowMaskGraphic
        {
            get => showMaskGraphic;
            set
            {
                if (showMaskGraphic == value)
                    return;

                showMaskGraphic = value;
                RefreshMaskGraphic();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            rectTransform = GetComponent<RectTransform>();
            maskGraphic = GetComponent<Graphic>();

            LoadShaders();
            RegisterGraphicCallbacks();
            RebuildMaskStack();
            RefreshHierarchy();
            RefreshMaskGraphic();
            lastHierarchyCount = transform.hierarchyCount;

            Canvas.willRenderCanvases += UpdateMaskProperties;
        }

        protected override void OnDisable()
        {
            Canvas.willRenderCanvases -= UpdateMaskProperties;
            UnregisterGraphicCallbacks();

            RefreshDescendantMaskStacks();
            RefreshAssignedMaskables();
            NotifyChildren();
            ClearCache();

            if (maskGraphic != null)
            {
                maskGraphic.SetVerticesDirty();
                maskGraphic.SetMaterialDirty();
            }

            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            Canvas.willRenderCanvases -= UpdateMaskProperties;
            UnregisterGraphicCallbacks();
            ClearCache();

            base.OnDestroy();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            MarkMaskPropertiesDirty();
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            RebuildMaskStack();
            RefreshHierarchy();
        }

        protected void OnTransformChildrenChanged() => RefreshHierarchy();

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            RebuildMaskStack();
            ClearCache();
            RefreshHierarchy();

            if (Application.IsPlaying(gameObject))
                NotifyChildren();
        }

        protected override void OnDidApplyAnimationProperties()
        {
            base.OnDidApplyAnimationProperties();
            MarkMaskPropertiesDirty();
        }

        void LoadShaders()
        {
            if (embeddedShader == null) { embeddedShader = Shader.Find(ShaderName); }
            if (embeddedTMPShader == null) { embeddedTMPShader = Shader.Find(TMPShaderName); }
            if (embeddedTMPMobileShader == null) { embeddedTMPMobileShader = Shader.Find(TMPMobileShaderName); }
            if (embeddedTMPBitmapShader == null) { embeddedTMPBitmapShader = Shader.Find(TMPBitmapShaderName); }
        }

        void RegisterGraphicCallbacks()
        {
            if (maskGraphic == null)
                return;

            maskGraphic.RegisterDirtyMaterialCallback(MarkMaskPropertiesDirty);
            maskGraphic.RegisterDirtyVerticesCallback(MarkMaskPropertiesDirty);
        }

        void UnregisterGraphicCallbacks()
        {
            if (maskGraphic == null)
                return;

            maskGraphic.UnregisterDirtyMaterialCallback(MarkMaskPropertiesDirty);
            maskGraphic.UnregisterDirtyVerticesCallback(MarkMaskPropertiesDirty);
        }

        void MarkMaskPropertiesDirty()
        {
            unchecked { dataVersion++; }
        }

        void RefreshDataVersion()
        {
            if (rectTransform == null || maskGraphic == null)
                return;

            Rect currentRect = maskGraphic.GetPixelAdjustedRect();
            Matrix4x4 currentWorldToLocal = rectTransform.worldToLocalMatrix;
            Texture currentMaskTexture = maskGraphic.mainTexture;

            if (hasTransformSnapshot && currentRect == lastRect && currentWorldToLocal == lastWorldToLocal && currentMaskTexture == lastMaskTexture)
                return;

            hasTransformSnapshot = true;
            lastRect = currentRect;
            lastWorldToLocal = currentWorldToLocal;
            lastMaskTexture = currentMaskTexture;

            unchecked { dataVersion++; }
        }

        void RebuildMaskStack()
        {
            for (int i = 0; i < maskStack.Length; i++)
                maskStack[i] = null;

            maskCount = 0;
            bool exceededLimit = false;
            Transform current = transform;

            while (current != null)
            {
                if (current.TryGetComponent(out SoftMask mask) && mask.isActiveAndEnabled)
                {
                    if (maskCount < MaxMaskDepth)
                    {
                        maskStack[maskCount] = mask;
                        maskCount++;
                    }
                    else
                    {
                        exceededLimit = true;
                    }
                }

                current = current.parent;
            }

            unchecked { stackVersion++; }

            if (!exceededLimit)
            {
                didWarnMaskLimit = false;
                return;
            }

            if (!didWarnMaskLimit)
            {
                didWarnMaskLimit = true;
                Debug.LogWarning($"Soft Mask supports up to {MaxMaskDepth} nested masks. The nearest {MaxMaskDepth} masks will be used.", this);
            }
        }

        void RefreshHierarchy()
        {
            RefreshAssignedMaskables();
            RefreshDescendantMaskStacks();
            EnsureMaskables();

            lastHierarchyCount = transform.hierarchyCount;
        }

        void RefreshDescendantMaskStacks()
        {
            GetComponentsInChildren(true, nestedMasks);
            for (int i = 0; i < nestedMasks.Count; i++)
            {
                SoftMask mask = nestedMasks[i];
                if (mask != null && mask != this)
                    mask.RebuildMaskStack();
            }
        }

        void NotifyChildren()
        {
            GetComponentsInChildren(true, graphics);
            for (int i = 0; i < graphics.Count; i++)
            {
                Graphic graphic = graphics[i];
                if (graphic == null || graphic.gameObject == gameObject)
                    continue;

                graphic.SetMaterialDirty();
            }
        }

        void EnsureMaskables()
        {
            GetComponentsInChildren(true, graphics);
            for (int i = 0; i < graphics.Count; i++)
            {
                Graphic graphic = graphics[i];
                if (graphic == null || graphic.gameObject == gameObject)
                    continue;

                if (!graphic.TryGetComponent(out SoftMaskable maskable))
                    maskable = graphic.gameObject.AddComponent<SoftMaskable>();

                maskable.Initialize(graphic);
            }
        }

        void RefreshAssignedMaskables()
        {
            for (int i = assignedMaskables.Count - 1; i >= 0; i--)
            {
                SoftMaskable maskable = assignedMaskables[i];
                if (maskable == null)
                {
                    assignedMaskables.RemoveAt(i);
                    continue;
                }

                maskable.RefreshMask();
            }
        }

        internal void RegisterMaskable(SoftMaskable maskable)
        {
            if (maskable != null && !assignedMaskables.Contains(maskable))
                assignedMaskables.Add(maskable);
        }

        internal void UnregisterMaskable(SoftMaskable maskable)
        {
            if (maskable != null)
                assignedMaskables.Remove(maskable);
        }

        /// <summary>
        /// Refreshes dynamically created or reparented UI descendants immediately.
        /// </summary>
        public void Refresh()
        {
            RebuildMaskStack();
            RefreshHierarchy();
            MarkMaskPropertiesDirty();
        }

        int GetMaterialDataVersion()
        {
            while (true)
            {
                if (maskCount == 0 || maskStack[0] != this || !isActiveAndEnabled)
                    RebuildMaskStack();

                int currentVersion = stackVersion;
                bool stackInvalid = false;

                for (int i = 0; i < maskCount; i++)
                {
                    SoftMask mask = maskStack[i];

                    if (mask == null || !mask.isActiveAndEnabled)
                    {
                        stackInvalid = true;
                        break;
                    }

                    mask.RefreshDataVersion();
                    unchecked { currentVersion = currentVersion * 397 ^ mask.dataVersion; }
                }

                if (!stackInvalid)
                    return currentVersion;

                RebuildMaskStack();
            }
        }

        void SetMaterialProperties(MaterialEntry entry, bool force, int currentVersion)
        {
            if (entry == null || entry.ModifiedMaterial == null)
                return;

            Matrix4x4 canvasToWorld = entry.Canvas != null ? entry.Canvas.transform.localToWorldMatrix : Matrix4x4.identity;

            if (!force && entry.AppliedVersion == currentVersion && entry.HasCanvasSnapshot && entry.CanvasToWorld == canvasToWorld)
                return;

            for (int i = 0; i < maskCount; i++)
                maskStack[i].WriteMaterialData(i, canvasToWorld, entry.ModifiedMaterial);

            entry.ModifiedMaterial.SetFloat(PropsCount, maskCount);
            entry.ModifiedMaterial.SetVectorArray(PropsCanvasToLocalX, CanvasToLocalX);
            entry.ModifiedMaterial.SetVectorArray(PropsCanvasToLocalY, CanvasToLocalY);
            entry.ModifiedMaterial.SetVectorArray(PropsRect, MaskRects);
            entry.ModifiedMaterial.SetVectorArray(PropsData, MaskData);
            entry.ModifiedMaterial.SetVectorArray(PropsPRRect, ProceduralRects);
            entry.ModifiedMaterial.SetVectorArray(PropsPRRadii, ProceduralRadii);
            entry.ModifiedMaterial.SetVectorArray(PropsPRFillData, ProceduralFillData);
            entry.ModifiedMaterial.SetVectorArray(PropsBorderData, BorderData);
            entry.ModifiedMaterial.SetVectorArray(PropsUVOuter, UVOuter);
            entry.ModifiedMaterial.SetVectorArray(PropsUVInner, UVInner);

            entry.AppliedVersion = currentVersion;
            entry.CanvasToWorld = canvasToWorld;
            entry.HasCanvasSnapshot = true;
        }

        void WriteMaterialData(int index, Matrix4x4 canvasToWorld, Material material)
        {
            if (maskGraphic == null || rectTransform == null)
                return;

            Rect rect = rectTransform.rect;
            float mode = 0f;
            float modeDataY = 0f;
            float modeDataZ = 0f;
            float modeDataW = 0f;
            Texture maskTexture = maskGraphic.mainTexture != null ? maskGraphic.mainTexture : Texture2D.whiteTexture;
            Vector4 uvOuter = new(0f, 0f, 1f, 1f);
            Vector4 uvInner = uvOuter;
            Vector4 borderData = Vector4.zero;
            Vector4 proceduralRect = Vector4.zero;
            Vector4 proceduralRadii = Vector4.zero;
            Vector4 proceduralFillData = Vector4.zero;

            if (maskGraphic is ProceduralRect proceduralMask)
            {
                mode = 2f;
                rect = GetProceduralRectDrawingRect(proceduralMask);
                Vector2 halfSize = rect.size * 0.5f;

                proceduralRect = new Vector4(rect.center.x, rect.center.y, halfSize.x, halfSize.y);
                proceduralRadii = proceduralMask.GetRadiiPixels(rect);
                modeDataY = Mathf.Max(0f, proceduralMask.softness);
                proceduralFillData = new Vector4(Mathf.Clamp01(proceduralMask.clipAmount), GetPackedClipConfig(proceduralMask),
                    0f, UsesSquircleCorners(proceduralMask) ? 1f : 0f);
                maskTexture = Texture2D.whiteTexture;
            }
            else if (maskGraphic is Image image)
            {
                Sprite sprite = image.overrideSprite != null ? image.overrideSprite : image.sprite;
                rect = image.GetPixelAdjustedRect();

                if (sprite != null)
                {
                    uvOuter = UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);

                    if (image.type == Image.Type.Simple)
                    {
                        rect = GetImageDrawingRect(image, sprite, image.preserveAspect);
                    }
                    else if (image.type == Image.Type.Sliced)
                    {
                        if (sprite.border.sqrMagnitude > 0f)
                        {
                            mode = 1f;
                            modeDataY = image.fillCenter ? 1f : 0f;
                            GetSlicedImageData(image, sprite, ref rect, out borderData);
                            uvInner = UnityEngine.Sprites.DataUtility.GetInnerUV(sprite);
                        }
                        else
                        {
                            rect = GetImageDrawingRect(image, sprite, false);
                        }
                    }
                    else if (image.type == Image.Type.Filled)
                    {
                        mode = 3f;
                        rect = GetImageDrawingRect(image, sprite, image.preserveAspect);
                        float packedFill = (int)image.fillMethod + Mathf.Clamp(image.fillOrigin, 0, 3) * 8 + (image.fillClockwise ? 64 : 0);
                        proceduralFillData = new Vector4(Mathf.Clamp01(image.fillAmount), packedFill, 0f, 0f);
                    }
                    else if (image.type == Image.Type.Tiled)
                    {
                        mode = 4f;
                        modeDataY = image.fillCenter ? 1f : 0f;
                        GetTiledImageData(image, sprite, rect, out borderData, out modeDataZ, out modeDataW);
                        uvInner = UnityEngine.Sprites.DataUtility.GetInnerUV(sprite);
                    }
                }
            }
            else if (maskGraphic is RawImage rawImage)
            {
                rect = rawImage.GetPixelAdjustedRect();
                Rect uvRect = rawImage.uvRect;
                uvOuter = new Vector4(uvRect.xMin, uvRect.yMin, uvRect.xMax, uvRect.yMax);
                uvInner = uvOuter;
            }

            Matrix4x4 canvasToLocal = rectTransform.worldToLocalMatrix * canvasToWorld;

            CanvasToLocalX[index] = canvasToLocal.GetRow(0);
            CanvasToLocalY[index] = canvasToLocal.GetRow(1);
            MaskRects[index] = new Vector4(rect.xMin, rect.yMin, rect.width, rect.height);
            MaskData[index] = new Vector4(mode, modeDataY, modeDataZ, modeDataW);
            ProceduralRects[index] = proceduralRect;
            ProceduralRadii[index] = proceduralRadii;
            ProceduralFillData[index] = proceduralFillData;
            BorderData[index] = borderData;
            UVOuter[index] = uvOuter;
            UVInner[index] = uvInner;

            if (material.HasTexture(PropsTextures[index]))
                material.SetTexture(PropsTextures[index], maskTexture);
        }

        static Rect GetProceduralRectDrawingRect(ProceduralRect proceduralRect)
        {
            Rect rect = proceduralRect.rectTransform.rect;
            Sprite sprite = proceduralRect.sprite;

            if (sprite == null || proceduralRect.scaleMode != ProceduralRect.ScaleMode.Fit)
                return rect;

            Rect spriteRect = sprite.rect;
            if (spriteRect.width <= 0.001f || spriteRect.height <= 0.001f || rect.height <= 0.001f)
                return rect;

            float spriteAspect = spriteRect.width / spriteRect.height;
            float rectAspect = rect.width / rect.height;

            if (spriteAspect > rectAspect)
            {
                float newHeight = rect.width / spriteAspect;
                return new Rect(rect.x, rect.center.y - newHeight * 0.5f, rect.width, newHeight);
            }

            float newWidth = rect.height * spriteAspect;
            return new Rect(rect.center.x - newWidth * 0.5f, rect.y, newWidth, rect.height);
        }

        static float GetPackedClipConfig(ProceduralRect proceduralRect)
        {
            int method = (int)proceduralRect.clipMethod;
            int maxOrigin = proceduralRect.clipMethod switch
            {
                ProceduralRect.ClipMethod.Horizontal => 1,
                ProceduralRect.ClipMethod.Vertical => 1,
                ProceduralRect.ClipMethod.Radial360 => 3,
                _ => 0
            };
            int origin = Mathf.Clamp(proceduralRect.clipOrigin, 0, maxOrigin);
            int clockwise = proceduralRect.clipClockwise ? 1 : 0;
            return method + origin * 8 + clockwise * 64;
        }

        static bool UsesSquircleCorners(ProceduralRect proceduralRect)
        {
            if (proceduralRect.radiusSyncMode == ProceduralRect.RadiusSyncMode.None)
                return proceduralRect.squircleCorners;

            ProceduralRect target = proceduralRect.radiusSyncMode == ProceduralRect.RadiusSyncMode.MatchParent
                ? (proceduralRect.transform.parent != null ? proceduralRect.transform.parent.GetComponent<ProceduralRect>() : null)
                : proceduralRect.radiusSyncTarget;

            return target != null && target != proceduralRect ? target.squircleCorners : proceduralRect.squircleCorners;
        }

        static Rect GetImageDrawingRect(Image image, Sprite sprite, bool preserveAspect)
        {
            Rect rect = image.GetPixelAdjustedRect();
            Vector2 size = sprite.rect.size;

            if (preserveAspect && size.x > 0f && size.y > 0f && rect.width > 0f && rect.height > 0f)
            {
                float spriteRatio = size.x / size.y;
                float rectRatio = rect.width / rect.height;

                if (spriteRatio > rectRatio)
                {
                    float oldHeight = rect.height;
                    rect.height = rect.width / spriteRatio;
                    rect.y += (oldHeight - rect.height) * image.rectTransform.pivot.y;
                }
                else
                {
                    float oldWidth = rect.width;
                    rect.width = rect.height * spriteRatio;
                    rect.x += (oldWidth - rect.width) * image.rectTransform.pivot.x;
                }
            }

            int spriteWidth = Mathf.RoundToInt(size.x);
            int spriteHeight = Mathf.RoundToInt(size.y);
            if (spriteWidth <= 0 || spriteHeight <= 0)
                return rect;

            Vector4 padding = UnityEngine.Sprites.DataUtility.GetPadding(sprite);
            float xMin = padding.x / spriteWidth;
            float yMin = padding.y / spriteHeight;
            float xMax = (spriteWidth - padding.z) / spriteWidth;
            float yMax = (spriteHeight - padding.w) / spriteHeight;

            return Rect.MinMaxRect(rect.x + rect.width * xMin, rect.y + rect.height * yMin,
                rect.x + rect.width * xMax, rect.y + rect.height * yMax);
        }

        static Vector4 GetAdjustedBorders(RectTransform target, Vector4 border, Rect adjustedRect)
        {
            Rect originalRect = target.rect;

            for (int axis = 0; axis <= 1; axis++)
            {
                if (originalRect.size[axis] != 0f)
                {
                    float borderScaleRatio = adjustedRect.size[axis] / originalRect.size[axis];
                    border[axis] *= borderScaleRatio;
                    border[axis + 2] *= borderScaleRatio;
                }

                float combinedBorders = border[axis] + border[axis + 2];
                if (adjustedRect.size[axis] < combinedBorders && combinedBorders != 0f)
                {
                    float borderScaleRatio = adjustedRect.size[axis] / combinedBorders;
                    border[axis] *= borderScaleRatio;
                    border[axis + 2] *= borderScaleRatio;
                }
            }

            return border;
        }

        static void GetSlicedImageData(Image image, Sprite sprite, ref Rect rect, out Vector4 borderData)
        {
            float pixelsPerUnit = image.pixelsPerUnit * image.pixelsPerUnitMultiplier;
            if (pixelsPerUnit <= 0f) { pixelsPerUnit = 1f; }

            Vector4 border = GetAdjustedBorders(image.rectTransform, sprite.border / pixelsPerUnit, rect);
            Vector4 padding = UnityEngine.Sprites.DataUtility.GetPadding(sprite) / pixelsPerUnit;
            float paddedWidth = Mathf.Max(0f, rect.width - padding.x - padding.z);
            float paddedHeight = Mathf.Max(0f, rect.height - padding.y - padding.w);

            borderData = new Vector4(Mathf.Clamp(border.x - padding.x, 0f, paddedWidth), Mathf.Clamp(border.y - padding.y, 0f, paddedHeight),
                Mathf.Clamp(rect.width - border.z - padding.x, 0f, paddedWidth), Mathf.Clamp(rect.height - border.w - padding.y, 0f, paddedHeight));
            rect = new Rect(rect.x + padding.x, rect.y + padding.y, paddedWidth, paddedHeight);
        }

        static void GetTiledImageData(Image image, Sprite sprite, Rect rect, out Vector4 borderData, out float tileWidth, out float tileHeight)
        {
            float pixelsPerUnit = image.pixelsPerUnit * image.pixelsPerUnitMultiplier;
            if (pixelsPerUnit <= 0f) { pixelsPerUnit = 1f; }

            Vector4 spriteBorder = sprite.border;
            Vector4 border = GetAdjustedBorders(image.rectTransform, spriteBorder / pixelsPerUnit, rect);
            borderData = new Vector4(border.x, border.y, rect.width - border.z, rect.height - border.w);

            tileWidth = (sprite.rect.width - spriteBorder.x - spriteBorder.z) / pixelsPerUnit;
            tileHeight = (sprite.rect.height - spriteBorder.y - spriteBorder.w) / pixelsPerUnit;

            if (tileWidth <= 0f) { tileWidth = Mathf.Max(borderData.z - borderData.x, 0.001f); }
            if (tileHeight <= 0f) { tileHeight = Mathf.Max(borderData.w - borderData.y, 0.001f); }
        }

        void UpdateMaskProperties()
        {
            if (!isActiveAndEnabled || maskGraphic == null)
                return;

            if (lastHierarchyCount != transform.hierarchyCount)
                RefreshHierarchy();

            UpdateCachedMaterials();
        }

        void UpdateCachedMaterials()
        {
            if (materialCache.Count == 0)
                return;

            staleMaterialKeys.Clear();
            int currentVersion = GetMaterialDataVersion();
            bool refreshChildren = false;

            foreach (KeyValuePair<MaterialKey, MaterialEntry> pair in materialCache)
            {
                MaterialEntry entry = pair.Value;
                if (entry.SourceMaterial == null || entry.ModifiedMaterial == null)
                {
                    staleMaterialKeys.Add(pair.Key);
                    continue;
                }

                if (!IsMaterialCompatible(entry.ModifiedMaterial))
                {
                    staleMaterialKeys.Add(pair.Key);
                    refreshChildren = true;
                    continue;
                }

                SyncMaterial(entry, false, currentVersion);
            }

            for (int i = 0; i < staleMaterialKeys.Count; i++)
            {
                MaterialKey key = staleMaterialKeys[i];
                if (materialCache.TryGetValue(key, out MaterialEntry entry))
                    DestroyMaterial(entry.ModifiedMaterial);

                materialCache.Remove(key);
            }

            if (refreshChildren)
                NotifyChildren();
        }

        public void ModifyMesh(VertexHelper vertexHelper)
        {
            if (!showMaskGraphic)
                vertexHelper.Clear();
        }

        public void ModifyMesh(Mesh mesh)
        {
            if (!showMaskGraphic)
                mesh.Clear();
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (!isActiveAndEnabled)
                return true;

            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPoint, eventCamera);
        }

        void RefreshMaskGraphic()
        {
            if (maskGraphic == null)
                return;

            maskGraphic.SetVerticesDirty();

#if UNITY_EDITOR
            if (!Application.IsPlaying(gameObject))
            {
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
                UnityEditor.SceneView.RepaintAll();
            }
#endif
        }

        void ClearCache()
        {
            foreach (MaterialEntry entry in materialCache.Values)
                DestroyMaterial(entry.ModifiedMaterial);

            materialCache.Clear();
        }

        static void DestroyMaterial(Material material)
        {
            if (material == null)
                return;

            if (Application.isPlaying)
                Destroy(material);
            else
                DestroyImmediate(material);
        }

        static bool IsMaterialCompatible(Material material)
        {
            if (material == null || !material.HasProperty(PropsSupport) || !material.HasProperty(PropsCount))
                return false;

            for (int i = 0; i < PropsTextures.Length; i++)
            {
                if (!material.HasTexture(PropsTextures[i]))
                    return false;
            }

            return true;
        }

        Shader GetReplacementShader(Material baseMaterial)
        {
            if (baseMaterial == null || baseMaterial.shader == null)
                return null;

            string shaderName = baseMaterial.shader.name;

            if (shaderName == "UI/Default" || shaderName == "UI/DefaultETC1" || shaderName == ShaderName)
                return embeddedShader;

            if (shaderName == TMPShaderName)
                return embeddedTMPShader;
            if (shaderName == TMPMobileShaderName)
                return embeddedTMPMobileShader;
            if (shaderName == TMPBitmapShaderName)
                return embeddedTMPBitmapShader;

            if (shaderName.StartsWith("TextMeshPro/", StringComparison.Ordinal))
            {
                if (shaderName.IndexOf("Sprite", StringComparison.Ordinal) >= 0)
                    return embeddedTMPBitmapShader;

                if (shaderName.IndexOf("Bitmap", StringComparison.Ordinal) >= 0)
                    return embeddedTMPBitmapShader;

                return shaderName.IndexOf("Mobile", StringComparison.Ordinal) >= 0
                    ? embeddedTMPMobileShader
                    : embeddedTMPShader;
            }

            if (baseMaterial.HasProperty(PropsSupport) && baseMaterial.GetFloat(PropsSupport) > 0.5f)
                return baseMaterial.shader;

            return null;
        }

        public Material GetModifiedMaterialForChild(Material baseMaterial, Canvas targetCanvas)
        {
            if (!isActiveAndEnabled || maskGraphic == null || baseMaterial == null)
                return baseMaterial;

            if (targetCanvas != null)
                targetCanvas = targetCanvas.rootCanvas;

            MaterialKey key = new(baseMaterial, targetCanvas);
            if (materialCache.TryGetValue(key, out MaterialEntry entry) && entry.ModifiedMaterial != null)
            {
                if (entry.SourceShader == baseMaterial.shader && IsMaterialCompatible(entry.ModifiedMaterial))
                {
                    SyncMaterial(entry, false, GetMaterialDataVersion());
                    return entry.ModifiedMaterial;
                }

                DestroyMaterial(entry.ModifiedMaterial);
                materialCache.Remove(key);
            }

            Shader replacementShader = GetReplacementShader(baseMaterial);
            if (replacementShader == null)
                return baseMaterial;

            Material modifiedMaterial = new(replacementShader)
            {
                name = $"{baseMaterial.name} (Evo Soft Mask)",
                hideFlags = HideFlags.HideAndDontSave
            };

            if (!IsMaterialCompatible(modifiedMaterial))
            {
                DestroyMaterial(modifiedMaterial);
                return baseMaterial;
            }

            entry = new MaterialEntry(baseMaterial, modifiedMaterial, targetCanvas);
            materialCache[key] = entry;

            SyncMaterial(entry, true, GetMaterialDataVersion());
            return modifiedMaterial;
        }

        void SyncMaterial(MaterialEntry entry, bool force, int currentVersion)
        {
            int sourceCRC = entry.SourceMaterial.ComputeCRC();
            bool sourceChanged = force || !entry.HasSourceSnapshot || entry.SourceCRC != sourceCRC;

            if (sourceChanged)
            {
                entry.ModifiedMaterial.CopyMatchingPropertiesFromMaterial(entry.SourceMaterial);
                entry.ModifiedMaterial.DisableKeyword(TMPBitmapMobileKeyword);
                entry.ModifiedMaterial.DisableKeyword(TMPSpriteKeyword);

                if (entry.TMPBitmapMode == 1)
                    entry.ModifiedMaterial.EnableKeyword(TMPBitmapMobileKeyword);
                else if (entry.TMPBitmapMode == 2)
                    entry.ModifiedMaterial.EnableKeyword(TMPSpriteKeyword);

                if (entry.SourceMaterial.HasProperty(PropsMainTex) && entry.ModifiedMaterial.HasProperty(PropsMainTex))
                {
                    Texture mainTexture = entry.SourceMaterial.GetTexture(PropsMainTex);
                    if (mainTexture != null) { entry.ModifiedMaterial.SetTexture(PropsMainTex, mainTexture); }
                }

                entry.SourceCRC = sourceCRC;
                entry.HasSourceSnapshot = true;
                entry.AppliedVersion = int.MinValue;
            }

            SetMaterialProperties(entry, sourceChanged, currentVersion);
        }

        readonly struct MaterialKey : IEquatable<MaterialKey>
        {
            readonly Material material;
            readonly Canvas canvas;
            readonly int hashCode;

            public MaterialKey(Material material, Canvas canvas)
            {
                this.material = material;
                this.canvas = canvas;

                int materialId = material != null ? material.GetHashCode() : 0;
                int canvasId = canvas != null ? canvas.GetHashCode() : 0;
                hashCode = materialId * 397 ^ canvasId;
            }

            public bool Equals(MaterialKey other) => ReferenceEquals(material, other.material) && ReferenceEquals(canvas, other.canvas);

            public override bool Equals(object obj) => obj is MaterialKey other && Equals(other);

            public override int GetHashCode() => hashCode;
        }

        sealed class MaterialEntry
        {
            public readonly Material SourceMaterial;
            public readonly Material ModifiedMaterial;
            public readonly Shader SourceShader;
            public readonly Canvas Canvas;
            public readonly int TMPBitmapMode;
            public int AppliedVersion = int.MinValue;
            public int SourceCRC;
            public bool HasSourceSnapshot;
            public bool HasCanvasSnapshot;
            public Matrix4x4 CanvasToWorld;

            public MaterialEntry(Material sourceMaterial, Material modifiedMaterial, Canvas canvas)
            {
                SourceMaterial = sourceMaterial;
                ModifiedMaterial = modifiedMaterial;
                SourceShader = sourceMaterial.shader;
                Canvas = canvas;

                string shaderName = SourceShader != null ? SourceShader.name : string.Empty;
                if (shaderName.IndexOf("Sprite", StringComparison.Ordinal) >= 0)
                    TMPBitmapMode = 2;
                else if (shaderName.IndexOf("Bitmap", StringComparison.Ordinal) >= 0 
                    && shaderName.IndexOf("Mobile", StringComparison.Ordinal) >= 0)
                    TMPBitmapMode = 1;
            }
        }

#if UNITY_EDITOR
        // Cache the delegate to prevent Editor memory leaks from repeated OnValidate calls
        UnityEditor.EditorApplication.CallbackFunction onValidateDelayCall;

        protected override void Reset()
        {
            base.Reset();

            rectTransform = GetComponent<RectTransform>();
            maskGraphic = GetComponent<Graphic>();
            LoadShaders();
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            if (rectTransform == null) { rectTransform = GetComponent<RectTransform>(); }
            if (maskGraphic == null) { maskGraphic = GetComponent<Graphic>(); }

            LoadShaders();
            MarkMaskPropertiesDirty();
            RefreshMaskGraphic();

            onValidateDelayCall ??= () =>
            {
                if (this == null)
                    return;

                RebuildMaskStack();
                RefreshHierarchy();
                RefreshMaskGraphic();
                Canvas.ForceUpdateCanvases();
            };

            UnityEditor.EditorApplication.delayCall -= onValidateDelayCall;
            UnityEditor.EditorApplication.delayCall += onValidateDelayCall;
        }

#endif
    }
}
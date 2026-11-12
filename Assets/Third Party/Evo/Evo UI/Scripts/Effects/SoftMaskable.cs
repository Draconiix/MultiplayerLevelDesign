using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Evo.UI
{
    [ExecuteAlways]
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    public class SoftMaskable : UIBehaviour, IMaterialModifier
    {
        const HideFlags RuntimeHideFlags = HideFlags.HideInInspector | HideFlags.NotEditable | HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

        public SoftMask AssignedMask { get; private set; }

        Graphic graphic;
        bool removalQueued;

        protected override void Awake()
        {
            base.Awake();
            graphic = GetComponent<Graphic>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (graphic == null)
                graphic = GetComponent<Graphic>();

            FindMask();
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            FindMask();
        }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            FindMask();

            if (graphic != null)
                graphic.SetMaterialDirty();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (graphic != null)
                graphic.SetMaterialDirty();
        }

        protected override void OnDestroy()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall -= RemoveEditorTrace;
#endif

            if (AssignedMask != null)
                AssignedMask.UnregisterMaskable(this);

            AssignedMask = null;
            base.OnDestroy();
        }

        internal void Initialize(Graphic targetGraphic)
        {
            graphic = targetGraphic;

            if (hideFlags != RuntimeHideFlags)
                hideFlags = RuntimeHideFlags;

            FindMask();
        }

        internal void RefreshMask() => FindMask();

        void FindMask()
        {
#if UNITY_EDITOR
            if (removalQueued)
            {
                removalQueued = false;
                UnityEditor.EditorApplication.delayCall -= RemoveEditorTrace;
            }
#endif

            SoftMask foundMask = null;
            Transform current = transform.parent;

            while (current != null)
            {
                if (current.TryGetComponent(out SoftMask mask) && mask.isActiveAndEnabled)
                {
                    foundMask = mask;
                    break;
                }

                current = current.parent;
            }

            if (AssignedMask == foundMask)
            {
#if UNITY_EDITOR
                if (!Application.IsPlaying(gameObject) && AssignedMask == null)
                    QueueEditorRemoval();
#endif
                return;
            }

            if (AssignedMask != null)
                AssignedMask.UnregisterMaskable(this);

            AssignedMask = foundMask;

            if (AssignedMask != null)
                AssignedMask.RegisterMaskable(this);

            if (graphic != null)
                graphic.SetMaterialDirty();

#if UNITY_EDITOR
            if (!Application.IsPlaying(gameObject) && AssignedMask == null)
                QueueEditorRemoval();
#endif
        }

        public Material GetModifiedMaterial(Material baseMaterial)
        {
            if (AssignedMask == null || !AssignedMask.isActiveAndEnabled || baseMaterial == null)
                return baseMaterial;

            if (graphic is MaskableGraphic maskableGraphic && !maskableGraphic.maskable)
                return baseMaterial;

            return AssignedMask.GetModifiedMaterialForChild(baseMaterial, graphic != null ? graphic.canvas : null);
        }

#if UNITY_EDITOR
        void QueueEditorRemoval()
        {
            if (removalQueued)
                return;

            removalQueued = true;
            UnityEditor.EditorApplication.delayCall -= RemoveEditorTrace;
            UnityEditor.EditorApplication.delayCall += RemoveEditorTrace;
        }

        void RemoveEditorTrace()
        {
            UnityEditor.EditorApplication.delayCall -= RemoveEditorTrace;
            removalQueued = false;

            if (this == null || Application.IsPlaying(gameObject) || UnityEditor.EditorUtility.IsPersistent(this))
                return;

            if (AssignedMask != null && AssignedMask.isActiveAndEnabled)
                return;

            if (AssignedMask != null)
                AssignedMask.UnregisterMaskable(this);

            AssignedMask = null;

            if (graphic != null)
                graphic.SetMaterialDirty();

            DestroyImmediate(this);
        }

        [UnityEditor.InitializeOnLoadMethod]
        static void CleanupEditorTraces()
        {
            UnityEditor.EditorApplication.delayCall -= CleanupEditorTracesDelayed;
            UnityEditor.EditorApplication.delayCall += CleanupEditorTracesDelayed;
        }

        static void CleanupEditorTracesDelayed()
        {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            SoftMaskable[] traces = Resources.FindObjectsOfTypeAll<SoftMaskable>();
            for (int i = 0; i < traces.Length; i++)
            {
                SoftMaskable trace = traces[i];

                if (trace == null || UnityEditor.EditorUtility.IsPersistent(trace) || !trace.gameObject.scene.IsValid())
                    continue;

                if (trace.graphic == null)
                    trace.graphic = trace.GetComponent<Graphic>();

                trace.FindMask();

                if (trace.AssignedMask != null)
                {
                    if (trace.hideFlags != RuntimeHideFlags)
                        trace.hideFlags = RuntimeHideFlags;

                    continue;
                }

                if (trace.graphic != null)
                    trace.graphic.SetMaterialDirty();

                trace.removalQueued = false;
                UnityEditor.EditorApplication.delayCall -= trace.RemoveEditorTrace;
                DestroyImmediate(trace);
            }
        }
#endif
    }
}
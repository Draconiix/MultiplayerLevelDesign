using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LevelDesign.Systems.Player
{
    public class HealthBarDisplay : MonoBehaviour
    {
        public RawImage healthBar;
        public TMP_Text namePlate;

        private static readonly int fillId = Shader.PropertyToID("_Fill");

        private HealthManager health;
        private Player nameSource;
        private Material fillMaterial;
        private Camera viewCamera;
        private Vector3 followOffset;
        private float lastFill = -1f;
        private bool isBound;

        #region Unity Calls
        // Each nameplate gets its own material instance so remote players don't share one fill value.
        private void Awake() {
            if(healthBar == null) { return; }

            fillMaterial = new Material(healthBar.material);
            healthBar.material = fillMaterial;
        }

        private void LateUpdate() {
            UpdateFill();
            FaceCamera();
        }

        private void OnDestroy() {
            UnsubscribeName();

            if(fillMaterial != null) {
                Destroy(fillMaterial);
            }
        }
        #endregion

        #region Core
        // Polled rather than event-driven, since remote copies only receive health through ReceiveHealth and raise no event.
        private void UpdateFill() {
            if(health == null || fillMaterial == null) { return; }

            float fill = health.maxHealth > 0f ? Mathf.Clamp01(health.currentHealth / health.maxHealth) : 0f;
            if(Mathf.Approximately(fill, lastFill)) { return; }

            lastFill = fill;
            fillMaterial.SetFloat(fillId, fill);

            Material rendered = healthBar.materialForRendering;
            if(rendered != null && rendered != fillMaterial) {
                rendered.SetFloat(fillId, fill);
            }
        }

        // Matching the camera's rotation keeps the canvas parallel to the view plane, so the UI reads correctly instead of mirrored.
        private void FaceCamera() {
            if(viewCamera == null || !viewCamera.isActiveAndEnabled) {
                viewCamera = Camera.main;
            }
            if(viewCamera == null) { return; }

            transform.rotation = viewCamera.transform.rotation;
        }

        private void SubscribeName(Player source) {
            UnsubscribeName();

            nameSource = source;
            if(nameSource == null) { return; }

            nameSource.NameChanged += HandleNameChanged;
            HandleNameChanged(nameSource.DisplayName);
        }

        private void UnsubscribeName() {
            if(nameSource == null) { return; }

            nameSource.NameChanged -= HandleNameChanged;
            nameSource = null;
        }

        private void HandleNameChanged(string newName) {
            if(namePlate == null) { return; }

            namePlate.text = newName;
        }
        #endregion

        #region Public Accessors
        // Safe to call every frame; it only does work when the sources change.
        public void Bind(Player player) {
            if(player == null) { return; }
            if(isBound && nameSource == player && health == player.healthM) { return; }

            isBound = true;
            health = player.healthM;
            gameObject.SetActive(!player.IsLocalPlayer);

            if(player.IsLocalPlayer) { return; }

            SubscribeName(player);

            lastFill = -1f;
            UpdateFill();
        }
        #endregion
    }
}
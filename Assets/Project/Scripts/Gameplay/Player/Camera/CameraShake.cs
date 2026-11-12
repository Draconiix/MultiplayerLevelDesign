using UnityEngine;
using TinyInspector;

namespace LevelDesign.Systems {
    public class DamageCameraShake : MonoBehaviour {
        [BoxGroup("Scene Refs")]
        [SerializeField] private HealthManager healthManager;
        [BoxGroup("Scene Refs")]
        [SerializeField] private Transform shakeTarget;

        [BoxGroup("Config")]
        [SerializeField] private float duration = 0.2f;
        [BoxGroup("Config")]
        [SerializeField] private float magnitude = 0.1f;
        [BoxGroup("Config")]
        [SerializeField] private float frequency = 25f;

        private Vector3 restPosition;
        private float timeRemaining;
        private float noiseSeed;

        #region Unity Calls
        private void Awake() {
            if(shakeTarget == null) {
                shakeTarget = transform;
            }

            restPosition = shakeTarget.localPosition;
            noiseSeed = Random.value * 100f;
        }

        private void OnEnable() {
            if(healthManager == null) { return; }

            healthManager.Damaged += OnDamaged;
        }

        private void OnDisable() {
            if(healthManager != null) {
                healthManager.Damaged -= OnDamaged;
            }

            StopShake();
        }

        private void LateUpdate() {
            if(timeRemaining <= 0f) { return; }

            timeRemaining -= Time.deltaTime;

            if(timeRemaining <= 0f) {
                StopShake();
                return;
            }

            float falloff = timeRemaining / duration;
            float strength = magnitude * falloff * falloff;
            float sample = Time.time * frequency;

            float offsetX = Mathf.PerlinNoise(noiseSeed, sample) * 2f - 1f;
            float offsetY = Mathf.PerlinNoise(noiseSeed + 1f, sample) * 2f - 1f;

            shakeTarget.localPosition = restPosition + new Vector3(offsetX, offsetY, 0f) * strength;
        }
        #endregion

        #region Core
        private void OnDamaged(float damage) {
            Shake();
        }

        private void StopShake() {
            timeRemaining = 0f;

            if(shakeTarget != null) {
                shakeTarget.localPosition = restPosition;
            }
        }
        #endregion

        #region Public Accessors
        public void Shake() {
            if(duration <= 0f) { return; }

            timeRemaining = duration;
        }
        #endregion
    }
}

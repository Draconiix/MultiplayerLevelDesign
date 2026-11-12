using System.Collections.Generic;
using UnityEngine;
using LevelDesign.Systems.Player;

namespace LevelDesign.Systems {
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public class Hazard : MonoBehaviour {
        [Header("Config")]
        [SerializeField] private float damage = 10f;
        [SerializeField] private float tickInterval = .5f;
        [SerializeField] private bool damageOnEnter = true;

        public LevelDesign.Systems.Player.Player ignorePlayer;

        private readonly Dictionary<LevelDesign.Systems.Player.Player, float> nextTickTime = new Dictionary<LevelDesign.Systems.Player.Player, float>();
        private Collider trigger;
        private Rigidbody body;

        #region Unity Calls
        private void Awake() {
            trigger = GetComponent<Collider>();
            trigger.isTrigger = true;

            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
        }

        private void OnDisable() {
            nextTickTime.Clear();
        }

        private void OnTriggerEnter(Collider other) {
            HandleContact(other);
        }

        private void OnTriggerStay(Collider other) {
            HandleContact(other);
        }
        #endregion

        #region Core
        private void HandleContact(Collider other) {
            if(!other.TryGetComponent(out Hurtbox hurtbox)) { return; }

            LevelDesign.Systems.Player.Player player = hurtbox.GetComponentInParent<LevelDesign.Systems.Player.Player>();
            if(player == null) { return; }
            if(!player.IsLocalPlayer) { return; }
            if(player == ignorePlayer) { return; }
            if(!TryConsumeTick(player)) { return; }

            hurtbox.Trigger(damage, true);
        }

        private bool TryConsumeTick(LevelDesign.Systems.Player.Player player) {
            if(nextTickTime.TryGetValue(player, out float next)) {
                if(Time.time < next) { return false; }
            } else if(!damageOnEnter) {
                nextTickTime[player] = Time.time + tickInterval;
                return false;
            }

            nextTickTime[player] = Time.time + tickInterval;
            return true;
        }
        #endregion

        #region Public Accessors
        public float Damage => damage;
        public float TickInterval => tickInterval;

        public void SetDamage(float value) {
            damage = value;
        }

        public void SetTickInterval(float value) {
            tickInterval = value;
        }
        #endregion
    }
}
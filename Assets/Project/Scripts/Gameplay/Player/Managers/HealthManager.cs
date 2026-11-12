using System;
using UnityEngine;
using PurrNet;
using LevelDesign.Systems.Player;

namespace LevelDesign.Systems
{
    public class HealthManager : NetworkBehaviour
    {
        public float currentHealth;
        public float maxHealth;

        [SerializeField] private float regenDelay = 3f;
        [SerializeField] private float regenPerSecond = 10f;
        [SerializeField] private float regenInterval = 0.25f;

        private StackManager stacks;
        private bool isDead;
        private float lastDamageTime;
        private float nextRegenTime;

        // Died fires when a life was spent and a respawn is expected; call Revive() once the player is back.
        public event Action Died;
        public event Action Revived;
        public event Action Eliminated;
        public event Action<float> Damaged;

        public int Lives => stacks != null ? stacks.Get(StackType.Life) : 0;
        public bool IsDead => isDead;

        public void Initialize(StackManager stackManager) {
            stacks = stackManager;
            isDead = false;
        }

        // Regen runs where damage resolves, and ticks at an interval so it doesn't send a health RPC every frame.
        private void Update() {
            if(isSpawned && !isOwner) { return; }
            if(isDead || currentHealth <= 0 || currentHealth >= maxHealth) { return; }
            if(Time.time - lastDamageTime < regenDelay || Time.time < nextRegenTime) { return; }

            nextRegenTime = Time.time + regenInterval;
            Heal(regenPerSecond * regenInterval);
        }

        // Damage resolves on the owning client, since that is where the life stacks live. Anyone else routes it through the server.
        public void TakeDamage(float damage)
        {
            if(!isSpawned || isOwner) {
                ApplyDamage(damage);
            } else {
                RequestDamage(damage);
            }
        }

        public void Heal(float health)
        {
            currentHealth += health;
            if(currentHealth > maxHealth) { currentHealth = maxHealth; }

            BroadcastHealth();
        }

        public void ResetHealth() {
            isDead = false;
            currentHealth = maxHealth;
            BroadcastHealth();
        }

        public void AddLife(int increase)
        {
            if(stacks == null) { return; }

            stacks.Add(StackType.Life, increase);
        }

        public void RemoveLife(int removal)
        {
            if(stacks == null) { return; }

            stacks.ConsumeUpTo(StackType.Life, removal);
            if(Lives == 0) {
                Defeated();
            }
        }

        public void Die()
        {
            if(isDead) { return; }

            isDead = true;
            BroadcastHealth();

            if(stacks != null && stacks.TryConsume(StackType.Life)) {
                Died?.Invoke();
            } else {
                Defeated();
            }
        }

        public void Revive() {
            isDead = false;
            currentHealth = maxHealth;
            BroadcastHealth();
            Revived?.Invoke();
        }

        public void Defeated()
        {
            Eliminated?.Invoke();
        }

        private void ApplyDamage(float damage) {
            if(isDead) { return; }

            currentHealth -= damage;
            lastDamageTime = Time.time;
            Damaged?.Invoke(damage);

            if(currentHealth <= 0) {
                Die();
            } else {
                BroadcastHealth();
            }
        }

        private void BroadcastHealth() {
            if(!isSpawned) { return; }

            SendHealth(currentHealth, isDead);
        }

        [ServerRpc(requireOwnership: false)]
        private void RequestDamage(float damage) {
            if(owner.HasValue) {
                DeliverDamage(owner.Value, damage);
            } else {
                ApplyDamage(damage);
            }
        }

        [TargetRpc]
        private void DeliverDamage(PlayerID target, float damage) {
            ApplyDamage(damage);
        }

        [ServerRpc]
        private void SendHealth(float health, bool dead) {
            ReceiveHealth(health, dead);
        }

        [ObserversRpc(bufferLast: true, excludeOwner: true)]
        private void ReceiveHealth(float health, bool dead) {
            currentHealth = health;
            isDead = dead;
        }
    }
}
using System;
using System.Collections.Generic;
using PurrNet;
using UnityEngine;

namespace LevelDesign.Systems.Player {
    [Serializable]
    public class StackPickupVisual {
        public StackType Type;
        public GameObject ReadyToUseObject;
        public GameObject OnCooldownObject;
    }

    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public class StackPickup : NetworkBehaviour {
        #region Fields
        [Header("Config")]
        [SerializeField] private StackType type = StackType.Ammo;
        [SerializeField] private int amount = 1;
        [Space]
        [SerializeField] private bool respawns = true;
        [SerializeField] private float respawnDelay = 10f;
        [Space]
        [SerializeField] private float requestTimeout = 1f;
        [SerializeField] private float retryDelay = .25f;
        [Space]
        [SerializeField] private List<StackPickupVisual> visuals = new List<StackPickupVisual> {
            new StackPickupVisual { Type = StackType.DoubleJump },
            new StackPickupVisual { Type = StackType.Slide },
            new StackPickupVisual { Type = StackType.Life },
            new StackPickupVisual { Type = StackType.Ammo }
        };

        [SerializeField] private GameObject respawnableObject;

        private readonly Dictionary<StackType, StackPickupVisual> visualLookup = new Dictionary<StackType, StackPickupVisual>();

        private SyncVar<bool> available = new(true);

        private Collider trigger;
        private Rigidbody body;
        private float respawnTime;
        private Player pendingCollector;
        private float pendingSince;
        private float nextRequestTime;

        public event Action<StackPickup, Player> Collected;
        public event Action<StackPickup, bool> AvailabilityChanged;
        #endregion

        #region Unity Calls
        private void Awake() {
            trigger = GetComponent<Collider>();
            trigger.isTrigger = true;

            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            available.onChanged += OnAvailableChanged;

            BuildVisualLookup();
            ValidateVisuals();
            ApplyAvailable(available.value);

            SetObjectActive(respawnableObject, respawns);
        }

        protected override void OnSpawned() {
            base.OnSpawned();
            ApplyAvailable(available.value);
        }

        private void Update() {
            if(!isServer) { return; }
            if(available.value || !respawns) { return; }

            if(Time.time >= respawnTime) {
                available.value = true;
            }
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
            if(!isSpawned) { return; }
            if(!available.value) { return; }
            if(Time.time < nextRequestTime) { return; }

            Player player = other.GetComponentInParent<Player>();
            if(player == null) { return; }
            if(!player.IsLocalPlayer) { return; }

            TryCollect(player);
        }

        [ServerRpc(requireOwnership: false)]
        private void RequestCollect(RPCInfo info = default) {
            if(!available.value) {
                DenyCollect(info.sender);
                return;
            }

            available.value = false;
            respawnTime = Time.time + respawnDelay;
            GrantCollect(info.sender);
        }

        [TargetRpc]
        private void GrantCollect(PlayerID target) {
            Player player = pendingCollector;
            pendingCollector = null;
            if(player == null) { return; }

            StackManager stacks = player.StackM;
            if(stacks == null) { return; }

            stacks.Add(type, amount);
            Collected?.Invoke(this, player);
        }

        [TargetRpc]
        private void DenyCollect(PlayerID target) {
            pendingCollector = null;
            nextRequestTime = Time.time + retryDelay;
        }

        private void OnAvailableChanged(bool value) {
            ApplyAvailable(value);
            AvailabilityChanged?.Invoke(this, value);
        }

        private void ApplyAvailable(bool value) {
            if(trigger == null) {
                trigger = GetComponent<Collider>();
            }

            trigger.enabled = value;

            if(visualLookup.Count == 0) {
                BuildVisualLookup();
            }

            foreach(StackPickupVisual entry in visuals) {
                bool isCurrentType = entry.Type == type;
                SetObjectActive(entry.ReadyToUseObject, isCurrentType && value);
                SetObjectActive(entry.OnCooldownObject, isCurrentType && !value && respawns);
            }
        }

        private void BuildVisualLookup() {
            visualLookup.Clear();

            foreach(StackPickupVisual entry in visuals) {
                visualLookup[entry.Type] = entry;
            }

            foreach(StackType stackType in Enum.GetValues(typeof(StackType))) {
                if(!visualLookup.ContainsKey(stackType)) {
                    StackPickupVisual entry = new StackPickupVisual { Type = stackType };
                    visuals.Add(entry);
                    visualLookup[stackType] = entry;
                }
            }
        }

        private void ValidateVisuals() {
            StackPickupVisual current = visualLookup[type];

            if(current.ReadyToUseObject == null) {
                Debug.LogWarning($"StackPickup has no ReadyToUseObject for {type}; nothing will show while it is available.", this);
            }
            if(respawns && current.OnCooldownObject == null) {
                Debug.LogWarning($"StackPickup has no OnCooldownObject for {type}; nothing will show while it is on cooldown.", this);
            }

            foreach(StackPickupVisual entry in visuals) {
                WarnIfContainsTrigger(entry.ReadyToUseObject);
                WarnIfContainsTrigger(entry.OnCooldownObject);
            }
        }

        private void WarnIfContainsTrigger(GameObject target) {
            if(target == null) { return; }
            if(target != gameObject && !trigger.transform.IsChildOf(target.transform)) { return; }

            Debug.LogWarning($"StackPickup visual '{target.name}' contains the pickup or its trigger; hiding it will stop pickup logic or respawning. Assign a child object instead.", this);
        }

        private void SetObjectActive(GameObject target, bool active) {
            if(target == null) { return; }
            if(target.activeSelf == active) { return; }

            target.SetActive(active);
        }

        private bool IsRequestPending() {
            if(pendingCollector == null) { return false; }

            if(Time.time - pendingSince > requestTimeout) {
                pendingCollector = null;
                return false;
            }

            return true;
        }
        #endregion

        #region Public Accessors
        public bool IsAvailable => available.value;
        public StackType Type => type;
        public int Amount => amount;

        public StackPickupVisual GetVisual(StackType stackType) {
            if(visualLookup.Count == 0) {
                BuildVisualLookup();
            }

            return visualLookup[stackType];
        }

        public bool TryCollect(Player player) {
            if(!isSpawned) { return false; }
            if(!available.value) { return false; }
            if(IsRequestPending()) { return false; }
            if(player == null) { return false; }

            if(player.StackM == null) {
                player.StackM = player.GetComponentInChildren<StackManager>();
            }
            if(player.StackM == null) { return false; }

            pendingCollector = player;
            pendingSince = Time.time;
            RequestCollect();
            return true;
        }

        public void SetAvailable(bool value) {
            if(!isServer) {
                Debug.LogWarning("StackPickup.SetAvailable can only be called on the server.", this);
                return;
            }

            available.value = value;

            if(!value) {
                respawnTime = Time.time + respawnDelay;
            }
        }
        #endregion
    }
}
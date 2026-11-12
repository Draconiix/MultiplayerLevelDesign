using System;
using System.Collections.Generic;
using UnityEngine;

namespace LevelDesign.Systems.Player {
    [Serializable]
    public class StackSlot {
        public StackType Type;
        public int StartAmount;
        public bool Unlimited;
        public int Current;
    }

    public class StackManager : MonoBehaviour {
        [SerializeField] private List<StackSlot> slots = new List<StackSlot> {
            new StackSlot { Type = StackType.DoubleJump, StartAmount = 0 },
            new StackSlot { Type = StackType.Slide, StartAmount = 0 },
            new StackSlot { Type = StackType.Life, StartAmount = 3 },
            new StackSlot { Type = StackType.Ammo, StartAmount = 3 }
        };

        private readonly Dictionary<StackType, StackSlot> lookup = new Dictionary<StackType, StackSlot>();

        // Arguments: type, new amount, delta (positive when gained, negative when consumed).
        public event Action<StackType, int, int> Changed;

        #region Unity Calls
        private void Awake() {
            BuildLookup();
            ResetToStart();
        }
        #endregion

        #region Core
        private void BuildLookup() {
            lookup.Clear();

            foreach(StackSlot slot in slots) {
                lookup[slot.Type] = slot;
            }

            foreach(StackType type in Enum.GetValues(typeof(StackType))) {
                if(!lookup.ContainsKey(type)) {
                    StackSlot slot = new StackSlot { Type = type };
                    slots.Add(slot);
                    lookup[type] = slot;
                }
            }
        }

        private StackSlot GetSlot(StackType type) {
            if(lookup.Count == 0) {
                BuildLookup();
            }

            return lookup[type];
        }

        private void Apply(StackSlot slot, int amount) {
            int clamped = Mathf.Max(0, amount);
            int delta = clamped - slot.Current;
            if(delta == 0) { return; }

            slot.Current = clamped;
            Changed?.Invoke(slot.Type, clamped, delta);
        }
        #endregion

        #region Public Accessors
        public int Get(StackType type) {
            return GetSlot(type).Current;
        }

        public bool IsUnlimited(StackType type) {
            return GetSlot(type).Unlimited;
        }

        public bool Has(StackType type, int amount = 1) {
            StackSlot slot = GetSlot(type);
            return slot.Unlimited || slot.Current >= amount;
        }

        public int Add(StackType type, int amount = 1) {
            if(amount <= 0) { return 0; }

            StackSlot slot = GetSlot(type);
            if(slot.Unlimited) { return 0; }

            int before = slot.Current;
            Apply(slot, slot.Current + amount);
            return slot.Current - before;
        }

        public bool TryConsume(StackType type, int amount = 1) {
            if(amount <= 0) { return true; }

            StackSlot slot = GetSlot(type);
            if(slot.Unlimited) { return true; }
            if(slot.Current < amount) { return false; }

            Apply(slot, slot.Current - amount);
            return true;
        }

        public int ConsumeUpTo(StackType type, int amount) {
            if(amount <= 0) { return 0; }

            StackSlot slot = GetSlot(type);
            if(slot.Unlimited) { return amount; }

            int taken = Mathf.Min(amount, slot.Current);
            Apply(slot, slot.Current - taken);
            return taken;
        }

        public void Set(StackType type, int amount) {
            Apply(GetSlot(type), amount);
        }

        public void SetUnlimited(StackType type, bool unlimited) {
            GetSlot(type).Unlimited = unlimited;
        }

        public void ResetToStart() {
            foreach(StackSlot slot in lookup.Values) {
                Apply(slot, slot.StartAmount);
            }
        }
        #endregion
    }
}
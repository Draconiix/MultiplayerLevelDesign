using UnityEngine;
using UnityEngine.Serialization;
using LevelDesign.Async.Auth;
using LevelDesign.Data;
using TinyInspector;

// Base class for every movement controller. Any controller works with Player as long as it implements these members.

namespace LevelDesign.Systems.Player {
    public enum Stance { Stand, Crouch, Slide, Air }

    public abstract class _MovementController : _InputAuth {
        [BoxGroup("_MovementController/Scene Refs")]
        public Transform _visualSpawnPoint;
        [BoxGroup("_MovementController/Scene Refs")]
        public Hurtbox _hurtbox;
        [Space]
        [BoxGroup("_MovementController/Scene Refs")]
        public CharacterDataSO _characterData;

        [BoxGroup("_MovementController/Debug")]
        public bool _isInitialized;
        [BoxGroup("_MovementController/Debug")]
        public Stance _stanceMirror;
        
        [BoxGroup("_MovementController/Nameplate")]
        public HealthBarDisplay _healthBarDisplay;

        protected StackManager _stacks;

        #region Core
        public abstract void _Initialize(PlayerStateMachine psm, CharacterDataSO characterdata);
        public abstract void _RemoteInit();
        public abstract void _UpdateBody(float deltaTime, Transform playerCam);

        public abstract void _SpawnVisuals();

        public abstract Transform _GetCameraTarget();
        public abstract RigInfo _GetCurrentRigInfo();

        // Movement state for view model and camera effects (bob, air tilt, landing).
        // Override to return the body's real world-space velocity.
        public virtual Vector3 _GetVelocity() => Vector3.zero;
        // Speed (m/s) that counts as full walk for view model bob. Return 0 to let the weapon's SO decide.
        public virtual float _GetWalkSpeed() => 0f;

        public abstract void _Teleport(Vector3 position);
        public abstract void _SetRotation(Quaternion rotation);
        #endregion

        #region Stacks
        public void _BindStacks(StackManager stacks) {
            _stacks = stacks;
        }

        protected int _GetStackCount(StackType type) {
            return _stacks != null ? _stacks.Get(type) : 0;
        }

        protected bool _HasStack(StackType type, int amount = 1) {
            return _stacks != null && _stacks.Has(type, amount);
        }

        // Consumes the stack and returns true only when the ability is allowed to fire.
        protected bool _TryUseStack(StackType type, int amount = 1) {
            if(_stacks == null) { return false; }

            return _stacks.TryConsume(type, amount);
        }
        #endregion
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TinyInspector;
using KinematicCharacterController;
using LevelDesign.Async.Auth;
using LevelDesign.Data;

namespace LevelDesign.Systems.Player {
    public struct CharacterState {
        public bool Grounded;
        public Stance Stance;
        public Vector3 Velocity;
    }

    public class PlayerCharacter : _MovementController, ICharacterController {
        [BoxGroup("Scene Refs")]
        [SerializeField] private KinematicCharacterMotor motor;
        [BoxGroup("Scene Refs")]
        [SerializeField] private Transform cameraTarget;
        [BoxGroup("Scene Refs")]
        [SerializeField] private Transform particleSpawnPoint;

        [BoxGroup("Particles")]
        [SerializeField] private GameObject jumpParticle;

        [TabGroup("Controller", "Config")]
        [SerializeField] private float groundResponse = 25f;
        [TabGroup("Controller", "Config")]
        [SerializeField] private float gravity = -90f;

        [TabGroup("Controller", "Height")]
        [SerializeField] private float standHeight = 2f;
        [TabGroup("Controller", "Height")]
        [SerializeField] private float crouchHeight = 1.2f;
        [TabGroup("Controller", "Height")] [Range(0f, 1f)]
        [SerializeField] private float cameraStandHeight = .9f;
        [TabGroup("Controller", "Height")] [Range(0f, 1f)]
        [SerializeField] private float cameraCrouchHeight = .7f;
        [TabGroup("Controller", "Height")]
        [SerializeField] private float heightResponse = 15f;

        [TabGroup("Movement", "Speeds")]
        [SerializeField] private float walkSpeed = 20f;
        [TabGroup("Movement", "Speeds")]
        [SerializeField] private float crouchSpeed = 10f;
        [TabGroup("Movement", "Speeds")]
        [SerializeField] private float sprintSpeed = 32f;
        [TabGroup("Movement", "Speeds")]
        [SerializeField] private float airSpeed = 15f;
        [TabGroup("Movement", "Speeds")]
        [SerializeField] private float airAcceleration = 70f;

        [TabGroup("Movement", "Jumping")]
        [SerializeField] private float jumpSpeed = 20f;
        [TabGroup("Movement", "Jumping")]
        [SerializeField] private float maxAerialJumps = 2f;
        [TabGroup("Movement", "Jumping")]
        [SerializeField] private float aerialJumpMultiplier = 1f;

        [TabGroup("Movement", "Sliding")]
        [SerializeField] private float slideStartSpeed = 40f;
        [TabGroup("Movement", "Sliding")]
        [SerializeField] private float slideFriction = .8f;
        [TabGroup("Movement", "Sliding")]
        [SerializeField] private float slideSteerAcceleration = 5f;
        [TabGroup("Movement", "Sliding")]
        [SerializeField] private float slideGravity = -90f;
        [TabGroup("Movement", "Sliding")]
        [SerializeField] private float sprintCoyoteTime = .15f;

        [BoxGroup("Debug")]
        [SerializeField] private Stance debugStance;
        [BoxGroup("Debug")]
        [SerializeField] private bool debugSprinting;
        [BoxGroup("Debug")]
        [SerializeField] private RigInfo currentRigInfo;

        public CharacterState state;
        private CharacterState lastState;

        private Quaternion cameraYaw = Quaternion.identity;
        private Quaternion requestedRotation = Quaternion.identity;
        private Vector3 requestedMovement;
        private bool requestedJump;
        private bool requestedCrouch;
        private bool requestedSprint;
        private bool isCrouched;
        private bool isSprinting;
        private float timeSinceSprint;

        private int currentJumpCount;

        private Transform playerCamera;

        private PlayerStateMachine psm;

        private const float MinPlanarSqrMagnitude = 0.0001f;
        private const float MinSlideSpeedRatio = .7f;

        #region Core
        public override void _Initialize(PlayerStateMachine psm, CharacterDataSO characterdata) {
            if(_isInitialized) { return; }

            _isInitialized = true;

            aInputInit(true);
            InputAuthManager.Instance.RequestInput(this);

            this.psm = psm;
            motor.CharacterController = this;
            motor.enabled = true;
            state.Stance = Stance.Stand;
            lastState = state;
            timeSinceSprint = sprintCoyoteTime + 1f;

            _characterData = characterdata;

            _SpawnVisuals();
        }

        public override void _RemoteInit() {
            motor.enabled = false;
        }

        public override void _UpdateBody(float deltaTime, Transform playerCam) {
            if(!_isInitialized || motor == null || cameraTarget == null) { return; }

            playerCamera = playerCam;

            _stanceMirror = state.Stance;

            UpdateCameraYaw();
            UpdateInput();

            var cameraHeight = motor.Capsule.height * (isCrouched ? cameraCrouchHeight : cameraStandHeight);

            cameraTarget.localPosition = Vector3.Lerp(
                cameraTarget.localPosition,
                new Vector3(0f, cameraHeight, 0f),
                1f - Mathf.Exp(-heightResponse * deltaTime));
        }

        public override void _SpawnVisuals() {
            currentRigInfo = Instantiate(_characterData.thirdPersonVisuals, _visualSpawnPoint);
            if(currentRigInfo != null) {
                currentRigInfo.meshManager.disableMeshes = true;
                currentRigInfo.playerAnimation.m_Controller = this;
                currentRigInfo.localPlayer = true;
            }
        }

        private void UpdateCameraYaw() {
            if(playerCamera == null) { return; }

            var up = motor.CharacterUp;
            var forward = Vector3.ProjectOnPlane(playerCamera.forward, up);

            if(forward.sqrMagnitude < MinPlanarSqrMagnitude) {
                forward = Vector3.ProjectOnPlane(playerCamera.up, up) * -Mathf.Sign(Vector3.Dot(playerCamera.forward, up));
            }

            if(forward.sqrMagnitude > MinPlanarSqrMagnitude) {
                cameraYaw = Quaternion.LookRotation(forward.normalized, up);
            }
        }

        public void ClearInput() {
            requestedMovement = Vector3.zero;
            requestedJump = false;
            requestedCrouch = false;
            requestedSprint = false;
        }

        public void UpdateInput() {
            if(!_isInitialized || !_inputAuthorized) {
                ClearInput();
                return;
            }

            requestedRotation = cameraYaw;

            var move = _input.Move.ReadValue<Vector2>();
            requestedMovement = cameraYaw * Vector3.ClampMagnitude(new Vector3(move.x, 0f, move.y), 1f);

            requestedJump |= _input.Jump.WasPressedThisFrame();
            requestedCrouch = _input.Crouch.IsPressed();
            requestedSprint = _input.Sprint.IsPressed();
        }

        private void FindComponent<T>(ref T field) where T : Component {
            if(field != null) { return; }

            field = GetComponent<T>() ?? GetComponentInChildren<T>();
        }

        private void SetCrouched(bool crouch) {
            if(crouch == isCrouched) { return; }

            isCrouched = crouch;

            var height = crouch ? crouchHeight : standHeight;
            motor.SetCapsuleDimensions(motor.Capsule.radius, height, height * .5f);
        }
        #endregion

        #region Character Controller
        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime) {
            var forward = Vector3.ProjectOnPlane(requestedRotation * Vector3.forward, motor.CharacterUp);
            if(forward != Vector3.zero) {
                currentRotation = Quaternion.LookRotation(forward, motor.CharacterUp);
            }
        }

        public void BeforeCharacterUpdate(float deltaTime) {
            lastState = state;
            SetCrouched(requestedCrouch);
        }

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime) {
            var grounded = motor.GroundingStatus.IsStableOnGround;
            if(grounded) {
                currentJumpCount = 0;
            }

            timeSinceSprint += deltaTime;

            if(grounded) {
                var groundedMovement = motor.GetDirectionTangentToSurface(
                    requestedMovement, motor.GroundingStatus.GroundNormal) * requestedMovement.magnitude;

                if(state.Stance is Stance.Slide && isCrouched) {
                    UpdateSlide(ref currentVelocity, groundedMovement, deltaTime);
                } else if(CanStartSlide(currentVelocity, groundedMovement) && _TryUseStack(StackType.Slide)) {
                    StartSlide(ref currentVelocity);
                } else {
                    state.Stance = isCrouched ? Stance.Crouch : Stance.Stand;

                    isSprinting = !isCrouched
                                  && requestedSprint
                                  && requestedMovement.sqrMagnitude > MinPlanarSqrMagnitude;

                    if(isSprinting) {
                        timeSinceSprint = 0f;
                    }

                    var speed = isCrouched ? crouchSpeed : (isSprinting ? sprintSpeed : walkSpeed);

                    currentVelocity = Vector3.Lerp(
                        currentVelocity,
                        groundedMovement * speed,
                        1f - Mathf.Exp(-groundResponse * deltaTime));
                }
            } else {
                state.Stance = Stance.Air;
                isSprinting = false;

                if(requestedMovement.sqrMagnitude > 0f) {
                    var planarMovement = Vector3.ProjectOnPlane(requestedMovement, motor.CharacterUp).normalized
                                         * requestedMovement.magnitude;
                    var planarVelocity = Vector3.ProjectOnPlane(currentVelocity, motor.CharacterUp);
                    var movementForce = planarMovement * airAcceleration * deltaTime;

                    if(planarVelocity.magnitude < airSpeed) {
                        var target = Vector3.ClampMagnitude(planarVelocity + movementForce, airSpeed);
                        movementForce = target - planarVelocity;
                    } else if(Vector3.Dot(planarVelocity, movementForce) > 0f) {
                        movementForce = Vector3.ProjectOnPlane(movementForce, planarVelocity.normalized);
                    }

                    currentVelocity += movementForce;
                }

                currentVelocity += motor.CharacterUp * gravity * deltaTime;
            }

            if(requestedJump) {
                requestedJump = false;

                if(grounded || _TryUseStack(StackType.DoubleJump)) {
                    if(!state.Grounded) {
                        Instantiate(jumpParticle, particleSpawnPoint.position, particleSpawnPoint.rotation);
                    }

                    motor.ForceUnground();
                    currentJumpCount++;

                    var verticalSpeed = Vector3.Dot(currentVelocity, motor.CharacterUp);
                    var aerialMult = grounded ? 1f : aerialJumpMultiplier;
                    var targetSpeed = jumpSpeed * aerialMult;
                    currentVelocity += motor.CharacterUp * (Mathf.Max(targetSpeed, verticalSpeed) - verticalSpeed);
                }
            }
        }

        private bool CanStartSlide(Vector3 currentVelocity, Vector3 groundedMovement) {
            if(!isCrouched || groundedMovement.sqrMagnitude < MinPlanarSqrMagnitude) { return false; }

            var planarSpeed = Vector3.ProjectOnPlane(currentVelocity, motor.GroundingStatus.GroundNormal).magnitude;
            if(planarSpeed < walkSpeed * MinSlideSpeedRatio) { return false; }

            var wasInAir = !lastState.Grounded;
            var wasSprinting = lastState.Stance is Stance.Stand && timeSinceSprint <= sprintCoyoteTime;

            return wasInAir || wasSprinting;
        }

        private void StartSlide(ref Vector3 currentVelocity) {
            var groundNormal = motor.GroundingStatus.GroundNormal;

            if(!lastState.Grounded) {
                currentVelocity = Vector3.ProjectOnPlane(lastState.Velocity, groundNormal);
            }

            var speed = Mathf.Max(slideStartSpeed, currentVelocity.magnitude);

            state.Stance = Stance.Slide;
            isSprinting = false;
            currentVelocity = motor.GetDirectionTangentToSurface(currentVelocity, groundNormal) * speed;
        }

        private void UpdateSlide(ref Vector3 currentVelocity, Vector3 groundedMovement, float deltaTime) {
            isSprinting = false;

            currentVelocity -= currentVelocity * (slideFriction * deltaTime);
            currentVelocity += Vector3.ProjectOnPlane(motor.CharacterUp, motor.GroundingStatus.GroundNormal)
                               * (slideGravity * deltaTime);

            if(groundedMovement.sqrMagnitude > MinPlanarSqrMagnitude) {
                var currentSpeed = currentVelocity.magnitude;
                var steerForce = (groundedMovement * currentSpeed - currentVelocity) * (slideSteerAcceleration * deltaTime);
                currentVelocity = Vector3.ClampMagnitude(currentVelocity + steerForce, currentSpeed);
            }

            if(currentVelocity.magnitude < crouchSpeed) {
                state.Stance = Stance.Crouch;
            }
        }

        public void AfterCharacterUpdate(float deltaTime) {
            state.Grounded = motor.GroundingStatus.IsStableOnGround;
            state.Velocity = motor.Velocity;
            _stanceMirror = state.Stance;
            debugStance = state.Stance;
            debugSprinting = isSprinting;
        }

        public void PostGroundingUpdate(float deltaTime) { }
        public bool IsColliderValidForCollisions(Collider coll) => true;
        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) { }
        public void OnDiscreteCollisionDetected(Collider hitCollider) { }
        #endregion

        #region Public Accessors
        public override Transform _GetCameraTarget() => cameraTarget;
        public override Vector3 _GetVelocity() => state.Velocity;
        public override float _GetWalkSpeed() => walkSpeed;

        public override RigInfo _GetCurrentRigInfo() {
            if(currentRigInfo == null) {
                FindComponent(ref currentRigInfo);
            }

            return currentRigInfo;
        }

        public override void _Teleport(Vector3 position) {
            if(motor == null) { return; }

            motor.BaseVelocity = Vector3.zero;
            motor.SetPosition(position);
        }

        public override void _SetRotation(Quaternion rotation) {
            if(motor == null) { return; }

            cameraYaw = rotation;
            requestedRotation = rotation;
            motor.SetRotation(rotation);
        }

        public void ResetMovementStates() {
            if(!_isInitialized || motor == null) { return; }

            ClearInput();
            state = default;
            state.Stance = Stance.Stand;
            lastState = state;
            motor.BaseVelocity = Vector3.zero;
            isSprinting = false;
            timeSinceSprint = sprintCoyoteTime + 1f;
            SetCrouched(false);
        }

        public CharacterState GetState() => state;
        public Stance GetStance() => state.Stance;
        public bool GetIsSprinting() => isSprinting;
        #endregion
    }
}
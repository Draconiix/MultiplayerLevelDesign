using UnityEngine;
using LevelDesign.Async.Auth;
using TinyInspector;

// Summary
// Base class responsible for managing the base movement states such as walk, run, jump, crouch.
// Will need to be extended when elements such as abilities modify the animations.

namespace LevelDesign.Systems.Player
{
    public abstract class _PlayerAnimation : _InputAuth
    {
        [BoxGroup("Scene Refs")]
        [SerializeField] protected RigInfo rigInfo;
        [BoxGroup("Scene Refs")]
        public Animator characterAnimator;
        [Space]
        [BoxGroup("Scene Refs")]
        public _MovementController m_Controller;

        [BoxGroup("Debug")]
        public float deltaTime;
        [BoxGroup("Debug")]
        public float animationReactionModifier = 1f;

        private float currentXVal;
        private float currentYVal;

        private bool isGrounded = true;
        private bool isCrouching = false;
        private bool isMoving = false;
        private bool isSliding = false;

        private Stance characterStance;

        protected virtual void Awake()
        {
            _FindComponent(ref rigInfo);
        }

        protected virtual void OnEnable()
        {
            aInputInit(true);
            InputAuthManager.Instance.RequestInput(this);
        }

        void _FindComponent<T>(ref T field) where T : Component
        {
            if(field != null) { return; }
            field = GetComponent<T>() ?? GetComponentInChildren<T>();
        }

        protected virtual void Update()
        {
            if(!rigInfo.localPlayer) { return; }

            deltaTime = Time.deltaTime;

            if(characterAnimator == null) { return; }

            bool sprinting = false;
            Vector2 requestedMovement = new Vector2(0,0);
            if(_inputAuthorized) { 
                requestedMovement = _input.Move.ReadValue<Vector2>();
                sprinting = _input.Sprint.IsPressed();
            }

            float maxMovementValue = sprinting ? 1f : 0.5f;
            float targetX = Mathf.Clamp(requestedMovement.x, -maxMovementValue, maxMovementValue);
            float targetY = Mathf.Clamp(requestedMovement.y, -maxMovementValue, maxMovementValue);

            currentXVal = Mathf.Lerp(currentXVal, targetX, deltaTime * animationReactionModifier);
            currentYVal = Mathf.Lerp(currentYVal, targetY, deltaTime * animationReactionModifier);

            isMoving = Mathf.Abs(currentXVal) >= .01f || Mathf.Abs(currentYVal) >= .01f;

            _UpdateAnimationStateFromStance();
            _UpdateAnimatorValues();
        }

        protected virtual void _UpdateAnimationStateFromStance()
        {
            if (m_Controller != null)
            {
                characterStance = m_Controller._stanceMirror;
            }

            switch (characterStance)
            {
                case Stance.Air:
                    isGrounded = false; isCrouching = false; isSliding = false; break;
                case Stance.Stand:
                    isGrounded = true; isCrouching = false; isSliding = false; break;
                case Stance.Crouch:
                    isGrounded = true; isCrouching = true; isSliding = false; break;
                case Stance.Slide:
                    isGrounded = true; isCrouching = false; isSliding = true; break;
            }
        }

        protected virtual void _UpdateAnimatorValues()
        {
            if(characterAnimator == null) { return; }

            characterAnimator.SetFloat("xVal", currentXVal);
            characterAnimator.SetFloat("yVal", currentYVal);
            characterAnimator.SetBool("Grounded", isGrounded);
            characterAnimator.SetBool("Crouch", isCrouching);
            characterAnimator.SetBool("Sliding", isSliding);
            characterAnimator.SetBool("Moving", isMoving);
        }

        protected virtual void OnDestroy()
        {
            if(InputAuthManager.Instance != null)
            {
                InputAuthManager.Instance.RelinquishRequest(this);
            }
        }
    }
}
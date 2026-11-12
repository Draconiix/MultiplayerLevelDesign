using UnityEngine;
using LevelDesign.Async.Auth;
using TinyInspector;

// Summary
// Script responsible for manaing logic such as weapons, melees, etc

namespace LevelDesign.Systems.Player
{
    public class ArmController : MonoBehaviour
    {
        [BoxGroup("Scene Refs")]
        public Animator armAnimator;
        [BoxGroup("Scene Refs")]
        public bool isInitialized;
        [Space]
        [BoxGroup("Scene Refs")]
        public Player player;
        [BoxGroup("Scene Refs")]
        public _MovementController movementController;

        // Movement States
        private bool idle;
        private bool crouch;
        private bool slide;

        private Stance lastStance;


        public void Initialize(Player playerRef, _MovementController controller)
        {
            player = playerRef;
            movementController = controller;
            isInitialized = true;
        }

        public void Update()
        {
            if(!isInitialized) { return; }

            UpdateStanceState();
            PassIntoAnimator();
        }

        private void UpdateStanceState()
        {
            var stance = movementController._stanceMirror;

            if(stance == lastStance) return;
            lastStance = stance;

            switch(stance)
            {
                case Stance.Air:
                    idle = true;
                    crouch = false;
                    slide = false;
                    break;

                case Stance.Stand:
                    idle = true;
                    crouch = false;
                    slide = false;
                    break;

                case Stance.Crouch:
                    idle = false;
                    crouch = true;
                    slide = false;
                    break;

                case Stance.Slide:
                    idle = false;
                    crouch = false;
                    slide = true;
                    break;
            }
        }

        private void PassIntoAnimator()
        {
            if(armAnimator == null) { return; }

            armAnimator.SetBool("Idle", idle);
            armAnimator.SetBool("Crouching", crouch);
            armAnimator.SetBool("Slide", slide);
        }
    }
}

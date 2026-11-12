using UnityEngine;
using UnityEngine.Serialization;
using TinyInspector;

namespace LevelDesign.Data
{
    [CreateAssetMenu(fileName = "ViewModelMovement", menuName = "ScriptableObjects/Weapons/ViewModelMovement", order = 2)]
    public class ViewModelMovementSO : ScriptableObject
    {
        // View model
        [BoxGroup("Sway")]
        public float swayAmount = 0.03f;
        [BoxGroup("Sway")]
        public float swayTilt = 4f;
        [BoxGroup("Sway")]
        public float swayInputScale = 0.05f;
        [BoxGroup("Sway")]
        public float swaySmooth = 8f;

        [BoxGroup("Bob")]
        public float bobAmount = 0.01f;
        [BoxGroup("Bob")]
        public float bobSpeed = 2f;
        [BoxGroup("Bob")]
        public float bobBlendSpeed = 6f;
        [BoxGroup("Bob")]
        public float bobReferenceSpeed = 5f;
        [BoxGroup("Bob")]
        public float bobMaxScale = 1.6f;
        [BoxGroup("Bob")]
        public float bobPitch = 0.6f;
        [BoxGroup("Bob")]
        public float bobYaw = 0.6f;
        [BoxGroup("Bob")]
        public float bobRoll = 1.2f;

        [BoxGroup("Breathing")]
        public float breathAmount = 0.003f;
        [BoxGroup("Breathing")]
        public float breathSpeed = 0.35f;

        [BoxGroup("Air Tilt")]
        public float airTiltPerSpeed = 0.5f;
        [BoxGroup("Air Tilt")]
        public float airTiltMax = 6f;
        [BoxGroup("Air Tilt")]
        public float airTiltSmooth = 6f;
        [BoxGroup("Air Tilt")]
        public float airOffset = 0.015f;

        [BoxGroup("Landing")]
        public float landKickPerSpeed = 0.08f;
        [BoxGroup("Landing")]
        public float landDrop = 0.03f;
        [BoxGroup("Landing")]
        public float landPitch = 4f;
        [BoxGroup("Landing")]
        public float landSnappiness = 18f;
        [BoxGroup("Landing")]
        public float landReturn = 7f;

        [BoxGroup("Weapon Kick")]
        public float kickBack = 0.04f;
        [BoxGroup("Weapon Kick")]
        public float kickPitch = 2f;
        [BoxGroup("Weapon Kick")]
        public float kickSnappiness = 25f;
        [BoxGroup("Weapon Kick")]
        public float kickReturn = 10f;

        // Camera
        [BoxGroup("Camera Recoil")]
        public float recoilUp = 1.4f;
        [BoxGroup("Camera Recoil")]
        public float recoilSide = 0.5f;
        [BoxGroup("Camera Recoil")]
        public int sideRampShots = 4;
        [BoxGroup("Camera Recoil")]
        public float sprayResetTime = 0.4f;
        [BoxGroup("Camera Recoil")]
        public float recoilSnappiness = 20f;
        [BoxGroup("Camera Recoil")]
        public float recoilReturn = 4f;

        public Vector2 GetRecoil(int t_shotIndex)
        {
            float t_ramp = sideRampShots > 0 ? Mathf.Clamp01((float)t_shotIndex / sideRampShots) : 1f;
            float t_side = Random.Range(-recoilSide, recoilSide) * t_ramp;
            float t_up = recoilUp * Random.Range(0.85f, 1.15f);
            return new Vector2(t_side, t_up);
        }
    }
}
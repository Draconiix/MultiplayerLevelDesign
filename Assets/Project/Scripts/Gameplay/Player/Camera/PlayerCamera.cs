using UnityEngine;
using System.Collections.Generic;
using LevelDesign.Async.Auth;
using TinyInspector;

namespace LevelDesign.Systems.Player
{
    public struct CameraInput
    {
        public Vector2 Look;
    }

    public class PlayerCamera : _InputAuth
    {
        [BoxGroup("Config")]
        [SerializeField] private float currentSensitivity = .08f;
        [BoxGroup("Config")]
        [SerializeField] private float cinematicPositionSmoothing = 5f;
        [BoxGroup("Config")]
        [SerializeField] private float cinematicRotationSmoothing = 5f;

        [BoxGroup("Scene Refs")]
        [SerializeField] private List<Camera> cameraList;
        [BoxGroup("Scene Refs")]
        [SerializeField] private PlayerStateMachine PSM;
    
        private Vector3 eulerAngles;
        private float maxLookAngle = 80f;
        private CameraInput cameraInput;

        public void Initialize(PlayerStateMachine psm = null)
        {
            aInputInit(true);
            InputAuthManager.Instance.RequestInput(this);
            
            if(psm != null) {
                PSM = psm;
            }
            else {
                Debug.LogWarning("Player Camera's Player State Machine Did Not Initialize");
            }
            
        }

        public void RemoteInit() {
            foreach(Camera cam in cameraList) {
                cam.gameObject.SetActive(false);
            }

            this.enabled = false;
        }

        public void UpdateCameraInput() {
            if (!_inputAuthorized)
            {
                cameraInput = new CameraInput { Look = Vector2.zero };
                return;
            }

            cameraInput = new CameraInput { Look = _input.Look.ReadValue<Vector2>() };
        }

        #region Generic Helpers        
        // Summary: Update the Camera's Current Position
        // Called by the Player.cs UpdateCameraTarget Function
        public void UpdatePosition(Transform moveLocation, bool setRotation = false)
        {
            transform.position = moveLocation.position;

            if (setRotation)
            {
                transform.eulerAngles = eulerAngles = moveLocation.eulerAngles;
            }
        }

        public void UpdateRotation()
        {
            eulerAngles += new Vector3(-cameraInput.Look.y, cameraInput.Look.x) * currentSensitivity;
            eulerAngles.x = Mathf.Clamp(eulerAngles.x, -maxLookAngle, maxLookAngle);

            transform.eulerAngles = eulerAngles;
        }

        public void SetRotation(Vector3 angles)
        {
            eulerAngles = angles;
            transform.eulerAngles = angles;
        }
        #endregion

        #region Cinematic Helpers
        public void SnapToTarget(Transform target)
        {
            transform.SetPositionAndRotation(target.position, target.rotation);
        }

        public void UpdatePositionSmooth(Transform targetLocation)
        {
            transform.position = Vector3.Lerp(transform.position, targetLocation.position, Time.deltaTime * cinematicPositionSmoothing);
        }

        public void UpdateRotationSmooth(Vector3 targetEulerAngles)
        {
            Quaternion targetRotation = Quaternion.Euler(targetEulerAngles);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * cinematicRotationSmoothing);
        }
        #endregion

        void OnDestroy() {
            InputAuthManager.Instance.RelinquishRequest(this);
        }
    }
}
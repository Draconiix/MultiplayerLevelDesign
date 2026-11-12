using TinyInspector;
using UnityEngine;

namespace LevelDesign.Systems
{
    public class RespawnPoint : MonoBehaviour
    {
        [BoxGroup("Scene Refs")]
        [SerializeField] private GameObject respawnCameraPoint;
        [BoxGroup("Scene Refs")]
        [SerializeField] private Camera respawnCamera;

        #region Public Accessors
        public Transform CameraPoint {
            get { return respawnCameraPoint != null ? respawnCameraPoint.transform : null; }
        }
        #endregion
    }
}
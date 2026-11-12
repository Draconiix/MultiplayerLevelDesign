using UnityEngine;

namespace LevelDesign.Tags
{
    public class TAG_LoadInLook : MonoBehaviour
    {
        [SerializeField] private Camera camera;

        private void Awake()
        {
            camera.enabled = false;
        }
    }
}

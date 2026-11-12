using UnityEngine;
using TinyInspector;

namespace LevelDesign.Async
{
    public class ToastController : MonoBehaviour
    {
        [BoxGroup("Scene Refs")]
        [SerializeField] private RectTransform instantiateLocation;
        [BoxGroup("Scene Refs")]
        [SerializeField] private ToastConstructor toastPrefab;

        private ToastConstructor currentToast;

        public static ToastController Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        public void Notify(string message) {
            if(currentToast != null) { currentToast.Clear(); }
            currentToast = Instantiate(toastPrefab, instantiateLocation);
            currentToast.Initialize(message);
        }
    }
}

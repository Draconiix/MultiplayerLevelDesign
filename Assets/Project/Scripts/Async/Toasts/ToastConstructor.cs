using UnityEngine;
using TinyInspector;
using TMPro;

namespace LevelDesign
{
    public class ToastConstructor : MonoBehaviour
    {
        [BoxGroup("Scene Refs")]
        [SerializeField] private TMP_Text messageText;
        [BoxGroup("Scene Refs")]
        [SerializeField] private RectTransform toastObject;

        [BoxGroup("Config")]
        public Vector3 startPosition;
        [BoxGroup("Config")]
        public Vector3 endPosition;
        [BoxGroup("Config")]
        public float duration = 1f;
        [BoxGroup("Config")]
        public float destroyTime = 5f;
        [BoxGroup("Config")]
        private float elapsedTime = 0f;
        
        public void Initialize(string message) {
            messageText.text = message;
        }

        void Update()
        {
            elapsedTime += Time.deltaTime;

            if (elapsedTime < duration)
            {

                float t = elapsedTime / duration;
                toastObject.anchoredPosition = Vector3.Lerp(startPosition, endPosition, t);
            }

            if(elapsedTime > destroyTime) {
                Clear();
            }
        }

        public void Clear() {
            Destroy(this.gameObject);
        }
    }
}

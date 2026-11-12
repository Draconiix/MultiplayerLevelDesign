using TMPro;
using UnityEngine;
using LevelDesign.Async;

namespace LevelDesign.UI
{
    public class UserNameDisplay : MonoBehaviour {
        [SerializeField] private TMP_Text label;
        [SerializeField] private string format = "{0}";

        private string cachedUsername = "";
        private UserData subscribedUserData;

        #region Unity Calls
        private void Reset() {
            label = GetComponent<TMP_Text>();
        }

        private void Start() {
            Subscribe();
        }

        private void OnDestroy() {
            Unsubscribe();
        }
        #endregion

        #region Core
        private void Subscribe() {
            if(UserData.Instance == null) { return; }

            subscribedUserData = UserData.Instance;
            subscribedUserData.OnUsernameChanged += HandleUsernameChanged;

            HandleUsernameChanged(subscribedUserData.Username);
        }

        private void Unsubscribe() {
            if(subscribedUserData == null) { return; }

            subscribedUserData.OnUsernameChanged -= HandleUsernameChanged;
            subscribedUserData = null;
        }

        private void HandleUsernameChanged(string newUsername) {
            cachedUsername = newUsername ?? "";
            Refresh();
        }

        private void Refresh() {
            if(label == null) { return; }

            label.text = string.Format(format, cachedUsername);
        }
        #endregion

        #region Public Accessors
        public string Username => cachedUsername;

        public void SetUsername(TMP_InputField inputField) {
            string newUsername = inputField.text;
            
            if(newUsername == null) {
                ToastController.Instance.Notify("Username is null");
                return;
            }

            if(newUsername.Length == 0) {
                ToastController.Instance.Notify("UserNameDisplay: Username is empty.");
                return;
            }

            if(UserData.Instance == null) {
                Debug.Log("UserData instance is null");
                return;
            }

            if(!UserData.Instance.SetUsername(newUsername)) {
                Debug.Log("Username was rejected by UserData");
            }
        }
        #endregion
    }
}
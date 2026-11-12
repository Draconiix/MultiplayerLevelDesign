using PurrNet;
using TMPro;
using UnityEngine;
using LevelDesign.Async;

namespace LevelDesign.UI
{
    public class PlayerNameTag : NetworkBehaviour {
        [SerializeField] private TMP_Text label;

        private readonly SyncVar<string> networkUsername = new("", ownerAuth: true);

        #region Unity Calls
        private void Reset() {
            label = GetComponentInChildren<TMP_Text>();
        }
        #endregion

        #region Core
        protected override void OnSpawned(bool asServer) {
            if(asServer) { return; }

            networkUsername.onChanged += HandleNetworkUsernameChanged;
            Refresh(networkUsername.value);

            if(!isOwner) { return; }

            if(UserData.Instance == null) {
                Debug.LogWarning("PlayerNameTag: No UserData instance found in the scene.");
                return;
            }

            UserData.Instance.OnUsernameChanged += PushUsername;
            PushUsername(UserData.Instance.Username);
        }

        protected override void OnDespawned(bool asServer) {
            if(asServer) { return; }

            networkUsername.onChanged -= HandleNetworkUsernameChanged;

            if(isOwner && UserData.Instance != null) {
                UserData.Instance.OnUsernameChanged -= PushUsername;
            }
        }

        private void PushUsername(string username) {
            if(!isOwner) { return; }
            if(string.IsNullOrEmpty(username)) { return; }

            networkUsername.value = username;
        }

        private void HandleNetworkUsernameChanged(string newUsername) {
            Refresh(newUsername);
        }

        private void Refresh(string username) {
            if(label == null) { return; }

            label.text = username;
        }
        #endregion
    }
}
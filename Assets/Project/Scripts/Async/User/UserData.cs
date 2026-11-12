using System;
using UnityEngine;
using LevelDesign.Async.Preferences;

namespace LevelDesign.Async
{
    public class UserData : MonoBehaviour {
        private const int MaxUsernameLength = 16;

        private static readonly string[] adjectives = {
            "Swift", "Quiet", "Brave", "Clever", "Lucky", "Mighty", "Nimble", "Sunny"
        };

        private static readonly string[] nouns = {
            "Fox", "Otter", "Falcon", "Panda", "Lynx", "Badger", "Raven", "Wolf"
        };

        private string username = "";

        #region Unity Calls
        private void Awake() {
            if(Instance != null && Instance != this) {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void Start() {
            LoadUsername();
        }
        #endregion

        #region Core
        private void LoadUsername() {
            if(PlayerPreferences.Instance == null) { return; }

            string stored = PlayerPreferences.Instance.GetString(PlayerPreferences.Keys.Username);

            if(string.IsNullOrWhiteSpace(stored)) {
                RandomUsername();
                return;
            }

            username = stored;
            OnUsernameChanged?.Invoke(username);
        }

        private static string GenerateRandomName() {
            string adjective = adjectives[UnityEngine.Random.Range(0, adjectives.Length)];
            string noun = nouns[UnityEngine.Random.Range(0, nouns.Length)];
            int number = UnityEngine.Random.Range(10, 100);

            return $"{adjective}{noun}{number}";
        }
        #endregion

        #region Public Accessors
        public static UserData Instance { get; private set; }

        public event Action<string> OnUsernameChanged;

        public string Username => username;

        public bool SetUsername(string newUsername) {
            if(string.IsNullOrWhiteSpace(newUsername)) { return false; }

            newUsername = newUsername.Trim();

            if(newUsername.Length > MaxUsernameLength) {
                newUsername = newUsername.Substring(0, MaxUsernameLength);
            }

            username = newUsername;

            if(PlayerPreferences.Instance != null) {
                PlayerPreferences.Instance.SetString(PlayerPreferences.Keys.Username, username);
                PlayerPreferences.Instance.SetBool(PlayerPreferences.Keys.EstablishedUser, true);
            }

            OnUsernameChanged?.Invoke(username);
            return true;
        }

        public void RandomUsername() {
            SetUsername(GenerateRandomName());
        }
        #endregion
    }
}
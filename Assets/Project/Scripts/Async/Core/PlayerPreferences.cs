using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// Summary
/// Key Value pair for player data to be stored via json
/// 
/// TODO:
///     - Link output path to Steamworks for saving / loading infromation across devices
namespace LevelDesign.Async.Preferences
{
    public class PlayerPreferences : MonoBehaviour
    {
        public static PlayerPreferences Instance { get; private set; }

        public event Action<string> OnPreferenceChanged;

        private static string SaveDirectory => Path.Combine(Application.persistentDataPath, "SaveData");
        private static string SaveFilePath => Path.Combine(SaveDirectory, "config.json");

        private readonly Dictionary<string, string> store = new Dictionary<string, string>();

        [Serializable]
        private class SaveEntry
        {
            public string key;
            public string value;
        }

        [Serializable]
        private class SaveData
        {
            public List<SaveEntry> entries = new List<SaveEntry>();
        }

        public static class Keys
        {
            public const string Username = "Username";
            public const string EstablishedUser = "EstablishedUser";
        }

        #region Unity

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
            }

            Instance = this;
            Load();
        }

        #endregion

        #region API

        public float GetFloat(string key, float defaultValue = 0f)
        {
            if (store.TryGetValue(key, out string raw) &&
                float.TryParse(raw, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float value))
            {
                return value;
            }

            return defaultValue;
        }

        public void SetFloat(string key, float value)
        {
            Set(key, value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            if (store.TryGetValue(key, out string raw) && int.TryParse(raw, out int value))
                return value;
            return defaultValue;
        }

        public void SetInt(string key, int value) => Set(key, value.ToString());

        public bool GetBool(string key, bool defaultValue = false)
        {
            if (store.TryGetValue(key, out string raw) && bool.TryParse(raw, out bool value))
                return value;
            return defaultValue;
        }

        public void SetBool(string key, bool value) => Set(key, value.ToString());

        public string GetString(string key, string defaultValue = "")
        {
            return store.TryGetValue(key, out string raw) ? raw : defaultValue;
        }

        public void SetString(string key, string value) => Set(key, value ?? "");

        public bool HasKey(string key) => store.ContainsKey(key);

        public void DeleteKey(string key)
        {
            if (store.Remove(key)) Save();
        }

        public void DeleteAll()
        {
            store.Clear();
            Save();
        }

        public string LoadInputBinding(string actionName, bool keyboardScheme = true)
        {
            return GetString(GetInputBindingKey(actionName, keyboardScheme));
        }

        public void SaveInputBinding(string actionName, string bindingPath, bool keyboardScheme = true)
        {
            SetString(GetInputBindingKey(actionName, keyboardScheme), bindingPath);
        }

        public void ClearInputBinding(string actionName, bool keyboardScheme = true)
        {
            DeleteKey(GetInputBindingKey(actionName, keyboardScheme));
        }

        private static string GetInputBindingKey(string actionName, bool keyboardScheme)
        {
            return $"InputBinding_{actionName}_{(keyboardScheme ? "MK" : "Gamepad")}";
        }

        #endregion

        #region Helpers

        private void Set(string key, string value)
        {
            store[key] = value;
            Save();
            OnPreferenceChanged?.Invoke(key);
        }

        public void Save()
        {
            try
            {
                if (!Directory.Exists(SaveDirectory)) Directory.CreateDirectory(SaveDirectory);

                var data = new SaveData();
                foreach (var kvp in store)
                    data.entries.Add(new SaveEntry { key = kvp.Key, value = kvp.Value });

                File.WriteAllText(SaveFilePath, JsonUtility.ToJson(data, true));
            }
            catch (Exception ex)
            {
                Debug.LogError($"PlayerPreferences: Failed to save - {ex.Message}");
            }
        }

        private void Load()
        {
            store.Clear();

            if (!File.Exists(SaveFilePath))
            {
                Debug.Log($"PlayerPreferences: No save file found at {SaveFilePath}. Using defaults.");
                return;
            }

            try
            {
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SaveFilePath));
                if (data?.entries != null)
                {
                    foreach (var entry in data.entries)
                    {
                        if (!string.IsNullOrEmpty(entry.key))
                            store[entry.key] = entry.value ?? "";
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"PlayerPreferences: Failed to load - {ex.Message}. Starting empty.");
                store.Clear();
            }
        }

        #endregion
    }
}
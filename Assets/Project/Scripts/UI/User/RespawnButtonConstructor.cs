using System.Collections.Generic;
using UnityEngine.EventSystems;
using TinyInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LevelDesign.Systems.Player;
using LevelDesign.Systems;
using Evo.UI;

namespace LevelDesign.UI {
    public class RespawnButtonConstructor : MonoBehaviour {
        [BoxGroup("Scene Refs")]
        [SerializeField] private Evo.UI.Button buttonPrefab;
        [BoxGroup("Scene Refs")]
        [SerializeField] private Transform buttonContainer;
        [BoxGroup("Scene Refs")]
        [SerializeField] private RespawnManager respawnM;

        private readonly List<Evo.UI.Button> spawnedButtons = new List<Evo.UI.Button>();

        #region Core
        private void CreateButton(Player player, RespawnPoint point) {
            if(point.CameraPoint == null) {
                Debug.LogWarning($"Respawn point '{point.name}' has no camera point assigned");
                return;
            }

            Evo.UI.Button button = Instantiate(buttonPrefab, buttonContainer);
            TMP_Text label = button.GetComponentInChildren<TMP_Text>();

            string displayName = $"Location {spawnedButtons.Count + 1}";

            if(label != null) {
                label.text = displayName;
            }

            button.gameObject.name = $"RespawnButton_{displayName}";
            button.onClick.AddListener(() => {
                respawnM.SetActivePoint(point);
                player.SetAndLoadCinematic(point.CameraPoint);
            });
            spawnedButtons.Add(button);
        }
        private void Clear() {
            foreach(Evo.UI.Button button in spawnedButtons) {
                if(button != null) {
                    Destroy(button.gameObject);
                }
            }

            spawnedButtons.Clear();
        }
        private void SelectFirst() {
            if(spawnedButtons.Count == 0) { return; }

            Evo.UI.Button first = spawnedButtons[0];

            if(EventSystem.current != null) {
                EventSystem.current.SetSelectedGameObject(first.gameObject);
            }

            first.onClick.Invoke();
        }
        #endregion

        #region Public Accessors
        public void Build(Player player) {
            Clear();

            if(buttonPrefab == null || buttonContainer == null || respawnM == null) {
                Debug.LogWarning("Respawn Button Constructor is missing its button prefab, container or respawn manager");
                return;
            }

            RespawnPoint[] points = FindObjectsByType<RespawnPoint>(FindObjectsSortMode.InstanceID);

            foreach(RespawnPoint point in points) {
                CreateButton(player, point);
            }

            SelectFirst();
        }
        #endregion
    }
}
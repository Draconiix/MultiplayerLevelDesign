using System;
using System.Collections;
using UnityEngine;
using TinyInspector;
using LevelDesign.Data;
using LevelDesign.Systems.Player;
using LevelDesign.Systems;
using LevelDesign.UI;

namespace LevelDesign
{
    public class RespawnManager : MonoBehaviour
    {
        #region Fields
        [BoxGroup("Config")]
        [SerializeField] private float deathLength;

        [BoxGroup("Scene Refs")]
        [SerializeField] private CharacterDataSO characterData;

        [BoxGroup("Debug")]
        [SerializeField] private RespawnPoint activePoint;
        [BoxGroup("Debug")]
        public float deathTimeRemaining;

        private Player player;
        private PlayerCamera playerCamera;
        private CharacterDataManager characterDataM;
        private Coroutine spawnRoutine;

        public event Action<RespawnPoint> OnConfirmed;
        #endregion

        #region Unity Calls
        private void Update() {
            if(player == null || !player.IsDead) { return; }

            if(deathTimeRemaining > 0f) {
                deathTimeRemaining = Mathf.Max(0f, deathTimeRemaining - Time.deltaTime);
            }
        }
        #endregion

        #region Core
        private IEnumerator SpawnRoutine(RespawnPoint point) {
            characterDataM.LoadCharacterData(characterData);

            _MovementController controller = characterDataM.currentMovementController;

            if(controller == null) {
                spawnRoutine = null;
                yield break;
            }

            // Player.RoutineChecks initializes the new controller on the next Update
            while(controller != null && !controller._isInitialized) {
                yield return null;
            }

            if(controller == null) {
                spawnRoutine = null;
                yield break;
            }

            Quaternion spawnRotation = GetFlatRotation(point.transform);

            controller._Teleport(point.transform.position);
            controller._SetRotation(spawnRotation);

            if(playerCamera != null) {
                playerCamera.SetRotation(new Vector3(0f, spawnRotation.eulerAngles.y, 0f));
            }

            player.ExitCinematic();
            player.healthM.ResetHealth();
            OnConfirmed?.Invoke(point);

            spawnRoutine = null;
        }

        private Quaternion GetFlatRotation(Transform point) {
            Vector3 forward = Vector3.ProjectOnPlane(point.forward, Vector3.up);

            if(forward.sqrMagnitude < 0.0001f) { return Quaternion.identity; }

            return Quaternion.LookRotation(forward.normalized, Vector3.up);
        }
        #endregion

        #region Public Accessors
        public bool IsSpawning => spawnRoutine != null;

        public bool DeathTimerElapsed => deathTimeRemaining <= 0f;

        public bool IsAwaitingRespawn => player != null && player.IsDead && DeathTimerElapsed && !IsSpawning;

        public void Initialize(Player playerRef, CharacterDataManager dataManager, PlayerCamera cameraRef) {
            player = playerRef;
            characterDataM = dataManager;
            playerCamera = cameraRef;
        }

        public void StartDeathTimer() {
            deathTimeRemaining = deathLength;
        }

        public void SetActivePoint(RespawnPoint point) {
            activePoint = point;
        }

        public void SetCharacterData(CharacterDataSO newCharacterData) {
            characterData = newCharacterData;
        }

        public void Confirm() {
            if(!IsAwaitingRespawn) { return; }

            if(activePoint == null) {
                Debug.LogWarning("Respawn Manager has no active respawn point to confirm");
                return;
            }

            if(characterData == null) {
                Debug.LogWarning("Respawn Manager has no character data assigned");
                return;
            }

            if(characterDataM == null) {
                Debug.LogWarning("Respawn Manager was confirmed before it was initialized");
                return;
            }

            spawnRoutine = StartCoroutine(SpawnRoutine(activePoint));
        }
        #endregion
    }
}
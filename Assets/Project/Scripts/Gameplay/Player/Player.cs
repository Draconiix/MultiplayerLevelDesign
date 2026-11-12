using System;
using UnityEngine;
using PurrNet;
using TinyInspector;
using LevelDesign.Async;
using LevelDesign.Systems;

namespace LevelDesign.Systems.Player
{
    public class Player : NetworkBehaviour
    {
        [BoxGroup("State Machine")]
        public PlayerStateMachine PSM;
        [BoxGroup("State Machine")]
        [SerializeField] private LocalPlayerState LPS;

        [BoxGroup("Controllers")]
        [SerializeField] private PlayerCamera playerCamera;
        [Space]
        [BoxGroup("Controllers")]
        [SerializeField] private _MovementController playerCharacter;
        [BoxGroup("Controllers")]
        [SerializeField] private ArmController playerArmController;

        [BoxGroup("Drops")]
        [SerializeField] private GameObject healthPickup;

        [BoxGroup("Managers")]
        public CharacterDataManager characterDataM;
        [BoxGroup("Managers")]
        public RespawnManager respawnM;
        [BoxGroup("Managers")]
        public UIManager uiM;
        [BoxGroup("Managers")]
        public HealthManager healthM;
        [BoxGroup("Managers")]
        public StackManager StackM;

        private Transform cameraFocalTarget;
        private Transform spectatorCameraTarget;
        private GameManager subscribedGameManager;

        private string displayName = "";
        private UserData subscribedUserData;

        public event Action<string> NameChanged;

        #region Unity Calls
        public void Initialize() {
            if(LPS.isLocalPlayer) {
                LocalInit();
            } else {
                RemoteInit();
            }
        }

        private void Update() {
            RoutineChecks();
            ProcessControllers();

            if(!LPS.isLocalPlayer) { return; }

            if(GameManager.Instance != null) {
                PSM.isPlaying = GameManager.Instance.localState == GameState.Playing;
            }

            SyncLifeState();
        }

        private void LateUpdate() {
            UpdateCameraTarget();
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            UnsubscribeUsername();

            if(healthM != null) {
                healthM.Died -= OnDeath;
            }

            if(subscribedGameManager != null) {
                subscribedGameManager.OnStateEntered -= OnGameStateEntered;
                subscribedGameManager = null;
            }
        }
        #endregion

        #region Initializations
        private void LocalInit() {
            if(StackM == null) {
                StackM = GetComponentInChildren<StackManager>();
            }

            healthM.Died += OnDeath;

            playerCamera.Initialize(PSM);
            characterDataM.Initialize();
            healthM.Initialize(StackM);
            respawnM.Initialize(this, characterDataM, playerCamera);
            uiM.Initialize(Player: this);

            StartUnspawned();

            if(GameManager.Instance != null) {
                subscribedGameManager = GameManager.Instance;
                subscribedGameManager.OnStateEntered += OnGameStateEntered;
                subscribedGameManager.RegisterLocalPlayer(this);

                if(subscribedGameManager.localState == GameState.GameOver) {
                    OnGameOver();
                }
            } else {
                Debug.LogWarning("Player could not register: no GameManager in the scene");
            }
        }

        // The player begins with no body and zero health, so the first spawn goes through the same respawn flow as every later one.
        private void StartUnspawned() {
            characterDataM.ClearCharacter();
            healthM.currentHealth = 0;
        }

        private void RemoteInit() {
            playerCamera.RemoteInit();
        }
        #endregion

        #region Controllers
        public void ProcessControllers() {
            if(playerCamera != null) {
                playerCamera.UpdateCameraInput();
                playerCamera.UpdateRotation();
            }

            if(playerCharacter != null && playerCamera != null) {
                playerCharacter._UpdateBody(Time.deltaTime, playerCamera.gameObject.transform);
            }
        }

        private void BindHealthBar() {
            if(playerCharacter == null || playerCharacter._healthBarDisplay == null) { return; }

            playerCharacter._healthBarDisplay.Bind(this);
        }

        public void UpdateCameraTarget() {
            if(playerCamera == null) { return; }

            if(!PSM.isCinematic) {
                if(playerCharacter == null) { return; }

                spectatorCameraTarget = null;
                cameraFocalTarget = playerCharacter._GetCameraTarget();

                if(cameraFocalTarget == null) { return; }

                playerCamera.UpdatePosition(cameraFocalTarget);
            } else {
                if(spectatorCameraTarget == null) { return; }

                playerCamera.SnapToTarget(spectatorCameraTarget);
            }
        }

        public void ClearMovement() { }
        #endregion

        #region Checks
        private void RoutineChecks() {
            if(!LPS.isLocalPlayer) {
                if(playerCharacter == null) {
                    playerCharacter = GetComponentInChildren<_MovementController>();

                    if(playerCharacter != null && playerCharacter.enabled) {
                        playerCharacter._RemoteInit();
                    }
                }

                if(playerCharacter != null) {
                    BindHealthBar();
                    RigInfo rig = playerCharacter._GetCurrentRigInfo();
                }
            }

            if(characterDataM.currentMovementController != null && LPS.isLocalPlayer) {
                if(!characterDataM.currentMovementController._isInitialized) {
                    playerCharacter = characterDataM.currentMovementController;

                    playerCharacter._BindStacks(StackM);
                    playerCharacter._Initialize(PSM, characterDataM.currentCharacterData);
                    BindHealthBar();
                }
            }

            if(characterDataM.currentArmController != null && LPS.isLocalPlayer) {
                if(!characterDataM.currentArmController.isInitialized) {
                    playerArmController = characterDataM.currentArmController;

                    if(playerCharacter != null) {
                        playerArmController.Initialize(playerRef: this, controller: playerCharacter);
                    }
                }
            }
        }

        // PSM life flags are a read-only mirror for systems that still read them; HealthManager and RespawnManager own the actual state.
        private void SyncLifeState() {
            PSM.isDead = IsDead;
            PSM.awaitingRespawn = respawnM.IsAwaitingRespawn;
        }

        public void OnDeath() {
            Vector3 spawnHealthPickupPoint = characterDataM.currentCharacterObject.transform.position;
            Instantiate(healthPickup, spawnHealthPickupPoint, Quaternion.identity, null);

            characterDataM.ClearCharacter();
            respawnM.StartDeathTimer();
        }

        private void OnGameStateEntered(GameState next) {
            if(next != GameState.GameOver) { return; }

            OnGameOver();
        }

        private void OnGameOver() {
            characterDataM.ClearCharacter();
            playerCharacter = null;
            playerArmController = null;
        }
        #endregion

        #region Name Sync
        protected override void OnSpawned() {
            if(!isOwner) { return; }

            SubscribeUsername();
        }

        protected override void OnDespawned() {
            UnsubscribeUsername();
        }

        private void SubscribeUsername() {
            if(UserData.Instance == null) {
                Debug.LogWarning("Player: no UserData instance, name will not be sent");
                return;
            }

            subscribedUserData = UserData.Instance;
            subscribedUserData.OnUsernameChanged += HandleUsernameChanged;

            HandleUsernameChanged(subscribedUserData.Username);
        }

        private void UnsubscribeUsername() {
            if(subscribedUserData == null) { return; }

            subscribedUserData.OnUsernameChanged -= HandleUsernameChanged;
            subscribedUserData = null;
        }

        private void HandleUsernameChanged(string newUsername) {
            SetDisplayName(newUsername);
            SendDisplayName(displayName);
        }

        private void SetDisplayName(string newName) {
            displayName = newName ?? "";
            NameChanged?.Invoke(displayName);
        }

        [ServerRpc]
        private void SendDisplayName(string newName) {
            ReceiveDisplayName(newName);
        }

        [ObserversRpc(bufferLast: true, excludeOwner: true)]
        private void ReceiveDisplayName(string newName) {
            SetDisplayName(newName);
        }
        #endregion

        #region Public Accessors
        public bool IsLocalPlayer => LPS != null && LPS.isLocalPlayer;

        public string DisplayName => displayName;

        public bool IsDead => healthM != null && healthM.currentHealth <= 0;

        public Transform GetCameraTarget() {
            return playerCharacter != null ? playerCharacter._GetCameraTarget() : null;
        }

        public void SetAndLoadCinematic(Transform cinematicPosition) {
            spectatorCameraTarget = cinematicPosition;
            PSM.isCinematic = true;
        }

        public void ExitCinematic() {
            PSM.isCinematic = false;
        }
        #endregion
    }
}
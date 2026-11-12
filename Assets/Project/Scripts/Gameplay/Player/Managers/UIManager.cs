using TinyInspector;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using LevelDesign.UI;

namespace LevelDesign.Systems.Player
{
    public class UIManager : MonoBehaviour
    {
        [BoxGroup("Scene Refs/Gameplay")]
        [SerializeField] private GameObject gamePage;
        [BoxGroup("Scene Refs/Gameplay")]
        [SerializeField] private GameObject hitmarkerSpawn;
        [BoxGroup("Scene Refs/Gameplay")]
        [SerializeField] private GameObject hitmarkerFab;
        [BoxGroup("Scene Refs/Gameplay")]
        [SerializeField] private TMP_Text healthBarText;
        [BoxGroup("Scene Refs/Gameplay")]
        [SerializeField] private TMP_Text healthBarLowerText;
        [BoxGroup("Scene Refs/Gameplay")]
        [SerializeField] private RawImage healthBarFill;
        [BoxGroup("Scene Refs/Gameplay")]
        public TMP_Text ammoText;

        [BoxGroup("Scene Refs/Respawn")]
        [SerializeField] private GameObject respawnPage;
        [BoxGroup("Scene Refs/Respawn")]
        [SerializeField] private RespawnButtonConstructor respawnButtonConstructor;

        [BoxGroup("Scene Refs/Death")]
        [SerializeField] private GameObject deathPage;
        [BoxGroup("Scene Refs/Death")]
        [SerializeField] private TMP_Text countdownText;

        private Player player;
        private HealthManager healthM;
        private RespawnManager respawnM;
        private PlayerStateMachine PSM;

        private static readonly int FillId = Shader.PropertyToID("_Fill");

        private bool initialized;
        private bool respawnButtonsBuilt;

        #region Unity Calls
        private void Awake() {
            healthBarFill.material = new Material(healthBarFill.material);
        }

        private void Update() {
            if(!initialized) { return; }

            DriveGameplay();
            DriveRespawn();
            DriveDeath();
        }
        #endregion

        #region Core
        private void DriveGameplay() {
            bool show = PSM.isPlaying && !player.IsDead;
            gamePage.SetActive(show);

            if(!show) { return; }

            healthBarText.text = Mathf.RoundToInt(healthM.currentHealth).ToString();
            healthBarLowerText.text = Mathf.RoundToInt(healthM.currentHealth).ToString();

            healthBarFill.material.SetFloat(FillId, Mathf.Clamp01((float)healthM.currentHealth / healthM.maxHealth));
        }

        private void DriveRespawn() {
            bool show = PSM.isPlaying && respawnM.IsAwaitingRespawn;
            respawnPage.SetActive(show);

            if(!show) {
                respawnButtonsBuilt = false;
                return;
            }

            if(!respawnButtonsBuilt && respawnButtonConstructor != null) {
                respawnButtonConstructor.Build(player);
                respawnButtonsBuilt = true;
            }
        }

        private void DriveDeath() {
            bool show = PSM.isPlaying && player.IsDead && !respawnM.DeathTimerElapsed;
            deathPage.SetActive(show);

            if(!show) { return; }

            countdownText.text = Mathf.CeilToInt(respawnM.deathTimeRemaining).ToString();
        }
        #endregion

        #region Public Accessors
        public void Initialize(Player Player) {
            player = Player;
            PSM = Player.PSM;
            healthM = player.healthM;
            respawnM = player.respawnM;
            initialized = true;
        }
        public void GameplayHit()
        {
            Instantiate(hitmarkerFab, parent: hitmarkerSpawn.transform);
        }
        #endregion
    }
}
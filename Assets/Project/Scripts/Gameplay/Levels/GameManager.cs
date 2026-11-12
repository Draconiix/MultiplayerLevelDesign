using System;
using System.Collections.Generic;
using PurrNet;
using UnityEngine;
using TinyInspector;
using LevelDesign.Tags;
using LevelDesign.Async;
using PlayerController = LevelDesign.Systems.Player.Player;
using TMPro;
using UnityEngine.SocialPlatforms;

namespace LevelDesign.Systems
{
    public enum GameState
    {
        LoadingIn,
        Playing,
        GameOver
    }

    public class GameManager : NetworkBehaviour
    {
        private const int MaxUsernameLength = 16;

        [SerializeField] private GameObject gameStartingUI;
        [SerializeField] private TMP_Text gameCountdownText;
        [SerializeField] private float loadInDuration = 20f;
        [SerializeField] private float matchDuration = 0f;

        [Header("Game Over")]
        [SerializeField] private GameObject gameOverUI;
        [SerializeField] private TMP_Text winnerText;
        [SerializeField] private string winnerFormat = "{0} wins!";
        [SerializeField] private string noWinnerText = "No winner";

        [Header("Debug")]
        [SerializeField] private TAG_LoadInLook activeLook;
        [SerializeField] private int activeLookIndex = -1;

        private readonly SyncVar<GameState> state = new SyncVar<GameState>(GameState.LoadingIn);
        private readonly HashSet<PlayerID> confirmedPlayers = new HashSet<PlayerID>();
        private readonly HashSet<PlayerID> defeatedPlayers = new HashSet<PlayerID>();
        private readonly Dictionary<PlayerID, string> usernames = new Dictionary<PlayerID, string>();
        private readonly List<TAG_LoadInLook> looks = new List<TAG_LoadInLook>();

        public GameState localState = GameState.LoadingIn;
        private PlayerController localPlayer;
        private HealthManager localHealth;
        private bool localDefeatReported;
        private int lookIndex;
        private bool clientSpawned;

        private bool loadInRunning;
        private float loadInElapsed;
        private float loadInTotal;

        private bool serverLoadInStarted;
        private float serverTimer;
        private int matchPlayerCount;

        private string winnerName = "";

        #region Events
        public static GameManager Instance { get; private set; }

        public event Action<GameState> OnStateEntered;
        public event Action<float> OnCountdownStarted;
        public event Action OnCanPlay;
        public event Action OnUsernamesChanged;
        public event Action<string> OnWinnerDeclared;
        #endregion

        #region Unity Calls
        private void Awake() {
            Instance = this;

            RefreshLooks();

            if(looks.Count == 0) {
                Debug.LogWarning("GameManager found no TAG_LoadInLook objects in the scene");
            }
        }

        private void Update() {
            TickLookCycle();

            if(isSpawned && isServer) {
                TickServer();
            }

            if(localState == GameState.LoadingIn)
            {
                gameStartingUI.SetActive(true);
                gameCountdownText.text = (Mathf.RoundToInt(LoadInTimeRemaining).ToString() + "s");
            }
            else
            {
                gameStartingUI.SetActive(false);
            }

            if(gameOverUI != null) {
                gameOverUI.SetActive(localState == GameState.GameOver);
            }
        }

        private void OnDestroy() {
            UnsubscribeLocalHealth();

            if(Instance == this) {
                Instance = null;
            }
        }
        #endregion

        #region Network Callbacks
        protected override void OnSpawned(bool asServer) {
            if(asServer) {
                confirmedPlayers.Clear();
                defeatedPlayers.Clear();
                usernames.Clear();
                serverLoadInStarted = false;
                matchPlayerCount = 0;
                return;
            }

            clientSpawned = true;
            EnterLocalState(state.value);

            if(localPlayer != null) {
                ConfirmInitialized();
            }
        }
        #endregion

        #region State Machine
        private void ChangeState(GameState next) {
            if(!isServer) { return; }
            if(state.value == next) { return; }

            state.value = next;
            serverLoadInStarted = false;
            serverTimer = next == GameState.Playing ? matchDuration : 0f;

            if(next == GameState.Playing) {
                matchPlayerCount = CountPlayers();
            }

            RpcStateChanged(next);
        }

        [ObserversRpc(runLocally: true)]
        private void RpcStateChanged(GameState next) {
            EnterLocalState(next);
        }

        private void EnterLocalState(GameState next) {
            localState = next;
            lookIndex = 0;
            loadInElapsed = 0f;
            loadInTotal = loadInDuration;
            loadInRunning = next == GameState.GameOver;

            ApplyCinematicForState();

            OnStateEntered?.Invoke(next);

            if(next == GameState.Playing) {
                OnCanPlay?.Invoke();
            }
        }

        private void TickServer() {
            switch(state.value) {
                case GameState.LoadingIn:
                    TickLoadingIn();
                    break;
                case GameState.Playing:
                    TickPlaying();
                    break;
            }
        }

        private void TickLoadingIn() {
            if(!serverLoadInStarted) {
                if(!AllPlayersConfirmed()) { return; }

                serverLoadInStarted = true;
                serverTimer = loadInDuration;
                RpcLoadInStarted(loadInDuration);
                return;
            }

            serverTimer -= Time.deltaTime;

            if(serverTimer <= 0f) {
                ChangeState(GameState.Playing);
            }
        }

        private void TickPlaying() {
            if(TryResolveLastPlayerStanding()) { return; }
            if(matchDuration <= 0f) { return; }

            serverTimer -= Time.deltaTime;

            if(serverTimer <= 0f) {
                EndMatch("");
            }
        }

        private bool AllPlayersConfirmed() {
            int playerCount = 0;

            foreach(PlayerID player in networkManager.players) {
                playerCount++;

                if(!confirmedPlayers.Contains(player)) {
                    return false;
                }
            }

            return playerCount > 0;
        }

        private int CountPlayers() {
            int playerCount = 0;

            foreach(PlayerID player in networkManager.players) {
                playerCount++;
            }

            return playerCount;
        }

        // Only resolves for matches that started with at least two players, so a solo session never ends instantly.
        private bool TryResolveLastPlayerStanding() {
            if(matchPlayerCount < 2) { return false; }

            int aliveCount = 0;
            PlayerID survivor = default;

            foreach(PlayerID player in networkManager.players) {
                if(!defeatedPlayers.Contains(player)) {
                    aliveCount++;
                    survivor = player;
                }
            }

            if(aliveCount > 1) { return false; }

            EndMatch(aliveCount == 1 ? GetUsername(survivor) : "");
            return true;
        }

        private void EndMatch(string winner) {
            if(!isServer) { return; }
            if(state.value == GameState.GameOver) { return; }

            RpcGameOverResult(winner);
            ChangeState(GameState.GameOver);
        }

        [ObserversRpc(runLocally: true)]
        private void RpcLoadInStarted(float duration) {
            loadInTotal = duration;
            loadInElapsed = 0f;
            loadInRunning = true;

            OnCountdownStarted?.Invoke(duration);
        }

        [ObserversRpc(runLocally: true, bufferLast: true)]
        private void RpcGameOverResult(string winner) {
            winnerName = winner ?? "";

            if(winnerText != null) {
                winnerText.text = string.IsNullOrEmpty(winnerName) ? noWinnerText : string.Format(winnerFormat, winnerName);
            }

            OnWinnerDeclared?.Invoke(winnerName);
        }
        #endregion

        #region Cinematic
        private void RefreshLooks() {
            looks.Clear();
            looks.AddRange(FindObjectsByType<TAG_LoadInLook>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID));
            looks.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
            lookIndex = 0;
        }

        private void ApplyCinematicForState() {
            if(localPlayer == null) { return; }

            if(localState == GameState.Playing) {
                activeLook = null;
                activeLookIndex = -1;
                localPlayer.ExitCinematic();
                return;
            }

            if(looks.Count == 0) {
                RefreshLooks();
            }

            ShowLook(lookIndex);
        }

        private void TickLookCycle() {
            if(localPlayer == null) { return; }
            if(localState == GameState.Playing) { return; }
            if(!loadInRunning) { return; }
            if(loadInTotal <= 0f) { return; }
            if(looks.Count == 0) { return; }

            loadInElapsed += Time.deltaTime;

            if(localState == GameState.GameOver) {
                loadInElapsed %= loadInTotal;
            } else {
                loadInElapsed = Mathf.Min(loadInElapsed, loadInTotal);
            }

            int nextIndex = Mathf.Min(Mathf.FloorToInt(LoadInPercent * looks.Count), looks.Count - 1);

            if(nextIndex == lookIndex) { return; }

            lookIndex = nextIndex;
            ShowLook(lookIndex);
        }

        private void ShowLook(int index) {
            if(looks.Count == 0) { return; }

            activeLook = looks[index];
            activeLookIndex = index;
            localPlayer.SetAndLoadCinematic(activeLook.transform);
        }
        #endregion

        #region Initialization Confirmation
        private void ConfirmInitialized() {
            string localUsername = UserData.Instance != null ? UserData.Instance.Username : "";
            ConfirmInitializedServer(localUsername);
        }

        [ServerRpc(requireOwnership: false)]
        private void ConfirmInitializedServer(string username, RPCInfo info = default) {
            confirmedPlayers.Add(info.sender);
            usernames[info.sender] = CleanUsername(username, info.sender);
            BroadcastUsernames();
        }
        #endregion

        #region Players
        private static string CleanUsername(string raw, PlayerID player) {
            if(string.IsNullOrWhiteSpace(raw)) { return FallbackUsername(player); }

            string cleaned = raw.Trim();

            if(cleaned.Length > MaxUsernameLength) {
                cleaned = cleaned.Substring(0, MaxUsernameLength);
            }

            return cleaned;
        }

        private static string FallbackUsername(PlayerID player) {
            return $"Player {player.id}";
        }

        private void BroadcastUsernames() {
            PlayerID[] ids = new PlayerID[usernames.Count];
            string[] names = new string[usernames.Count];
            int index = 0;

            foreach(KeyValuePair<PlayerID, string> entry in usernames) {
                ids[index] = entry.Key;
                names[index] = entry.Value;
                index++;
            }

            RpcUsernames(ids, names);
        }

        [ObserversRpc(bufferLast: true)]
        private void RpcUsernames(PlayerID[] ids, string[] names) {
            usernames.Clear();

            int count = Mathf.Min(ids.Length, names.Length);

            for(int i = 0; i < count; i++) {
                usernames[ids[i]] = names[i];
            }

            OnUsernamesChanged?.Invoke();
        }

        private void SubscribeLocalHealth(PlayerController player) {
            UnsubscribeLocalHealth();

            localDefeatReported = false;
            localHealth = player != null ? player.GetComponentInChildren<HealthManager>() : null;

            if(localHealth == null) {
                Debug.LogWarning("GameManager could not find a HealthManager on the local player");
                return;
            }

            localHealth.Eliminated += HandleLocalEliminated;
        }

        private void UnsubscribeLocalHealth() {
            if(localHealth == null) { return; }

            localHealth.Eliminated -= HandleLocalEliminated;
            localHealth = null;
        }

        private void HandleLocalEliminated() {
            if(localDefeatReported) { return; }
            if(!clientSpawned) { return; }

            localDefeatReported = true;
            ReportDefeatedServer();
        }

        [ServerRpc(requireOwnership: false)]
        private void ReportDefeatedServer(RPCInfo info = default) {
            defeatedPlayers.Add(info.sender);
        }
        #endregion

        #region Public Accessors
        public GameState CurrentState => state.value;
        public bool LoadInRunning => loadInRunning;
        public float LoadInPercent => loadInTotal > 0f ? loadInElapsed / loadInTotal : 0f;
        public float LoadInTimeRemaining => Mathf.Max(0f, loadInTotal - loadInElapsed);
        public string WinnerName => winnerName;
        public IReadOnlyDictionary<PlayerID, string> Usernames => usernames;

        public string GetUsername(PlayerID player) {
            if(usernames.TryGetValue(player, out string username)) { return username; }

            return FallbackUsername(player);
        }

        public bool IsDefeated(PlayerID player) {
            return defeatedPlayers.Contains(player);
        }

        public void RegisterLocalPlayer(PlayerController player) {
            localPlayer = player;

            SubscribeLocalHealth(player);
            ApplyCinematicForState();

            if(clientSpawned) {
                ConfirmInitialized();
            }
        }

        public void EndGame() {
            EndMatch("");
        }
        #endregion
    }
}
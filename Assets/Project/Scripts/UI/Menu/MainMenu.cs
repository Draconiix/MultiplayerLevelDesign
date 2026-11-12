using UnityEngine;
using TinyInspector;
using UnityEngine.UI;
using QFSW.QC;
using UnityEditor;
using PurrNet;
using PurrNet.Transports;
using LevelDesign.Async;
using LevelDesign.Async.Preferences;
using System;
using TMPro;

namespace LevelDesign.UI {
    public class MainMenu : MonoBehaviour {
        public enum MenuState {
            Main,
            Connect,
            HostedLobby,
            Customization,
            Lobby
        }

        [BoxGroup("Scene Refs")]
        public RectTransform imageDivider;
        [BoxGroup("Scene Refs")]
        public RectTransform followUpDivider;
        [BoxGroup("Scene Refs")]
        public Transform UserList;

        [BoxGroup("Scene Refs/Page Refs")]
        public GameObject mainPage;
        [BoxGroup("Scene Refs/Page Refs")]
        public GameObject connectPage;
        [BoxGroup("Scene Refs/Page Refs")]
        public GameObject hostPage;
        [BoxGroup("Scene Refs/Page Refs")]
        public GameObject lobbyPage;
        [BoxGroup("Scene Refs/Page Refs")]
        public GameObject customizationPage;

        [BoxGroup("Scene Refs/Network")]
        [HideInInspector] public PurrTransport transport;
        [BoxGroup("Scene Refs/Network")]
        [HideInInspector] public NetworkManager networkM;
        [BoxGroup("Scene Refs/Network")]
        public TMP_Text lobbyCode;
        [BoxGroup("Scene Refs/Network")]
        public TMP_Text inputText;
        [BoxGroup("Scene Refs/Network")]
        public PlayerNameTag playerNamePlate;

        [BoxGroup("Scene Refs/Status")]
        public TMP_Text statusText;
        [BoxGroup("Scene Refs/Status")]
        public Image statusLight;

        [BoxGroup("Transition")]
        public float transitionDuration = 0.5f;
        [BoxGroup("Transition")]
        public float collapsedScaleY = 1f;
        [BoxGroup("Transition")]
        public float imageExpandedScaleY = 30f;
        [BoxGroup("Transition")]
        public float followUpExpandedScaleY = 14f;

        [BoxGroup("Connection")]
        public float connectTimeout = 15f;

        [BoxGroup("Debug")]
        public MenuState menuState = MenuState.Main;

        private float imageFromY;
        private float imageToY;
        private float followUpFromY;
        private float followUpToY;
        private float transitionTime = float.MaxValue;

        private float connectElapsed;
        private bool isConnecting;

        #region Unity Calls
        private void OnEnable() {
            ResolveNetwork();

            networkM.onClientConnectionState += TryTransitionToLobby;
            networkM.onPlayerJoined += SpawnTag;
        }

        private void OnDisable() {
            if(networkM == null) { return; }

            networkM.onClientConnectionState -= TryTransitionToLobby;
            networkM.onPlayerJoined -= SpawnTag;
        }

        private void Update() {
            if(networkM == null) { return; }

            ManageStats();
            UpdateConnectTimeout();

            if(transitionTime > transitionDuration) { return; }

            transitionTime += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, transitionTime / transitionDuration);

            SetScaleY(imageDivider, Mathf.Lerp(imageFromY, imageToY, t));
            SetScaleY(followUpDivider, Mathf.Lerp(followUpFromY, followUpToY, t));

            switch(menuState) {
                case MenuState.Main:
                    mainPage.SetActive(true);
                    break;
                case MenuState.Connect:
                    connectPage.SetActive(true);
                    break;
                case MenuState.HostedLobby:
                    hostPage.SetActive(true);
                    lobbyPage.SetActive(true);
                    break;
                case MenuState.Lobby:
                    lobbyPage.SetActive(true);
                    break;
                case MenuState.Customization:
                    customizationPage.SetActive(true);
                    break;
            }
        }
        #endregion

        #region Core
        private void ResolveNetwork() {
            networkM = NetworkManager.main;
            transport = networkM.transport as PurrTransport;
        }

        private void SpawnTag(PlayerID player, bool isReconnect, bool asServer) {
            if(!asServer) { return; }

            PlayerNameTag spawned = Instantiate(playerNamePlate, UserList);
            spawned.GiveOwnership(player);
        }

        private void TransitionTo(MenuState state) {
            DisableAll();

            imageFromY = imageDivider.localScale.y;
            followUpFromY = followUpDivider.localScale.y;

            bool imageExpanded = state == MenuState.Connect || state == MenuState.HostedLobby || state == MenuState.Lobby || state == MenuState.Customization;
            imageToY = imageExpanded ? imageExpandedScaleY : collapsedScaleY;
            followUpToY = state == MenuState.Lobby || state == MenuState.HostedLobby ? followUpExpandedScaleY : collapsedScaleY;

            transitionTime = 0f;
            menuState = state;
        }

        private void DisableAll() {
            mainPage.SetActive(false);
            hostPage.SetActive(false);
            connectPage.SetActive(false);
            lobbyPage.SetActive(false);
            customizationPage.SetActive(false);
        }

        private static void SetScaleY(RectTransform target, float y) {
            Vector3 scale = target.localScale;
            scale.y = y;
            target.localScale = scale;
        }

        private string RandomCodeGenerator() {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            char[] code = new char[6];

            for(int i = 0; i < code.Length; i++) {
                code[i] = chars[UnityEngine.Random.Range(0, chars.Length)];
            }

            lobbyCode.text = new string(code);
            return new string(code);
        }

        private void ManageStats() {
            statusText.text = networkM.clientState.ToString();

            statusLight.color = (networkM.clientState == ConnectionState.Connected) ? Color.green : Color.yellow;
        }

        private void UpdateConnectTimeout() {
            if(!isConnecting) { return; }

            connectElapsed += Time.unscaledDeltaTime;
            if(connectElapsed < connectTimeout) { return; }

            isConnecting = false;
            networkM.StopClient();
            ToastController.Instance.Notify("Connection Timed Out");
        }

        private void TryTransitionToLobby(ConnectionState conn) {
            if(conn == ConnectionState.Connected) {
                isConnecting = false;

                if(menuState != MenuState.Lobby && menuState != MenuState.HostedLobby) {
                    lobbyCode.text = transport.roomName;
                    TransitionToLobby();
                }
                return;
            }

            if(conn == ConnectionState.Disconnected && isConnecting) {
                isConnecting = false;
                ToastController.Instance.Notify("Connection Failed");
            }
        }
        #endregion

        #region Public Accessors
        [Command("debug_ui_menu_transitionmain")]
        public void TransitionToMain() {
            TransitionTo(MenuState.Main);
        }

        [Command("debug_ui_menu_transitionconnect")]
        public void TransitionToConnect() {
            TransitionTo(MenuState.Connect);
        }

        [Command("debug_ui_menu_transitionhost")]
        public void TransitionToHost() {
            TransitionTo(MenuState.HostedLobby);
            transport.roomName = RandomCodeGenerator();
            networkM.StartServer();
            networkM.StartClient();
        }

        [Command("debug_ui_menu_transitionlobby")]
        public void TransitionToLobby() {
            TransitionTo(MenuState.Lobby);
        }

        public void TransitionToCustomize() {
            TransitionTo(MenuState.Customization);
        }

        [Command("debug_ui_menu_loadscene")]
        public void LoadNetworkedScene(string sceneName) {
            if(!networkM.isServer) {
                ToastController.Instance.Notify("Only The Host Can Load A Scene");
                return;
            }

            if(string.IsNullOrEmpty(sceneName)) {
                ToastController.Instance.Notify("No Scene Name");
                return;
            }

            networkM.sceneModule.LoadSceneAsync(sceneName);
        }

        public void ConnectClient() {
            if(isConnecting) { return; }

            string roomCode = inputText.text.Replace("\u200B", "").Trim().ToUpper();

            if(string.IsNullOrEmpty(roomCode)) {
                ToastController.Instance.Notify("No Available Code");
                return;
            }

            transport.roomName = roomCode;
            ToastController.Instance.Notify("Connecting");
            connectElapsed = 0f;
            isConnecting = true;
            networkM.StartClient();
        }

        public void CloseConnection() {
            isConnecting = false;
            networkM.StopClient();
            networkM.StopServer();
        }

        public void CopyLobbyCode() {
            GUIUtility.systemCopyBuffer = lobbyCode.text;
        }

        public void CloseGame() {
            Application.Quit();
        }
        #endregion
    }
}
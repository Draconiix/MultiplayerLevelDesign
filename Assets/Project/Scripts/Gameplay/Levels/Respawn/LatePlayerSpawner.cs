using System.Collections.Generic;
using PurrNet.Logging;
using PurrNet.Modules;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PurrNet
{
    public class LatePlayerSpawner : PurrMonoBehaviour
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private bool ignoreNetworkRules;
        [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

        private int currentSpawnPoint;
        private IProvideSpawnPoints spawnPointProvider;
        private IProvidePrefabInstantiated prefabInstantiatedProvider;

        #region Unity Calls
        private void Awake() {
            CleanupSpawnPoints();
        }

        private void OnEnable() {
            var main = NetworkManager.main;

            if(!main || !main.isServer) { return; }

            SubscribeToScenePlayers(main);
            SpawnForPlayersInScene(main);
        }

        private void OnDestroy() {
            if(NetworkManager.main && NetworkManager.main.TryGetModule(out ScenePlayersModule scenePlayersModule, true)) {
                scenePlayersModule.onPlayerLoadedScene -= OnPlayerLoadedScene;
            }
        }
        #endregion

        #region Subscriptions
        public override void Subscribe(NetworkManager manager, bool asServer) {
            if(!asServer) { return; }

            SubscribeToScenePlayers(manager);
            SpawnForPlayersInScene(manager);
        }

        public override void Unsubscribe(NetworkManager manager, bool asServer) {
            if(asServer && manager.TryGetModule(out ScenePlayersModule scenePlayersModule, true)) {
                scenePlayersModule.onPlayerLoadedScene -= OnPlayerLoadedScene;
            }
        }

        private void SubscribeToScenePlayers(NetworkManager manager) {
            if(!manager.TryGetModule(out ScenePlayersModule scenePlayersModule, true)) { return; }

            scenePlayersModule.onPlayerLoadedScene -= OnPlayerLoadedScene;
            scenePlayersModule.onPlayerLoadedScene += OnPlayerLoadedScene;
        }
        #endregion

        #region Core
        private void SpawnForPlayersInScene(NetworkManager manager) {
            if(!manager.TryGetModule(out ScenePlayersModule scenePlayersModule, true)) { return; }
            if(!manager.TryGetModule(out ScenesModule scenes, true)) { return; }
            if(!scenes.TryGetSceneID(gameObject.scene, out var sceneID)) { return; }
            if(!scenePlayersModule.TryGetPlayersInScene(sceneID, out var players)) { return; }

            foreach(var player in players) {
                TrySpawnPlayer(manager, scenes, player, sceneID, true);
            }
        }

        private void OnPlayerLoadedScene(PlayerID player, SceneID scene, bool asServer) {
            var main = NetworkManager.main;

            if(!asServer) { return; }
            if(!main || !main.TryGetModule(out ScenesModule scenes, true)) { return; }

            TrySpawnPlayer(main, scenes, player, scene, false);
        }

        private void TrySpawnPlayer(NetworkManager main, ScenesModule scenes, PlayerID player, SceneID scene, bool skipIfAlreadyOwned) {
            var unityScene = gameObject.scene;

            if(!scenes.TryGetSceneID(unityScene, out var sceneID)) { return; }
            if(sceneID != scene) { return; }

            bool despawnOnDisconnect = main.networkRules.ShouldDespawnOnOwnerDisconnect();
            bool respectRules = !ignoreNetworkRules && !despawnOnDisconnect;

            if((skipIfAlreadyOwned || respectRules) && PlayerOwnsPlayerPrefab(main, player)) { return; }

            GameObject newPlayer;
            NetworkIdentity identity;

            CleanupSpawnPoints();

            if(spawnPointProvider != null) {
                var point = spawnPointProvider.NextSpawnPoint(player, scene);
                newPlayer = SpawnPlayer(point.position, point.rotation, unityScene, out identity);
            } else if(spawnPoints.Count > 0) {
                var spawnPoint = spawnPoints[currentSpawnPoint];
                currentSpawnPoint = (currentSpawnPoint + 1) % spawnPoints.Count;
                newPlayer = SpawnPlayer(spawnPoint.position, spawnPoint.rotation, unityScene, out identity);
            } else {
                playerPrefab.transform.GetPositionAndRotation(out var position, out var rotation);
                newPlayer = SpawnPlayer(position, rotation, unityScene, out identity);
            }

            if(!newPlayer) { return; }

            prefabInstantiatedProvider?.OnPrefabInstantiated(newPlayer, player, scene);

            if(identity) {
                identity.GiveOwnership(player);
            }
        }

        private bool PlayerOwnsPlayerPrefab(NetworkManager main, PlayerID player) {
            if(!main.TryGetModule(out GlobalOwnershipModule ownership, true)) { return false; }
            if(main.prefabProvider == null || !main.prefabProvider.TryGetPrefabData(playerPrefab, out var prefabData)) { return false; }

            foreach(var owned in ownership.EnumerateAllPlayerOwnedIds(player)) {
                if(owned && owned.scopedPrefabId == prefabData.prefabId) { return true; }
            }

            return false;
        }

        private GameObject SpawnPlayer(Vector3 position, Quaternion rotation, Scene unityScene, out NetworkIdentity identity) {
            identity = null;

            var newPlayer = UnityProxy.Instantiate(playerPrefab, position, rotation, unityScene);

            if(!newPlayer) { return null; }
            if(!newPlayer.TryGetComponent(out identity)) { return newPlayer; }

            var nm = manager ? manager : NetworkManager.main;

            if(nm && !identity.IsSpawned(nm.isServer)) {
                nm.Spawn(newPlayer);
            }

            return newPlayer;
        }

        private void CleanupSpawnPoints() {
            bool hadNullEntry = false;

            for(int i = 0; i < spawnPoints.Count; i++) {
                if(!spawnPoints[i]) {
                    hadNullEntry = true;
                    spawnPoints.RemoveAt(i);
                    i--;
                }
            }

            if(hadNullEntry) {
                PurrLogger.LogWarning("Some spawn points were invalid and have been cleaned up.", this);
            }
        }
        #endregion

        #region Public Accessors
        public IReadOnlyList<Transform> SpawnPoints => spawnPoints;

        public void SetRespawnPointProvider(IProvideSpawnPoints provider) {
            spawnPointProvider = provider;
        }

        public void ResetSpawnPointProvider() {
            spawnPointProvider = null;
        }

        public void SetPrefabInstantiatedProvider(IProvidePrefabInstantiated provider) {
            prefabInstantiatedProvider = provider;
        }

        public void ResetPrefabInstantiatedProvider() {
            prefabInstantiatedProvider = null;
        }
        #endregion
    }
}
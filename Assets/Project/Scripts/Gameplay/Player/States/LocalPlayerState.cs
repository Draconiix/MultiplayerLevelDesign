using UnityEngine;
using PurrNet;


namespace LevelDesign.Systems.Player
{
    public class LocalPlayerState : NetworkIdentity
    {
        public LevelDesign.Systems.Player.Player playerObject;
        [SerializeField] private bool Host;
        [SerializeField] private bool DedicatedServer;

        public bool isLocalPlayer = false;

        protected override void OnSpawned()
        {
            if(Host)
            {
                isLocalPlayer = isOwner;
            }

            if(DedicatedServer)
            {
                isLocalPlayer = isOwner && !isServer;
            }

            playerObject.Initialize();
        }
    }
}

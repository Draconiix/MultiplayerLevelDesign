using UnityEngine;
using PurrNet;
using PurrNet.Transports;
using TinyInspector;
using Evo.UI;

namespace LevelDesign.UI {
    public class PlayButtonController : MonoBehaviour {
        [BoxGroup("Scene Refs")]
        [SerializeField] private Evo.UI.Button interactableButton;
        private NetworkManager networkM;

        #region Unity Calls
        private void OnEnable() {
            networkM = NetworkManager.main;
        }

        private void FixedUpdate() {
            if(networkM == null) {
                networkM = NetworkManager.main;
                if(networkM == null) { return; }
            }

            interactableButton.SetInteractable(networkM.clientState == ConnectionState.Connected);
        }
        #endregion
    }
}
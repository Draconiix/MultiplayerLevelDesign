using UnityEngine;
using TinyInspector;

namespace LevelDesign.Systems.Player
{
    public class IKManager : MonoBehaviour
    {
        [BoxGroup("Scene Refs")]
        [SerializeField] private CharacterDataManager characterDataM;
        [BoxGroup("Scene Refs")]
        [SerializeField] private Transform followConstraintMaster;

        private void Update() {
            if(characterDataM.currentMovementController == null) { return; }

            var rigInfo = characterDataM.currentMovementController._GetCurrentRigInfo();
            if(rigInfo == null) {
                Debug.Log($"IKManager Couldn't find Rig Info on {characterDataM.currentMovementController.name}");
                return;
            }

            rigInfo.followerConstraint.transform.position = followConstraintMaster.transform.position;
            rigInfo.followerConstraint.transform.rotation = followConstraintMaster.transform.rotation;
        }
    }
}

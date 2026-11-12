using UnityEngine;
using LevelDesign.Data;
using TinyInspector;

namespace LevelDesign.Systems.Player
{
    public class CharacterDataManager : MonoBehaviour
    {
        [BoxGroup("Scene Refs")]
        [SerializeField] private Transform characterSpawnRoot;
        [BoxGroup("Scene Refs")]
        [SerializeField] private Transform handSpawnRoot;
        
        [BoxGroup("Config")]
        [SerializeField] private CharacterDataSO fallbackCharacterData;

        [BoxGroup("Debug")]
        public CharacterDataSO currentCharacterData;
        [BoxGroup("Debug")]
        public GameObject currentCharacterObject;
        [BoxGroup("Debug")]
        public _MovementController currentMovementController;
        [BoxGroup("Debug")]
        public ArmController currentArmController;

        public void Initialize()
        {
        }

        public void LoadCharacterData(CharacterDataSO newCharacter){
            if(newCharacter == null || newCharacter.gameplayController == null) {
                Debug.LogError("Could Not Process Character Data Swap");
                return;
            }

            ClearCharacter();

            // Instantiate Character which then spins up the third person visuals
            currentCharacterData = newCharacter;
            currentMovementController = Instantiate(newCharacter.gameplayController, characterSpawnRoot);
            if(currentMovementController != null) {
                currentCharacterObject = currentMovementController.gameObject;
            }

            // Instantiate Arms for the character
            if(newCharacter.firstPersonArms != null) {
                currentArmController = Instantiate(newCharacter.firstPersonArms, handSpawnRoot);
                BindWeapons(currentArmController.gameObject);
            }
        }

        public void BindWeapons(GameObject root) {
            if(root == null) { return; }
            foreach(var t_weapon in root.GetComponentsInChildren<Weapon>(true)) {
                BindWeapon(t_weapon);
            }
        }

        public void BindWeapon(Weapon weapon) {
            if(weapon != null) { weapon.BindMovement(currentMovementController); }
        }

        public void ClearCharacter() {
            currentCharacterData = null;
            currentMovementController = null;

            if(currentArmController != null) {
                Destroy(currentArmController.gameObject);
                currentArmController = null;
            }

            if(currentCharacterObject != null) {
                Destroy(currentCharacterObject);
                currentCharacterObject = null;
            }
        }
    }
}
using UnityEngine;
using LevelDesign.Systems.Player;
using TinyInspector;

namespace LevelDesign.Data
{
    [CreateAssetMenu(fileName = "CharacterData", menuName = "ScriptableObjects/Character/CharacterData", order = 1)]
    public class CharacterDataSO : ScriptableObject
    {
        [BoxGroup("Gameplay Controllers")]
        public _MovementController gameplayController;
        
        [BoxGroup("Visuals")]
        public RigInfo thirdPersonVisuals;
        [BoxGroup("Visuals")]
        public ArmController firstPersonArms;

/*
        [BoxGroup("Sound")]
        public CharacterSoundData soundProfile;
*/
    }
}

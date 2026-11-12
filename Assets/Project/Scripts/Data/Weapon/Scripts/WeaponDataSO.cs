using UnityEngine;
using TinyInspector;

namespace LevelDesign.Data
{
    [CreateAssetMenu(fileName = "WeaponData", menuName = "ScriptableObjects/Weapons/WeaponData", order = 1)]
    public class WeaponDataSO : ScriptableObject
    {
        public enum FireMode
        {
            Semi,
            Burst,
            Auto
        }

        [BoxGroup("Config")]
        public FireMode fireMode;
        [BoxGroup("Config")]
        public float maxAmmo;
        [BoxGroup("Config")]
        public float ammoConsumedPerTick;
        [BoxGroup("Config")]
        public float fireRate;
        [BoxGroup("Config")]
        public float reloadTime;

        [BoxGroup("Refs")]
        public ViewModelMovementSO viewModelMove;
    
    }
}

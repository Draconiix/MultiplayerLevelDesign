using System.Collections.Generic;
using UnityEngine;

namespace TinyInspector.Demo
{
    public class DemoWeaponConfigClassic : MonoBehaviour
    {
        [Header("Identity")]
        public string weaponId;
        public string displayName;

        [Header("Type")]
        public WeaponType weaponType;
        public FireMode fireMode;

        [Header("Visuals & Audio")]
        public GameObject weaponModel;
        public AudioClip fireSound;

        [Header("Stats")]
        [SerializeField]
        private DamageData damage;

        [SerializeField]
        private RecoilData recoil;

        [SerializeField]
        private AccuracyData accuracy;

        [SerializeField]
        private AmmoData ammo;

        [Header("Modifiers")]
        public List<WeaponModifier> modifiers;
    }

    public enum WeaponType
    {
        Melee,
        Pistol,
        Rifle,
        Shotgun,
        Sniper,
        Special
    }
    [System.Serializable]
    public class DamageData
    {
        public int baseDamage;
        public float headshotMultiplier;
        public float armorPenetration;
    }
    [System.Serializable]
    public class RecoilData
    {
        public float verticalKick;
        public float horizontalKick;
        public float returnSpeed;
    }
    [System.Serializable]
    public class AccuracyData
    {
        public float baseSpread;
        public float spreadIncreasePerShot;
        public float maxSpread;
        public float spreadRecovery;
    }
    [System.Serializable]
    public class AmmoData
    {
        public bool usesAmmo;

        public int magazineSize;
        public int reserveAmmo;

        public float reloadTime;
    }
    [System.Serializable]
    public class WeaponModifier
    {
        public string id;
        public ModifierType type;
        public float value;
    }
    public enum ModifierType
    {
        Damage,
        FireRate,
        Recoil,
        Accuracy,
        ReloadSpeed,
        Range
    }
    public enum FireMode
    {
        Automatic,
        Burst,
        Single
    }


}
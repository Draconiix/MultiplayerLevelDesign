using System.Collections.Generic;
using UnityEngine;
using TinyInspector;

namespace TinyInspector.Demo
{
    public class DemoWeaponConfig : MonoBehaviour
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

        [Header("Damage")]
        [SerializeField, InlineDrawer]
        private DamageData damage;

        [Header("Recoil")]
        [SerializeField, InlineDrawer]
        private RecoilData recoil;

        [Header("Accuracy")]
        [SerializeField, InlineDrawer]
        private AccuracyData accuracy;

        [Header("Ammo")]
        [SerializeField, InlineDrawer]
        private AmmoData ammo;

        [Header("Modifiers")]
        public List<WeaponModifier> modifiers;
    }
}
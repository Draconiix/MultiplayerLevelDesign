using System.Collections.Generic;
using UnityEngine;

namespace TinyInspector.Demo
{
    public class DemoPlayerStatsClassic : MonoBehaviour
    {
        [Header("Identity")]
        public string playerName = "Some Name";
        public string characterClass = "Some Class";

        [Header("Vitals")]
        [Range(0, 100)]
        public int health = 100;

        [Range(0, 100)]
        public int stamina = 50;

        public Vector2 manaRange = new Vector2(5, 10);

        [Header("Stats")]
        public List<StatEntry> stats;

        [Header("Inventory")]
        public List<GameObject> inventory;

        [Header("Combat")]
        [Range(0, 100)]
        public int strength = 20;
    }

    [System.Serializable]
    public class StatEntry
    {
        public string statName;
        public int baseValue;
        public int bonusValue;
    }
}
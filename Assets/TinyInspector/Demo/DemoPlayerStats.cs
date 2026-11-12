using System.Collections.Generic;
using UnityEngine;
using TinyInspector;

namespace TinyInspector.Demo
{
    public class NewBehaviourScript : MonoBehaviour
    {
        [BoxGroup("Identity")]
        public string playerName = "Some Name";
        [BoxGroup("Identity")]
        public string characterClass = "Some Class";

        [ProgressBar(0,100, Color: TinyColor.Red),BoxGroup("Vitals")]
        public int health = 100;
        [ProgressBar(0,100, Color: TinyColor.Blue),BoxGroup("Vitals")]
        public int stamina = 50;
        [MinMaxSlider(0,20),BoxGroup("Vitals")]
        public Vector2 manaRange = new Vector2(5, 10);

        [TableList]
        public List<StatEntry> stats;
        [Reorderable]
        public List<GameObject> inventory;


        [BoxGroup("Combat")]
        public int strength = 20;
    }
}
using UnityEngine;

namespace LevelDesign.Systems.Player
{
    public class PlayerStateMachine : MonoBehaviour
    {
        public bool isDead;
        public bool awaitingRespawn;
        public bool isCinematic;

        public bool isPlaying;
        
        public bool overrideInput;

        public void FixedUpdate()
        {
            if(isCinematic || isDead)
            {
                overrideInput = true;
            }
            else
            {
                overrideInput = false;
            }
        }
    }
}

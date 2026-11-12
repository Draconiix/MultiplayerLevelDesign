using UnityEngine;

namespace LevelDesign.Systems.Player
{
    public class Hurtbox : MonoBehaviour
    {
        private HealthManager health;

        public void Trigger(float amount, bool isDamage)
        {
            if(!isDamage) { return; }

            if(health == null) {
                Player player = GetComponentInParent<Player>();
                health = player != null ? player.healthM : null;
            }

            if(health == null) { return; }

            health.TakeDamage(amount);
        }
    }
}
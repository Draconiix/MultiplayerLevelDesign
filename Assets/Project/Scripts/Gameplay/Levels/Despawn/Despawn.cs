using PurrNet;
using UnityEngine;

namespace LevelDesign.Systems {
    public class Despawn : NetworkBehaviour {
        [SerializeField] private float despawnTime;

        #region Unity Calls
        private void Update() {
            if(isSpawned && !isServer) { return; }

            despawnTime -= Time.deltaTime;

            if(despawnTime <= 0) {
                Destroy(gameObject);
            }
        }
        #endregion
    }
}
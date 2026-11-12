using System;
using Unity.VisualScripting;
using UnityEngine;

namespace LevelDesign.Systems
{
    public class DespawnClient : MonoBehaviour
    {
        [SerializeField] private float despawnTime;

        private void Start() {
            Destroy(gameObject, despawnTime);
        }
    }
}
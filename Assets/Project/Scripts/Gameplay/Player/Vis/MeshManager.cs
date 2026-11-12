using UnityEngine;
using QFSW.QC;

namespace LevelDesign.Systems.Player
{
    public class MeshManager : MonoBehaviour
    {
        public bool disableMeshes;
        private bool overwritten;
        
        private void Update()
        {
            if(overwritten) { return; }

            foreach(var renderer in GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = !disableMeshes;
            }
        }

        [Command("debug_mesh_forceon")]
        private void ForceEnableMeshes() {
            overwritten = true;

            foreach(var renderer in GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = true;
            }
        }

        [Command("debug_mesh_forceoff")]
        private void ReleaseOverwriteMesh() {
            overwritten = false;
        }
    }
}
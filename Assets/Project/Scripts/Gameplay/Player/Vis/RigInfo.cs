using UnityEngine;
using TinyInspector;

// Summary
// Script used for storing and accessing components on the visualized rig

namespace LevelDesign.Systems.Player
{
    public class RigInfo : MonoBehaviour
    {
        [BoxGroup("Scene Refs")]
        public Transform followerConstraint;
        [BoxGroup("Scene Refs/Components")]
        public Animator characterAnimator;
        [BoxGroup("Scene Refs/Components")]
        public _PlayerAnimation playerAnimation;
        [BoxGroup("Scene Refs/Components")]
        public MeshManager meshManager;

        [BoxGroup("Controls")]
        public bool localPlayer = false;
    }
}

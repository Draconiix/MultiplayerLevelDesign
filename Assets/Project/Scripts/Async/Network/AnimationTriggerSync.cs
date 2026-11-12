using PurrNet;
using UnityEngine;

namespace LevelDesign.Async
{
    public class AnimatorTriggerSync : NetworkBehaviour
    {
        [SerializeField] private Animator animator;

        void Awake()
        {
            if(!animator) { animator = GetComponentInChildren<Animator>(); }
        }

        public void SendTrigger(string triggerName)
        {
            if(!isOwner) { return; }
            SendTriggerRpc(Animator.StringToHash(triggerName));
        }

        [ServerRpc]
        void SendTriggerRpc(int hash) => ApplyTriggerRpc(hash);

        [ObserversRpc(excludeOwner: true)]
        void ApplyTriggerRpc(int hash)
        {
            if(animator) { animator.SetTrigger(hash); }
        }
    }
}
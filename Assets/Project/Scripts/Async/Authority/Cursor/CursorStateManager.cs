using System.Collections.Generic;
using UnityEngine;

/// Summary
/// A locking and unlocking request tool 
/// Free and lock the cursor to the game window 
/// 
/// Use:
/// CursorStateManager.Instance.RequestUnlock(this) and  CursorStateManager.Instance.RelinquishRequest(this)
///
/// Ensure:
/// Relinquish your request in the OnDestroy() Method, to ensure no floating remnants

namespace LevelDesign.Async.Auth
{
    public class CursorStateManager : MonoBehaviour
    {
        public static CursorStateManager Instance { get; private set; }

        [Header("Debug")]
        [SerializeField] private bool cursorLocked;
        
        [Space]
        public List<MonoBehaviour> scriptsRequestingUnlock = new List<MonoBehaviour>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
        
            Instance = this;
        }

        public void FixedUpdate()
        {
            scriptsRequestingUnlock.RemoveAll(script => script == null);

            if (scriptsRequestingUnlock.Count > 0)
            {
                cursorLocked = false;
                UnityEngine.Cursor.lockState = CursorLockMode.Confined;
                UnityEngine.Cursor.visible = true;
            }
            else
            {
                cursorLocked = true;
                UnityEngine.Cursor.lockState = CursorLockMode.Locked;
                UnityEngine.Cursor.visible = false;
            }
        }

        #region Public Accessors
        public void RequestUnlock(MonoBehaviour requestingScript)
        {
            if (requestingScript != null && !scriptsRequestingUnlock.Contains(requestingScript))
            {
                scriptsRequestingUnlock.Add(requestingScript);
            }
        }

        public void RelinquishRequest(MonoBehaviour requestingScript)
        {
            if (requestingScript != null)
            {
                scriptsRequestingUnlock.Remove(requestingScript);
            }
        }
        #endregion

        private void OnDestroy()
        {
            if (Instance == this) { Instance = null; }
        }
    }
}
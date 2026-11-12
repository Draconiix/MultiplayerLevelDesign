using System.Collections;
using System.Collections.Generic;
using LevelDesign.Async.Auth;
using UnityEngine;

namespace LevelDesign.UI {
    public class uiAuth : _InputAuth
    {
        [Header("Config")]
        public bool requestInput;
        public bool requestCursor;

        private Coroutine requestCursorRoutine;
        private Coroutine requestInputRoutine;

        void OnEnable()
        {
            if(requestCursor) { requestCursorRoutine = _WhenCursorStateManagerReady(() => CursorStateManager.Instance.RequestUnlock(this)); }
            if(requestInput) { requestInputRoutine = _WhenInputAuthManagerReady(() => InputAuthManager.Instance.RequestInput(this)); }
        }

        void OnDisable()
        {
            if(requestCursorRoutine != null)
            {
                StopCoroutine(requestCursorRoutine);
                requestCursorRoutine = null;
            }

            if(requestInputRoutine != null)
            {
                StopCoroutine(requestInputRoutine);
                requestInputRoutine = null;
            }

            if(requestCursor && CursorStateManager.Instance != null) { CursorStateManager.Instance.RelinquishRequest(this); }
            if(requestInput && InputAuthManager.Instance != null) { InputAuthManager.Instance.RelinquishRequest(this); }
        }
    }
}
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using LevelDesign.Data;
using TinyInspector;
using System;

// Summary: The 

namespace LevelDesign.Async.Auth {
    public abstract class _InputAuth : MonoBehaviour
    {
        [BoxGroup("InputAuth/Config")]
        public List<InputFIlterSO> _inputChannels = new List<InputFIlterSO>();
        
        [BoxGroup("InputAuth/Debug")]
        public bool _inputAuthorized;

        private bool _requestingInput;
        
        protected PlayerInputActions _inputActions;
        protected PlayerInputActions.GameplayActions _input;

        #region Input Auth Integration
        public void aGrantInput()
        {
            _inputAuthorized = true;
            aOnInputGranted();
        }

        public void aDenyInput()
        {
            _inputAuthorized = false;
            aOnInputDenied();
        }

        protected virtual void aOnInputGranted() { }
        protected virtual void aOnInputDenied() { }
        #endregion

        #region Input Auth Manager Integration
        public void aInputInit(bool autoPopulateGame = false)
        {
            if (InputAuthManager.Instance != null)
            {
                _inputActions = InputAuthManager.Instance.InputActions;

                if(autoPopulateGame == true) { _input = _inputActions.Gameplay; }
            }
            else
            {
                Debug.LogWarning("Input Auth Manager could not be found. Waiting until it is ready.");
                _WhenInputAuthManagerReady(() => aInputInit(autoPopulateGame));
            }
        }

        protected Coroutine _WhenInputAuthManagerReady(Action onReady)
        {
            return _WhenReady(() => InputAuthManager.Instance != null, onReady);
        }

        protected Coroutine _WhenCursorStateManagerReady(Action onReady)
        {
            return _WhenReady(() => CursorStateManager.Instance != null, onReady);
        }

        private Coroutine _WhenReady(Func<bool> isReady, Action onReady)
        {
            if(isReady())
            {
                onReady();
                return null;
            }

            return StartCoroutine(_WaitUntilReady(isReady, onReady));
        }

        private IEnumerator _WaitUntilReady(Func<bool> isReady, Action onReady)
        {
            yield return new WaitUntil(isReady);
            onReady();
        }
        #endregion
    }
}
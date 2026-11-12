using UnityEngine;
using TMPro;
using LevelDesign.Systems.Player;

namespace LevelDesign.UI {
    public class StackDisplay : MonoBehaviour {
        [SerializeField] private TMP_Text text;
        [SerializeField] private StackManager stacks;
        [SerializeField] private StackType type;

        #region Unity Calls
        private void OnEnable() {
            Subscribe();
            Refresh();
        }

        private void OnDisable() {
            Unsubscribe();
        }
        #endregion

        #region Core
        private void Subscribe() {
            if(stacks == null) { return; }

            stacks.Changed += OnStackChanged;
        }

        private void Unsubscribe() {
            if(stacks == null) { return; }

            stacks.Changed -= OnStackChanged;
        }

        private void OnStackChanged(StackType changedType, int amount, int delta) {
            if(changedType != type) { return; }

            Refresh();
        }
        #endregion

        #region Public Accessors
        public void SetSource(StackManager source) {
            Unsubscribe();
            stacks = source;

            if(isActiveAndEnabled) {
                Subscribe();
            }

            Refresh();
        }

        public void Refresh() {
            if(text == null) { return; }

            if(stacks == null) {
                text.text = string.Empty;
                return;
            }

            text.text = stacks.IsUnlimited(type) ? "∞" : ("+" + stacks.Get(type).ToString());
        }
        #endregion
    }
}
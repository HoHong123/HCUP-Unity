using UnityEngine;

namespace HUI.ButtonUI {
    [RequireComponent(typeof(DelegateButton))]
    public abstract class BaseOnPressButton : MonoBehaviour, IDelegateButton {
        protected DelegateButton Button;

        protected virtual void Awake() {
            Button = GetComponent<DelegateButton>();
            Button.OnPointDown -= OnPointDown;
            Button.OnPointDown += OnPointDown;
            Button.OnPointUp -= OnPointUp;
            Button.OnPointUp += OnPointUp;
        }

        private void OnDestroy() {
            Button.OnPointDown -= OnPointDown;
            Button.OnPointUp -= OnPointUp;
        }


        public abstract void OnPointDown();
        public abstract void OnPointUp();
    }
}

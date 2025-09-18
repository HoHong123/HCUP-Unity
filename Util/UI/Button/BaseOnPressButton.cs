using UnityEngine;

namespace Util.UI.ButtonUI {
    [RequireComponent(typeof(DelegateButton))]
    public abstract class BaseOnPressButton : MonoBehaviour, IDelegateButton {
        private void Awake() {
            DelegateButton button = GetComponent<DelegateButton>();
            button.OnPointDown += OnPointDown;
            button.OnPointUp += OnPointUp;
        }


        public abstract void OnPointDown();
        public abstract void OnPointUp();
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 버튼 입력 상태에 따라 특정 오브젝트의 활성화 상태를 제어하는 컴포넌트입니다.
 * 버튼 입력에 따른 UI 오브젝트 표시 상태를 간단하게 제어하기 위해 사용됩니다.
 * EnableUiEntity를 사용하여 여러 오브젝트의 활성화를 동시에 관리할 수 있습니다.
 * =========================================================
 */
#endif

using UnityEngine;
using HUI.Entity;
using HInspector;

namespace HUI.ButtonUI {
    /// <summary>
    /// 버튼의 Down / Up / Interactive / NonInteractive 이벤트에 따라 지정된 오브젝트들을 활성화 혹은 비활성화한다.
    /// </summary>
    [RequireComponent(typeof(DelegateButton))]
    public class EnableOnPressButton : BaseOnPressButton {
        #region Fields
        [HTitle("Mode")]
        [SerializeField]
        ButtonEventMode interactionMode = ButtonEventMode.UsePress;

        [HTitle("Targets")]
        [HListDrawer]
        [SerializeField]
        EnableUiEntity[] targets;

        [HTitle("Interaction Targets")]
        [HShowIf("@interactionMode == ButtonEventMode.UseInteraction")]
        [HListDrawer]
        [SerializeField]
        EnableUiEntity[] interactionTargets;
        #endregion

        #region Unity Life Cycle
        protected override void Awake() {
            useInteractionChangeEvent = interactionMode == ButtonEventMode.UseInteraction;
            base.Awake();
        }
        #endregion

        #region Point Events
        public override void OnPointDown() {
            if (targets == null) return;
            foreach (var target in targets) {
                if (target == null) continue;
                bool active = target.enableOnDown;
                target.Target.SetActive(active);
            }
        }

        public override void OnPointUp() {
            if (targets == null) return;
            foreach (var target in targets) {
                if (target == null) continue;
                bool active = target.enableOnUp;
                target.Target.SetActive(active);
            }
        }
        #endregion

        #region Interaction Events
        public override void OnButtonInteractive() {
            switch (interactionMode) {
            case ButtonEventMode.UsePress:
                OnPointUp();
                break;
            case ButtonEventMode.UseInteraction:
                _DoInteraction(interactionTargets, true);
                break;
            }
        }

        public override void OnButtonNonInteractive() {
            switch (interactionMode) {
            case ButtonEventMode.UsePress:
                OnPointDown();
                break;
            case ButtonEventMode.UseInteraction:
                _DoInteraction(interactionTargets, false);
                break;
            }
        }


        private void _DoInteraction(EnableUiEntity[] list, bool interactive) {
            if (list == null) return;
            foreach (var target in list) {
                if (target == null) continue;
                bool active = interactive ? target.enableOnInteractive : target.enableOnNonInteractive;
                target.Target.SetActive(active);
            }
        }
        #endregion
    }

}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 버튼 입력에 따라 UI Graphic의 색상을 변경하는 컴포넌트입니다.
 * 버튼 입력에 대한 시각적 피드백을 제공하기 위해 사용됩니다.
 * ColorUiEntity를 사용하여 여러 UI 요소의 색상을 동시에 제어할 수 있습니다.
 *
 * 기능 ::
 * - Pointer Down 시 색상 변경
 * - Pointer Up 시 색상 복원
 * - Interaction 상태에 따른 색상 변경 지원
 * =========================================================
 */
#endif

using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using HUI.Entity;
using HInspector;

namespace HUI.ButtonUI {
    public class ColorOnPressButton : BaseOnPressButton {
        #region Fields
        [HTitle("Mode")]
        [SerializeField]
        ButtonEventMode interactionMode = ButtonEventMode.UsePress;

        [HTitle("Target")]
        [SerializeField]
        ColorUiEntity[] targets;

        [HTitle("Interaction Targets")]
        [HShowIf("@interactionMode == ButtonEventMode.UseInteraction")]
        [SerializeField]
        ColorUiEntity[] interactionTargets;
        #endregion

        #region Properties
        public ColorUiEntity[] ColorEntities => targets;
        public ColorUiEntity[] InteractionEntities => interactionTargets;
        #endregion

        #region Setter
        public void SetColor(MaskableGraphic target, Color color) {
            targets.First(t => t.Graphic == target).SetColor(color);
        }
        public void SetColor(int targetIndex, Color color) {
            Assert.IsTrue(targetIndex >= 0 && targetIndex < targets.Length, $"[ColorOnPressButton] target index({targetIndex} out of range.)");
            targets[targetIndex].SetColor(color);
        }
        #endregion

        #region Unity Life Cycle
        protected override void Awake() {
            useInteractionChangeEvent = interactionMode == ButtonEventMode.UseInteraction;
            base.Awake();
        }
        #endregion

        #region Point Down/Up
        public override void OnPointDown() {
            Assert.IsNotNull(targets, "[ColorOnPressButton] target list is null");
            foreach (var target in targets) {
                target.Dye();
            }
        }

        public override void OnPointUp() {
            Assert.IsNotNull(targets, "[ColorOnPressButton] target list is null");
            foreach (var target in targets) {
                target.Reset();
            }
        }
        #endregion

        #region Interaction
        public override void OnButtonInteractive() {
            switch (interactionMode) {
            case ButtonEventMode.UsePress: OnPointUp(); break;
            case ButtonEventMode.UseInteraction: _InteractionReset(); break;
            default: break;
            }
        }

        public override void OnButtonNonInteractive() {
            switch (interactionMode) {
            case ButtonEventMode.UsePress: OnPointDown(); break;
            case ButtonEventMode.UseInteraction: _InteractionDye(); break;
            default: break;
            }
        }


        private void _InteractionDye() {
            if (interactionTargets == null)
                return;

            foreach (var target in interactionTargets) {
                if (target == null)
                    continue;
                target.Dye();
            }
        }

        private void _InteractionReset() {
            if (interactionTargets == null)
                return;

            foreach (var target in interactionTargets) {
                if (target == null)
                    continue;
                target.Reset();
            }
        }
        #endregion

        #region Debug
#if UNITY_EDITOR
        [HTitle("Debug / Press")]
        [HButton("Press Down")]
        private void _DebugPressDown() => OnPointDown();
        [HButton("Press Up")]
        private void _DebugPressUp() => OnPointUp();

        [HTitle("Debug / Interaction")]
        [HButton("Set Interactive = true")]
        private void _DebugSetInteractiveTrue() {
            if (Button == null) Button = GetComponent<DelegateButton>();
            UnityEngine.Debug.Assert(Button != null, "[ColorOnPressButton] DelegateButton is null.");
            Button.Interaction = true;
        }

        [HButton("Set Interactive = false")]
        private void _DebugSetInteractiveFalse() {
            if (Button == null) Button = GetComponent<DelegateButton>();
            UnityEngine.Debug.Assert(Button != null, "[ColorOnPressButton] DelegateButton is null.");
            Button.Interaction = false;
        }

        [HButton("Mode = UsePressColors")]
        private void _DebugSetModeUsePressColors() {
            interactionMode = ButtonEventMode.UsePress;
            useInteractionChangeEvent = true;
            ConnectButton();
        }

        [HButton("Mode = UseCustomInteractionColors")]
        private void _DebugSetModeUseCustomInteractionColors() {
            interactionMode = ButtonEventMode.UseInteraction;
            useInteractionChangeEvent = true;
            ConnectButton();
        }
#endif
        #endregion
    }
}

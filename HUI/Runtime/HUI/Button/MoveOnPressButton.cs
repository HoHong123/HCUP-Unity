#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 버튼 입력에 따라 UI 오브젝트 위치를 변경하는 컴포넌트입니다.
 * 버튼 입력 시 위치 기반 UI 피드백을 제공하기 위해 사용됩니다.
 * MovingUiEntity를 사용하여 여러 UI 요소의 이동을 동시에 제어할 수 있습니다.
 * =========================================================
 */
#endif

using UnityEngine;
using HUI.Entity;
using HInspector;

namespace HUI.ButtonUI {
    public class MoveOnPressButton : BaseOnPressButton {
        [HTitle("Target")]
        [SerializeField]
        MovingUiEntity[] targets;


        public override void OnPointDown() {
            foreach (var target in targets) {
                target.Move();
            }
        }

        public override void OnPointUp() {
            foreach (var target in targets) {
                target.Reset();
            }
        }
    }
}

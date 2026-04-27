#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 버튼 입력에 따라 UI 오브젝트의 크기를 변경하는 컴포넌트입니다.
 * 버튼 입력에 대한 Scale 기반 시각적 피드백을 제공하기 위해 사용됩니다.
 * ScalingUiEntity를 사용하여 여러 UI 요소의 크기 변화를 동시에 제어할 수 있습니다.
 * =========================================================
 */
#endif

using UnityEngine;
using Sirenix.OdinInspector;
using HUI.Entity;

namespace HUI.ButtonUI {
    public class ScaleOnPressButton : BaseOnPressButton {
        [Title("Targets")]
        [SerializeField]
        ScalingUiEntity[] targets;


        public override void OnPointDown() {
            foreach (var target in targets) {
                target.Scale();
            }
        }

        public override void OnPointUp() {
            foreach (var target in targets) {
                target.Reset();
            }
        }
    }
}

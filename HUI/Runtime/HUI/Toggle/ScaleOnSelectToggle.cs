#if UNITY_EDITOR
/* =========================================================
 * Toggle 상태에 따라 UI 스케일을 변경하는 컴포넌트입니다.
 * =========================================================
 */
#endif

using UnityEngine;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using HUI.Entity;

namespace HUI.ToggleUI {
    public class ScaleOnSelectToggle : BaseCustomToggle {
        [Title("Targets")]
        [SerializeField]
        ScalingUiEntity[] targets;


        public override void OnToggleActive(bool isOn, bool immediate) {
            if (activateOnSelect) _Scale(isOn, immediate);
        }

        public override void OnPointerDown(PointerEventData eventData) {
            if (activateOnPointerDown) _Scale(true, immediate: false);
        }

        public override void OnPointerUp(PointerEventData eventData) {
            if (activateOnPointerUp) _Scale(false, immediate: false);
        }


        private void _Scale(bool isOn, bool immediate) {
            foreach (var target in targets) {
                if (isOn)   target.Scale(immediate);
                else        target.Reset(immediate);
            }
        }
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 *
 * 주요 기능 ::
 * Toggle 상태 기반 UI 스케일 애니메이션
 *
 * 적용 대상 ::
 * ScalingUiEntity[]
 *
 * 사용법 ::
 * targets 배열에 ScalingUiEntity 등록
 *
 * 기타 ::
 * Toggle 이벤트와 Pointer 이벤트를 모두 지원합니다.
 * =========================================================
 */
#endif
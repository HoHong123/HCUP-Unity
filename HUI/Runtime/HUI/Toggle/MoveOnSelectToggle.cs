#if UNITY_EDITOR
/* =========================================================
 * Toggle 상태에 따라 UI 위치를 이동시키는 컴포넌트입니다.
 * =========================================================
 */
#endif

using UnityEngine;
using UnityEngine.EventSystems;
using HUI.Entity;
using HInspector;

namespace HUI.ToggleUI {
    public class MoveOnSelectToggle : BaseCustomToggle {
        [HTitle("Targets")]
        [SerializeField]
        MovingUiEntity[] targets;


        public override void OnToggleActive(bool isOn, bool immediate) {
            if (activateOnSelect)
                _Move(isOn, immediate);
        }
        public override void OnPointerDown(PointerEventData eventData) {
            if (activateOnPointerDown)
                _Move(true, immediate: false);
        }
        public override void OnPointerUp(PointerEventData eventData) {
            if (activateOnPointerUp)
                _Move(false, immediate: false);
        }


        private void _Move(bool isOn, bool immediate) {
            foreach (var target in targets) {
                if (isOn)   target.Move(immediate);
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
 * Toggle 상태 기반 UI 이동 애니메이션
 *
 * 적용 대상 ::
 * MovingUiEntity[]
 *
 * 사용법 ::
 * targets 배열에 MovingUiEntity 등록
 *
 * 기타 ::
 * Toggle 이벤트 및 Pointer 이벤트를 통해 이동 트리거 시점을 설정할 수 있습니다.
 * =========================================================
 */
#endif
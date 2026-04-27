#if UNITY_EDITOR
/* =========================================================
 * Toggle 상태에 따라 UI 색상을 변경하는 컴포넌트입니다.
 * =========================================================
 */
#endif

using UnityEngine;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using HUI.Entity;

namespace HUI.ToggleUI {
    public class ColorOnSelectToggle : BaseCustomToggle {
        [Title("Targets")]
        [SerializeField]
        ColorUiEntity[] targets;

        public ColorUiEntity[] ColorEntities => targets;


        public override void OnToggleActive(bool isOn, bool immediate) {
            if (activateOnSelect) _Dye(isOn, immediate);
        }
        public override void OnPointerDown(PointerEventData eventData) {
            if (activateOnPointerDown) _Dye(true, immediate: false);
        }
        public override void OnPointerUp(PointerEventData eventData) {
            if (activateOnPointerUp) _Dye(false, immediate: false);
        }


        private void _Dye(bool isOn, bool immediate = false) {
            foreach (var target in targets) {
                if (isOn)   target.Dye(immediate);
                else        target.Reset(immediate);
            }
        }
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH 2026.03.10
 *
 * 주요 기능 ::
 * Toggle 상태에 따라 UI 색상 변경
 *
 * 적용 대상 ::
 * ColorUiEntity[]
 *
 * 사용법 ::
 * targets 배열에 ColorUiEntity를 등록합니다.
 *
 * 동작 ::
 * Toggle On → Dye
 * Toggle Off → Reset
 *
 * 기타 ::
 * activateOnSelect / Pointer 옵션으로
 * 이벤트 트리거 타이밍을 조절할 수 있습니다.
 * =========================================================
 */
#endif
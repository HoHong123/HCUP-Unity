#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Toggle 상태에 따라 대상 오브젝트를 From 지점과 To 지점 사이에서 이동시키는 컴포넌트입니다.
 *
 * 특징 / 지원기능 ::
 * + isOn 이면 To 지점, 꺼지면 From 지점으로 이동합니다.
 * + 부모 이전 여부는 TransitUiEntity 의 reparentOnArrive 로 지정합니다.
 *
 * 주의사항 ::
 * 트리거 시점(Select / PointerDown / PointerUp)은 BaseCustomToggle 의 Event Timing 설정을 따릅니다.
 * =========================================================
 */
#endif

using UnityEngine;
using UnityEngine.EventSystems;
using HUI.Entity;
using HInspector;

namespace HUI.ToggleUI {
    public class TransitOnSelectToggle : BaseCustomToggle {
        #region Fields
        [HTitle("Targets")]
        [SerializeField]
        TransitUiEntity[] targets;
        #endregion

        #region Public - Callbacks
        public override void OnToggleActive(bool isOn, bool immediate) {
            if (activateOnSelect) _Transit(isOn, immediate);
        }

        public override void OnPointerDown(PointerEventData eventData) {
            if (activateOnPointerDown) _Transit(true, immediate: false);
        }

        public override void OnPointerUp(PointerEventData eventData) {
            if (activateOnPointerUp) _Transit(false, immediate: false);
        }
        #endregion

        #region Private
        private void _Transit(bool toDestination, bool immediate) {
            foreach (var target in targets) {
                if (toDestination) target.MoveToDestination(immediate);
                else target.MoveToOrigin(immediate);
            }
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.09.15 TransitOnSelectToggle 베이스 코드 생성
 *
 * # 목적
 * - MoveOnSelectToggle 과 같은 트리거 구조로, 이동 방식만 TransitUiEntity(두 Transform 지점 + 선택적 부모 이전)로 바꾼 토글.
 *
 * # 사용 흐름
 * - Toggle 과 같은 GameObject 에 붙이고 targets 에 TransitUiEntity 를 등록한다.
 * - Toggle 변경 → OnToggleActive → isOn 이면 MoveToDestination, 아니면 MoveToOrigin.
 * - OnEnable 의 SyncToToggleState 로 활성화 시 현재 isOn 에 맞는 지점으로 이동한다.
 *
 * =============================================================================
 */
#endif

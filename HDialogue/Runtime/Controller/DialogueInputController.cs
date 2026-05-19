#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- HDialogue 전용 키보드·게임패드 입력 컨트롤러 (HCUP-2.5.0 Phase 2).
 *
 * 특징 / 지원기능 ::
 * + "Dialogue" ActionMap OnEnable/OnDisable 자동 활성·비활성
 * + Advance / Skip / AutoToggle performed → 이벤트 발화
 *
 * 주의사항 ::
 * 이벤트 발화만 담당. 비즈니스 로직은 DialogueManager._Bind().
 * inputActions Inspector 슬롯에 DialogueInputActions 에셋 연결 필요.
 * =========================================================
 */
#endif

using System;
using HInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HDialogue {
    public sealed class DialogueInputController : MonoBehaviour {
        #region Fields
        [HTitle("Input")]
        [SerializeField]
        InputActionAsset inputActions;

        InputAction advanceAction;
        InputAction skipAction;
        InputAction autoToggleAction;
        #endregion

        #region Events
        public event Action OnInputAdvance;
        public event Action OnInputSkip;
        public event Action OnInputAutoToggle;
        #endregion

        #region Unity Life Cycle
        private void OnEnable() {
            if (inputActions == null) return;
            var map = inputActions.FindActionMap("Dialogue", throwIfNotFound: false);
            if (map == null) return;

            advanceAction = map.FindAction("Advance", throwIfNotFound: false);
            skipAction = map.FindAction("Skip", throwIfNotFound: false);
            autoToggleAction = map.FindAction("AutoToggle", throwIfNotFound: false);

            if (advanceAction != null) advanceAction.performed += _OnAdvance;
            if (skipAction != null) skipAction.performed += _OnSkip;
            if (autoToggleAction != null) autoToggleAction.performed += _OnAutoToggle;

            map.Enable();
        }

        private void OnDisable() {
            if (advanceAction != null) advanceAction.performed -= _OnAdvance;
            if (skipAction != null) skipAction.performed -= _OnSkip;
            if (autoToggleAction != null) autoToggleAction.performed -= _OnAutoToggle;

            if (inputActions == null) return;
            var map = inputActions.FindActionMap("Dialogue", throwIfNotFound: false);
            map?.Disable();
        }
        #endregion

        #region Private
        private void _OnAdvance(InputAction.CallbackContext ctx) => OnInputAdvance?.Invoke();
        private void _OnSkip(InputAction.CallbackContext ctx) => OnInputSkip?.Invoke();
        private void _OnAutoToggle(InputAction.CallbackContext ctx) => OnInputAutoToggle?.Invoke();
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.19 (최초 설계) :: DialogueInputController 생성 — HCUP-2.5.0 Phase 2
 *
 * # 목적
 * - Unity Input System → HDialogue 이벤트 브릿지.
 *   DialogueUiController(버튼 이벤트)와 동일 패턴으로 Manager가 양쪽 구독.
 *
 * # 설계 결정
 * - OnEnable/OnDisable: GO 활성 상태와 ActionMap 활성 상태 동기화.
 *   Awake 대신 OnEnable 사용 — GO 비활성화 시 자동 입력 차단.
 * - FindActionMap(throwIfNotFound:false): 에셋 미연결 방어, 에러 없이 조기 종료.
 * - 콜백 등록 후 map.Enable(): initialStateCheck 이벤트 손실 방지.
 * - 이벤트 발화만 담당: Manager 직접 호출 금지(CLAUDE.md §14 원칙).
 *
 * =============================================================================
 */
#endif

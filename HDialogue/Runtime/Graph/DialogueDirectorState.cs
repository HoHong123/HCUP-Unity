#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- DialogueDirector 상태 전이 열거형.
 *
 * 특징 / 지원기능 ::
 * + Idle              - 초기 / 정지 상태
 * + PlayingLine       - TextController.PlayLine 실행 중
 * + WaitingForLineEnd - 라인 타이핑 완료 대기 (OnLineComplete 수신 전)
 * + WaitingForChoice  - 선택지 입력 대기 (SelectChoice 호출 전)
 * + Waiting           - WaitNode 처리 중 (Time / Condition / UserInput)
 * + Finished          - ExitNode 도달 또는 Stop() 호출로 종료
 *
 * 주의사항 ::
 * SelectChoice()는 WaitingForChoice 상태에서만 유효.
 * Stop()은 어떤 상태에서도 Idle로 강제 전이.
 * =========================================================
 */
#endif

namespace HDialogue {
    public enum DialogueDirectorState {
        Idle,
        PlayingLine,
        WaitingForLineEnd,
        WaitingForChoice,
        Waiting,
        Finished,
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.15 DialogueDirectorState 베이스 코드 생성
 *
 * # 목적
 * - DialogueDirector 내부 상태 머신 표현
 * - HCUP-2.3.0 Phase 0
 *
 * # 사용 흐름
 * - DialogueDirector.State 프로퍼티로 외부 조회
 * - 외부 시스템이 State를 폴링해 UI 갱신 가능 (선택지 UI, 스킵 버튼 등)
 *
 * =============================================================================
 */
#endif

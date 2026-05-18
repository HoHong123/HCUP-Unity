#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- DialogueWaitNode 대기 방식 열거형.
 *
 * 특징 / 지원기능 ::
 * + Time      - UniTask.Delay(seconds) 후 자동 진행
 * + Condition - NotifyWaitConditionMet() 외부 신호 대기
 * + UserInput - OnAdvanceRequested 1회 수신 후 진행
 * =========================================================
 */
#endif

namespace HDialogue {
    public enum WaitMode {
        Time,
        Condition,
        UserInput,
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.15 WaitMode 베이스 코드 생성
 *
 * # 목적
 * - DialogueWaitNode 대기 방식 열거 — HCUP-2.3.0 Phase 1
 * - DialogueDirector._ProcessWaitNode에서 mode 별 처리
 *
 * =============================================================================
 */
#endif

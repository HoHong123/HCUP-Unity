#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 대화 텍스트 출력기의 상태 머신 상태 값.
 *
 * 특징 / 지원기능 ::
 * + Idle     - 라인 미할당, 대기
 * + Typing   - 한 글자씩 출력 중
 * + Paused   - <pause> 태그 또는 외부 Pause() 호출로 일시정지
 * + Waiting  - 출력 완료, 다음 입력 대기
 * + Skipped  - SkipToEnd()로 강제 완료된 상태
 *
 * 주의사항 ::
 * Skipped ≠ Waiting — Auto 모드 자동 진행 정책이 다르다.
 * Waiting : 자연 완료 → N초 후 자동 진행 가능.
 * Skipped : 사용자 스킵 → 한 번 더 입력이 있어야 진행.
 * =========================================================
 */
#endif

namespace HDialogue {
    public enum TextDisplayState {
        Idle,
        Typing,
        Paused,
        Waiting,
        Skipped,
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.15 HUI.TextUI → HDialogue 패키지 이관
 *
 * # 변경
 * - namespace HUI.TextUI → HDialogue
 * - HUI/Runtime/HUI/Text/Dialogue/ → HDialogue/Runtime/ 이관
 *
 * =============================================================================
 * @Jason - PKH 2026.05.14 TextDisplayState 베이스 코드 생성
 *
 * # 목적
 * - DialogueTextController 상태 머신의 5개 상태 정의
 * - HCUP-2.2.0 Custom TMP_Text Phase 0
 *
 * # 사용 흐름
 * - Idle → PlayLine() → Typing
 * - Typing ↔ Paused (pause 태그 / Pause() / Resume())
 * - Typing | Paused → 자연 완료 → Waiting
 * - Typing | Paused → SkipToEnd() → Skipped
 * - Waiting | Skipped → Clear() or 다음 PlayLine() → Idle
 *
 * =============================================================================
 */
#endif

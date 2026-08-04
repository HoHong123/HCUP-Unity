#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * InitManager 상태머신의 페이즈 열거형입니다.
 * =========================================================
 */
#endif

public enum InitPhaseType {
    None,
    Prepare,
    Start,
    Running,
    Pause,
    Resume,
    Over,
    Exit
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.08.04 GamePhaseType 을 InitPhaseType 으로 개칭
 *
 * # 변경
 * - 열거형명 GamePhaseType -> InitPhaseType, 파일명 동기화 (guid 보존, 직렬화는 int 값이라 무영향).
 *
 * # 이유
 * - InitModule 폴더 내 스크립트의 Game 접두를 Init 으로 통일 (Phase 1 HCUP 정리 후속).
 *
 * =============================================================================
 */
#endif

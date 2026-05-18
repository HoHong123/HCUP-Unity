#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- DialogueBranchNode 분기 평가 모드 열거형.
 *
 * 특징 / 지원기능 ::
 * + Boolean  - TryGetBool → "true" / "false" 허브 포트 키 매칭
 * + IntRange - TryGetInt → "min_max" 구간 허브 포트 키 매칭
 * + Switch   - TryGetString → 포트 키 직접 비교
 * =========================================================
 */
#endif

namespace HDialogue {
    public enum BranchMode {
        Boolean,
        IntRange,
        Switch,
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.15 BranchMode 베이스 코드 생성
 *
 * # 목적
 * - DialogueBranchNode 변수 평가 방식 열거 — HCUP-2.3.0 Phase 1
 * - DialogueDirector._ProcessBranchNode 에서 mode 별 분기
 *
 * =============================================================================
 */
#endif

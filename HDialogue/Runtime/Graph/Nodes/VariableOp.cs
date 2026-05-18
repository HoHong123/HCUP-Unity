#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- DialogueVariableNode 변수 조작 연산자 열거형.
 *
 * 특징 / 지원기능 ::
 * + SetBool / SetInt / SetString - 값 직접 설정
 * + AddInt                       - 정수 누산 (음수 허용)
 * + ToggleBool                   - bool 반전
 * =========================================================
 */
#endif

namespace HDialogue {
    public enum VariableOp {
        SetBool,
        SetInt,
        SetString,
        AddInt,
        ToggleBool,
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.15 VariableOp 베이스 코드 생성
 *
 * # 목적
 * - DialogueVariableNode가 수행할 IDialogueVariableContext 연산 열거 — Phase 1
 * - DialogueDirector._ProcessVariableNode에서 op 별 variables.Set/Add/Toggle 호출
 *
 * =============================================================================
 */
#endif

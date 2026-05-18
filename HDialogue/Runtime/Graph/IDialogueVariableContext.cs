#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 대화 변수 저장소 DI 인터페이스.
 *
 * 특징 / 지원기능 ::
 * + TryGetBool / TryGetInt / TryGetString - 값 조회 (없으면 false 반환)
 * + SetBool / SetInt / SetString           - 값 설정
 * + AddInt                                 - 정수 누산
 * + ToggleBool                             - bool 반전
 *
 * 주의사항 ::
 * 저장 스코프(씬 / 세션 / 영구)는 구현체 책임. 패키지는 인터페이스만 정의.
 * DialogueDirector에 null 주입 시 BranchNode/VariableNode는 워닝 + 기본값 처리.
 * =========================================================
 */
#endif

namespace HDialogue {
    public interface IDialogueVariableContext {
        bool TryGetBool(string key, out bool value);
        bool TryGetInt(string key, out int value);
        bool TryGetString(string key, out string value);

        void SetBool(string key, bool value);
        void SetInt(string key, int value);
        void SetString(string key, string value);

        void AddInt(string key, int delta);
        void ToggleBool(string key);
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.15 IDialogueVariableContext 베이스 코드 생성
 *
 * # 목적
 * - 변수 저장/스코프 정책을 외부에서 주입 (DI) — 패키지는 인터페이스만 제공
 * - HCUP-2.3.0 Phase 0
 *
 * # 사용 흐름
 * - DialogueDirector.Bind(textController, variables) 로 주입
 * - DialogueBranchNode / DialogueVariableNode 처리 시 인터페이스 경유
 * - null 허용 — null이면 해당 노드에서 워닝 로그 + 기본값(false/0/"") 처리
 *
 * =============================================================================
 */
#endif

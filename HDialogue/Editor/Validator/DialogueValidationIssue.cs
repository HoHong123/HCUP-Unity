#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 대화 카탈로그 검증 단일 이슈 항목 구조체.
 *
 * 특징 / 지원기능 ::
 * + NodeUID  - 이슈가 발생한 노드 식별자 (None = 카탈로그 수준 이슈)
 * + Code     - 규칙 코드 (E001~E006 오류, W001~W004 경고)
 * + Message  - 사람이 읽을 수 있는 설명 문자열
 *
 * 주의사항 ::
 * DialogueCatalogValidator.Validate() 반환값 DialogueValidationReport의 구성 요소.
 * 불변 구조체 — 생성자로만 값 설정.
 * =========================================================
 */
#endif

using HWindows.NodeWindow.Identity;

namespace HDialogue.Editor {
    public readonly struct DialogueValidationIssue {
        public readonly NodeUID NodeUID;
        public readonly string Code;
        public readonly string Message;

        public DialogueValidationIssue(NodeUID nodeUID, string code, string message) {
            NodeUID = nodeUID;
            Code = code;
            Message = message;
        }
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.15 DialogueValidationIssue 베이스 코드 생성
 *
 * # 목적
 * - DialogueCatalogValidator Phase 3 — 이슈 항목 계약 타입
 *
 * # 설계 결정
 * - readonly struct : 불변성 보장, GC 부담 없음
 * - NodeUID.None = 카탈로그 수준 이슈 (노드 특정 불가)
 *
 * =============================================================================
 */
#endif

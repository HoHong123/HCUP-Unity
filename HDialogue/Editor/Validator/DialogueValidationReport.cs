#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 대화 카탈로그 검증 결과 집계 구조체.
 *
 * 특징 / 지원기능 ::
 * + Errors   - 강제 규칙 위반 이슈 목록 (IsValid = false 조건)
 * + Warnings - 경고 규칙 이슈 목록 (실행은 되지만 주의 필요)
 * + IsValid  - Errors.Count == 0 이면 true
 *
 * 주의사항 ::
 * DialogueCatalogValidator.Validate() 반환 타입.
 * Errors / Warnings 는 절대 null 아님 — Validate() 가 항상 초기화 보장.
 * =========================================================
 */
#endif

using System.Collections.Generic;

namespace HDialogue.Editor {
    public struct DialogueValidationReport {
        public List<DialogueValidationIssue> Errors;
        public List<DialogueValidationIssue> Warnings;

        public readonly bool IsValid => Errors == null || Errors.Count == 0;
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.15 DialogueValidationReport 베이스 코드 생성
 *
 * # 목적
 * - DialogueCatalogValidator Phase 3 — 검증 결과 컨테이너
 *
 * # 설계 결정
 * - mutable struct (List 참조 포함) — 필드 직접 할당 패턴으로 생성
 * - IsValid: Errors null-safe 처리 (방어적 readonly)
 *
 * =============================================================================
 */
#endif

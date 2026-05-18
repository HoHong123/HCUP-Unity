#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 대화 카탈로그 분류 태그 열거형.
 *
 * 특징 / 지원기능 ::
 * + Normal        - 일반 대화 (플레이어 입력 대기)
 * + Cutscene      - 컷씬 (autoAdvanceDelay 후 자동 진행)
 * + Tutorial      - 튜토리얼 대화
 * + SystemMessage - 시스템 메시지 (알림류)
 *
 * 주의사항 ::
 * Cutscene 태그이면 DialogueDirector가 라인 완료 후 자동 진행.
 * =========================================================
 */
#endif

namespace HDialogue {
    public enum DialogueCatalogTag {
        Normal,
        Cutscene,
        Tutorial,
        SystemMessage,
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.15 DialogueCatalogTag 베이스 코드 생성
 *
 * # 목적
 * - DialogueCatalogSO 분류 태그 — DialogueDirector 동작 분기에 사용
 * - HCUP-2.3.0 Phase 0
 *
 * # 사용 흐름
 * - DialogueCatalogSO.CatalogTag 로 참조
 * - DialogueDirector._ProcessLineNode 에서 Cutscene 여부 판별 → 자동 진행
 *
 * =============================================================================
 */
#endif

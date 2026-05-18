#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- DialogueDirector → CharacterStageDirector 1라인 단위 무대 컨텍스트.
 *
 * 특징 / 지원기능 ::
 * + SpeakerKey          - 화자 캐릭터 식별자
 * + SpeakerPoseKey      - 이 라인에서 적용할 포즈 키 (빈 문자열 = 유지)
 * + SpeakerSlot         - 배치 슬롯 위치 (null = defaultSlot / 현재 슬롯 유지)
 * + SpeakerFacing       - 캐릭터 방향
 * + AutoHighlightSpeaker - true이면 화자 이외 슬롯 비활성 하이라이트
 *
 * 주의사항 ::
 * DialogueDirector._BuildLineStageContext(DialogueLineNode)에서 생성.
 * CharacterStageDirector.EnterLine(ctx) 에서 소비.
 * =========================================================
 */
#endif

namespace HDialogue {
    public struct LineStageContext {
        public string SpeakerKey;
        public string SpeakerPoseKey;
        public StageSlot? SpeakerSlot;
        public FacingDirection SpeakerFacing;
        public bool AutoHighlightSpeaker;
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: SpeakerSlot 타입 FacingDirection? → StageSlot?
 *
 * # 변경
 * - `FacingDirection? SpeakerSlot` → `StageSlot? SpeakerSlot`.
 * - SpeakerFacing(FacingDirection)은 방향 개념이므로 유지.
 *
 * # 이유
 * - AgentReview Warning #7. 슬롯 위치와 방향 개념을 분리.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: SpeakerSlotKey(string) → SpeakerSlot(FacingDirection?)
 *
 * # 변경
 * - `string SpeakerSlotKey` → `FacingDirection? SpeakerSlot`.
 *   null = 슬롯 미지정 (현재 슬롯 유지 또는 DefaultSlot 사용).
 *
 * =============================================================================
 * @Jason - PKH 2026.05.15 LineStageContext 베이스 코드 생성
 *
 * # 목적
 * - DialogueDirector → CharacterStageDirector 라인별 무대 지시 계약
 * - HCUP-2.3.0 Phase 2 (Director) 선행 생성 — Phase 4에서 CharacterStageDirector 완성
 *
 * # 사용 흐름
 * - DialogueDirector._ProcessLineNode → _BuildLineStageContext(node) → stageDirector?.EnterLine(ctx)
 *
 * =============================================================================
 */
#endif

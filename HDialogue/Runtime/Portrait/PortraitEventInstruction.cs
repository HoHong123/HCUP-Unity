#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- PortraitEventParser 파싱 결과 구조체.
 *
 * 특징 / 지원기능 ::
 * + Verb               - 동작 종류 (PortraitVerb)
 * + TargetCharacterKey - 대상 캐릭터 키 (없으면 string.Empty)
 * + Args               - 추가 인자 배열 (포즈키, 슬롯키, left/right 등)
 *
 * 주의사항 ::
 * PortraitEventParser.TryParse() 반환 out 파라미터.
 * CharacterStageDirector._Apply() 에서 소비.
 * =========================================================
 */
#endif

namespace HDialogue {
    public readonly struct PortraitEventInstruction {
        public readonly PortraitVerb Verb;
        public readonly string TargetCharacterKey;
        public readonly string[] Args;

        public PortraitEventInstruction(PortraitVerb verb, string targetCharacterKey, string[] args) {
            Verb = verb;
            TargetCharacterKey = targetCharacterKey;
            Args = args;
        }
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.15 PortraitEventInstruction 베이스 코드 생성
 *
 * # 목적
 * - HCUP-2.3.0 Phase 4-E — portrait.* 인라인 이벤트 파싱 결과 계약
 *
 * =============================================================================
 */
#endif

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- DialogueTagParser가 생성하는 토큰의 종류.
 *
 * 특징 / 지원기능 ::
 * TMP 가시 텍스트에 기여하는 것 : Char, PassThrough
 * 부수 효과만 일으키는 것      : 나머지 모두
 * + Char        - 일반 문자 1개 (maxVisibleCharacters 1 증가)
 * + Pause       - NumericArg 초만큼 출력 중단
 * + SpeedSet    - NumericArg 배수로 출력 속도 변경
 * + SpeedReset  - 속도 배수를 1.0으로 원복
 * + EffectPush  - shake/wave/rainbow 효과 범위 시작
 * + EffectPop   - 효과 범위 종료
 * + Sfx         - StringArg 키로 효과음 1회 재생 (글자 없음)
 * + Event       - StringArg 키로 외부 이벤트 발행 (글자 없음)
 * + SilentPush  - 이 지점부터 블립 사운드 억제
 * + SilentPop   - 블립 억제 해제
 * + VoiceSet    - StringArg 클립 키로 블립 클립 교체
 * + PassThrough - TMP 표준 태그 문자열 (<color>, <b> 등)
 *
 * 주의사항 ::
 * EffectPush.StringArg = "shake" | "wave" | "rainbow"
 * =========================================================
 */
#endif

namespace HDialogue {
    public enum DialogueTokenType {
        Char,
        Pause,
        SpeedSet,
        SpeedReset,
        EffectPush,
        EffectPop,
        Sfx,
        Event,
        SilentPush,
        SilentPop,
        VoiceSet,
        PassThrough,
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
 * @Jason - PKH 2026.05.14 DialogueTokenType 베이스 코드 생성
 *
 * # 목적
 * - DialogueToken.Type 필드의 값 집합
 * - DialogueTagParser 파싱 결과를 DialogueTextController가 switch 분기
 * - HCUP-2.2.0 Custom TMP_Text Phase 0
 *
 * =============================================================================
 */
#endif

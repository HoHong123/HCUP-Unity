#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- DialogueTagParser 파싱 결과의 단위 토큰.
 *
 * 특징 / 지원기능 ::
 * 필드 유효 범위 (Type에 따라 사용하는 필드가 다름) ::
 * + Char       : Character 유효
 * + Pause      : NumericArg (초)
 * + SpeedSet   : NumericArg (배수)
 * + EffectPush : StringArg ("shake" | "wave" | "rainbow")
 * + Sfx        : StringArg (clip 키)
 * + Event      : StringArg (이벤트 키)
 * + VoiceSet   : StringArg (clip 키)
 * + PassThrough: StringArg (TMP 표준 태그 원본 문자열)
 * 나머지 Type은 추가 필드 불필요
 *
 * 주의사항 ::
 * GC 0 유지를 위해 struct로 선언. heap 할당 없이 List<DialogueToken>에 값 복사.
 * =========================================================
 */
#endif

namespace HDialogue {
    public struct DialogueToken {
        #region Fields
        public DialogueTokenType Type;
        public char Character; // Char 타입에만 유효
        public float NumericArg; // Pause, SpeedSet 등에 사용
        public string StringArg; // Sfx, Event, Voice, EffectPush, PassThrough 등
        #endregion

        #region Static Factory
        public static DialogueToken Char(char c) =>
            new DialogueToken { Type = DialogueTokenType.Char, Character = c };

        public static DialogueToken Pause(float seconds) =>
            new DialogueToken { Type = DialogueTokenType.Pause, NumericArg = seconds };

        public static DialogueToken SpeedSet(float multiplier) =>
            new DialogueToken { Type = DialogueTokenType.SpeedSet, NumericArg = multiplier };

        public static DialogueToken SpeedReset() =>
            new DialogueToken { Type = DialogueTokenType.SpeedReset };

        public static DialogueToken EffectPush(string effectName) =>
            new DialogueToken { Type = DialogueTokenType.EffectPush, StringArg = effectName };

        public static DialogueToken EffectPop() =>
            new DialogueToken { Type = DialogueTokenType.EffectPop };

        public static DialogueToken Sfx(string clipKey) =>
            new DialogueToken { Type = DialogueTokenType.Sfx, StringArg = clipKey };

        public static DialogueToken Event(string eventKey) =>
            new DialogueToken { Type = DialogueTokenType.Event, StringArg = eventKey };

        public static DialogueToken SilentPush() =>
            new DialogueToken { Type = DialogueTokenType.SilentPush };

        public static DialogueToken SilentPop() =>
            new DialogueToken { Type = DialogueTokenType.SilentPop };

        public static DialogueToken VoiceSet(string clipKey) =>
            new DialogueToken { Type = DialogueTokenType.VoiceSet, StringArg = clipKey };

        public static DialogueToken PassThrough(string rawTag) =>
            new DialogueToken { Type = DialogueTokenType.PassThrough, StringArg = rawTag };
        #endregion
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
 * @Jason - PKH 2026.05.14 DialogueToken 베이스 코드 생성
 *
 * # 목적
 * - DialogueTagParser 파싱 결과를 담는 값 타입 단위
 * - HCUP-2.2.0 Custom TMP_Text Phase 0
 *
 * # 설계 결정
 * - struct 선택 이유: PlayLine 1회당 수십~수백 토큰 생성. heap 없이 List에 값 복사.
 * - Static Factory: 타입별 생성 진입점 일원화. 호출자가 필드 이름 기억할 필요 없음.
 * - 미사용 필드 기본값(0/null)은 명시적으로 채우지 않음 — struct 기본값으로 보장.
 *
 * =============================================================================
 */
#endif

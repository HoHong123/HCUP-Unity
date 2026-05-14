#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- HCUP-2.2.0 텍스트 효과 적용 범위 (Phase 3).
 *
 * 특징 / 지원기능 ::
 * + StartCharIndex / EndCharIndex : TMP characterInfo 기준 반개구간 [start, end)
 * + EffectName : "shake" / "wave" / "rainbow"
 *
 * 주의사항 ::
 * CharIndex는 Char 토큰만 카운트한 TMP visible character 인덱스.
 * PassThrough(TMP 태그) 토큰은 인덱스에 영향 없음.
 * =========================================================
 */
#endif

namespace HDialogue {
    public readonly struct TextEffectRange {
        public readonly int    StartCharIndex;
        public readonly int    EndCharIndex;
        public readonly string EffectName;

        public TextEffectRange(int start, int end, string effectName) {
            StartCharIndex = start;
            EndCharIndex   = end;
            EffectName     = effectName;
        }
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
 * @Jason - PKH 2026.05.15 TextEffectRange Phase 3 생성
 *
 * # 목적
 * - DialogueTextController._BuildEffectRanges() 반환값 타입.
 * - TextEffectHandler.SetEffectRanges() 파라미터로 전달.
 *
 * # 설계 결정
 * - readonly struct : 불변 값 타입. List<TextEffectRange> 복사 비용 무시 가능 (필드 3개).
 * - EndCharIndex 반개구간(exclusive) : for (i = start; i < end) 패턴과 정합.
 * - CharIndex 기준 : Char 토큰만 카운트. PassThrough는 TMP 마크업 태그이므로 카운트 제외.
 *
 * =============================================================================
 */
#endif

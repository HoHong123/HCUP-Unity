#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 대화 텍스트 출력 속도 관련 상수 집합.
 *
 * 특징 / 지원기능 ::
 * 기본 글자 간격 (TextSpeedMode별) ::
 * + BASE_INTERVAL_SLOW   - 0.08s
 * + BASE_INTERVAL_NORMAL - 0.04s
 * + BASE_INTERVAL_FAST   - 0.015s
 * + BASE_INTERVAL_INSTANT- 0s
 *
 * 문장 부호 추가 지연 ::
 * + PUNCT_DELAY_SENTENCE - ". ! ?" 뒤 +0.25s
 * + PUNCT_DELAY_COMMA    - "," 뒤  +0.12s
 * + PUNCT_DELAY_NEWLINE  - "\n" 뒤 +0.15s
 *
 * 기타 ::
 * + HOLD_SPEED_MULTIPLIER  - 키 누르고 있을 때 추가 0.5배 (2배 빠름)
 * + INPUT_GUARD_DURATION   - PlayLine 후 입력 무시 시간 0.05s (50ms)
 *
 * 주의사항 ::
 * Instant 모드에서는 모든 지연을 0으로 처리 (컨트롤러에서 분기).
 * HOLD_SPEED_MULTIPLIER는 Instant 전환이 아니라 빠른 진행 — 효과·사운드 보존.
 * =========================================================
 */
#endif

namespace HDialogue {
    public static class TextSpeedConstants {
        #region Base Interval (초 / 글자)
        public const float BASE_INTERVAL_SLOW = 0.08f;
        public const float BASE_INTERVAL_NORMAL = 0.04f;
        public const float BASE_INTERVAL_FAST = 0.015f;
        public const float BASE_INTERVAL_INSTANT = 0f;
        #endregion

        #region Punctuation Delay (초)
        public const float PUNCT_DELAY_SENTENCE = 0.25f;   // . ! ?
        public const float PUNCT_DELAY_COMMA = 0.12f;   // ,
        public const float PUNCT_DELAY_NEWLINE = 0.15f;   // \n
        #endregion

        #region Control
        public const float HOLD_SPEED_MULTIPLIER = 0.5f;   // 키 홀드 시 × 0.5 = 2배 빠름
        public const float INPUT_GUARD_DURATION = 0.05f;  // PlayLine 직후 50ms 입력 무시
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
 * @Jason - PKH 2026.05.14 TextSpeedConstants 베이스 코드 생성
 *
 * # 목적
 * - 매직 넘버 제거: 출력 속도·지연 수치를 단일 위치에 집약
 * - HCUP-2.2.0 Custom TMP_Text Phase 0
 *
 * # 설계 결정
 * - static class + const: 인스턴스 없이 참조. 값 변경은 이 파일 단일 위치에서만.
 * - HOLD_SPEED_MULTIPLIER = 0.5: 2배 빠름. Instant 전환이 아니므로 효과·사운드 보존됨.
 * - INPUT_GUARD_DURATION = 0.05: 50ms. 라인 종료 시점의 키 입력 누설 방지.
 *
 * =============================================================================
 */
#endif

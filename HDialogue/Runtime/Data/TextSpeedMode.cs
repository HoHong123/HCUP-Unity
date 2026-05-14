#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 대화 텍스트 출력 속도 모드.
 *
 * 특징 / 지원기능 ::
 * 실제 초당 간격은 TextSpeedConstants에 정의.
 * + Slow    - 0.08s / 글자
 * + Normal  - 0.04s / 글자
 * + Fast    - 0.015s / 글자
 * + Instant - 0s (한 프레임에 전부 출력)
 *
 * 주의사항 ::
 * Instant 모드는 모든 지연(pause, 문장부호 추가 지연)을 0으로 처리.
 * sfx/event 토큰도 Instant 모드에서는 발화하지 않는다.
 * =========================================================
 */
#endif

namespace HDialogue {
    public enum TextSpeedMode {
        Slow,
        Normal,
        Fast,
        Instant,
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
 * @Jason - PKH 2026.05.14 TextSpeedMode 베이스 코드 생성
 *
 * # 목적
 * - DialogueTextController.SetSpeedMode() 파라미터 타입
 * - HCUP-2.2.0 Custom TMP_Text Phase 0
 *
 * # 사용 흐름
 * - controller.SetSpeedMode(TextSpeedMode.Fast)
 * - 실제 간격은 TextSpeedConstants.BASE_INTERVAL_* 상수 참조
 *
 * =============================================================================
 */
#endif

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 포트레이트 트랜지션 방식 열거형.
 *
 * 특징 / 지원기능 ::
 * + Instant   - 즉시 적용 (0프레임)
 * + Fade      - 알파 페이드 인/아웃
 * + SlideIn   - 슬롯 외부에서 슬라이드 진입
 * + Crossfade - 페이드 아웃 + 인 중첩
 * + Scale     - 스케일 팝 (확대 후 수렴)
 *
 * 주의사항 ::
 * CharacterPortraitController에서 PortraitTransition.Type으로 분기.
 * =========================================================
 */
#endif

namespace HDialogue {
    public enum PortraitTransitionType {
        Instant,
        Fade,
        SlideIn,
        Crossfade,
        Scale
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: 열거형 값 줄바꿈 정렬
 *
 * =============================================================================
 * @Jason - PKH 2026.05.15 PortraitTransitionType 베이스 코드 생성
 *
 * # 목적
 * - HCUP-2.3.0 Phase 4-B — PortraitTransition 타입 정의
 *
 * =============================================================================
 */
#endif

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 포트레이트 트랜지션 설정 구조체.
 *
 * 특징 / 지원기능 ::
 * + Type     - PortraitTransitionType (Instant / Fade / SlideIn / Crossfade / Scale)
 * + Duration - 트랜지션 시간(초)
 * + Curve    - 이징 커브 (null이면 Linear)
 * + 정적 팩토리: Instant / Fade(d) / SlideIn(d) / Crossfade(d) / Scale(d)
 *
 * 주의사항 ::
 * CharacterPortraitController API 파라미터로 전달. 값 타입이므로 복사 전달.
 * =========================================================
 */
#endif

using HInspector;
using UnityEngine;

namespace HDialogue {
    [System.Serializable]
    public struct PortraitTransition {
        [HTitle("Transition")]
        public PortraitTransitionType Type;
        public float Duration;
        public AnimationCurve Curve;

        public static PortraitTransition Instant => new() { Type = PortraitTransitionType.Instant };

        public static PortraitTransition Fade(float duration) => new() {
            Type = PortraitTransitionType.Fade,
            Duration = duration
        };

        public static PortraitTransition SlideIn(float duration) => new() {
            Type = PortraitTransitionType.SlideIn,
            Duration = duration
        };

        public static PortraitTransition Crossfade(float duration) => new() {
            Type = PortraitTransitionType.Crossfade,
            Duration = duration
        };

        public static PortraitTransition Scale(float duration) => new() {
            Type = PortraitTransitionType.Scale,
            Duration = duration
        };

        public float EvaluateCurve(float t) =>
            Curve != null && Curve.length > 0 ? Curve.Evaluate(t) : t;
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: HInspector HTitle "Transition" 그룹 추가
 *
 * =============================================================================
 * @Jason - PKH 2026.05.15 PortraitTransition 베이스 코드 생성
 *
 * # 목적
 * - HCUP-2.3.0 Phase 4-B — 트랜지션 파라미터 계약 + 편의 팩토리
 *
 * # 설계 결정
 * - EvaluateCurve: null/empty 커브이면 linear(t) 반환 — 호출부 null 처리 불필요
 * - 정적 프로퍼티/메서드 팩토리: 호출부 가독성 개선 (PortraitTransition.Fade(0.3f))
 *
 * =============================================================================
 */
#endif

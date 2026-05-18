#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 포트레이트 포즈 렌더링 방식 열거형.
 *
 * 특징 / 지원기능 ::
 * + Static   - Image.sprite 직접 교체 (Animator 비활성)
 * + Animated - Animator.Play 경유 애니메이션 재생
 * + Sequence - 복수 스프라이트 프레임 시퀀스 (Phase 5 확장 예정)
 *
 * 주의사항 ::
 * CharacterPortraitController._ApplyPose() 에서 분기 조건으로 사용.
 * =========================================================
 */
#endif

namespace HDialogue {
    public enum PortraitPoseType {
        Static,
        Animated,
        Sequence
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: 열거형 값 줄바꿈 정렬
 *
 * =============================================================================
 * @Jason - PKH 2026.05.15 PortraitPoseType 베이스 코드 생성
 *
 * # 목적
 * - HCUP-2.3.0 Phase 4-A — CharacterPortraitSetSO.poses 항목 렌더링 방식 구분
 *
 * =============================================================================
 */
#endif

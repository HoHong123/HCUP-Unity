#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 스테이지 슬롯 위치 식별자 열거형.
 *
 * 특징 / 지원기능 ::
 * + Left  - 화면 기준 왼쪽 슬롯
 * + Right - 화면 기준 오른쪽 슬롯
 *
 * 주의사항 ::
 * 슬롯 "위치" 전용. 캐릭터 방향(Left facing / Right facing)은 FacingDirection 사용.
 * =========================================================
 */
#endif

namespace HDialogue {
    public enum StageSlot {
        Left,
        Right,
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.17 최초 생성 — FacingDirection에서 슬롯 위치 개념 분리
 *
 * # 목적
 * - AgentReview Warning #7. FacingDirection이 "방향"과 "슬롯 위치" 두 개념 혼용.
 * - StageSlot: 화면 상 슬롯 위치. FacingDirection: 캐릭터가 향하는 방향.
 * - 정수값(Left=0, Right=1)이 FacingDirection과 동일 → .asset 마이그레이션 불필요.
 *
 * =============================================================================
 */
#endif

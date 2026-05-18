#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 스테이지 슬롯 배치 설정 구조체.
 *
 * 특징 / 지원기능 ::
 * + Slot         - 슬롯 식별자 (StageSlot.Left / Right)
 * + AnchorPos    - RectTransform.anchoredPosition 목표값
 * + AnchorMin / AnchorMax - RectTransform 앵커 범위
 * + SortingOrder - Canvas.sortingOrder (앞/뒤 배치)
 * + Scale        - 슬롯 기본 스케일 (원근감 연출용)
 *
 * 주의사항 ::
 * StageLayoutSO.slots 원소. CharacterPortraitController.SetSlot() 에서 소비.
 * =========================================================
 */
#endif

using System;
using HInspector;
using UnityEngine;

namespace HDialogue {
    [Serializable]
    public struct SlotConfig {
        [HTitle("Identity")]
        public StageSlot Slot;

        [HTitle("Position")]
        public Vector2 AnchorPos;
        public Vector2 AnchorMin;
        public Vector2 AnchorMax;

        [HTitle("Display")]
        public int SortingOrder;
        public float Scale;
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: Slot 타입 FacingDirection → StageSlot 교체
 *
 * # 변경
 * - `FacingDirection Slot` → `StageSlot Slot`.
 *
 * # 이유
 * - AgentReview Warning #7. 슬롯 "위치"와 캐릭터 "방향"을 동일 타입으로 표현하던 혼용 제거.
 * - 정수값 동일(Left=0, Right=1) → 기존 .asset 마이그레이션 불필요.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: Key(string) → Slot(FacingDirection) 타입 변경
 *
 * # 변경
 * - `string Key` → `FacingDirection Slot`.
 *
 * # 이유
 * - 슬롯이 Left/Right 두 가지 뿐이므로 string 비교 불필요. 열거형으로 컴파일 타임 타입 안전 보장.
 * - CharacterStageDirector._GetSlotRoot 의 indexOf("right") 문자열 패턴 제거 가능.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: HInspector HTitle 그룹 추가 (Identity / Position / Display)
 *
 * =============================================================================
 * @Jason - PKH 2026.05.15 SlotConfig 베이스 코드 생성
 *
 * # 목적
 * - HCUP-2.3.0 Phase 4-A — StageLayoutSO 슬롯 배치 계약
 *
 * =============================================================================
 */
#endif

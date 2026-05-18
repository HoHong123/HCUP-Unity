#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 스테이지 슬롯 배치 + 하이라이트 스타일 ScriptableObject.
 *
 * 특징 / 지원기능 ::
 * + slots List<SlotConfig>           - 슬롯 설정 목록 (Left / Right)
 * + highlightStyle PortraitHighlightStyle - 화자/비화자 틴트·스케일 설정
 * + TryGet(StageSlot, out config) - 슬롯 조회
 *
 * 주의사항 ::
 * CharacterStageDirector.Bind() 에 전달. 슬롯 키는 StageSlot 열거형으로 지정.
 * =========================================================
 */
#endif

using System.Collections.Generic;
using HInspector;
using UnityEngine;

namespace HDialogue {
    [CreateAssetMenu(menuName = "HWindows/Dialogue/Stage Layout")]
    public sealed class StageLayoutSO : ScriptableObject {
        #region Fields
        [HTitle("Slots")]
        [SerializeField]
        List<SlotConfig> slots = new();

        [HTitle("Highlight")]
        [SerializeField]
        PortraitHighlightStyle highlightStyle = PortraitHighlightStyle.Default;
        #endregion

        public IReadOnlyList<SlotConfig> Slots => slots;
        public PortraitHighlightStyle HighlightStyle => highlightStyle;

        public bool TryGet(StageSlot slot, out SlotConfig config) {
            foreach (SlotConfig s in slots) {
                if (s.Slot == slot) {
                    config = s;
                    return true;
                }
            }
            config = default;
            return false;
        }

        public StageSlot DefaultSlot => slots.Count > 0 ? slots[0].Slot : StageSlot.Left;
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: TryGet / DefaultSlot 타입 FacingDirection → StageSlot
 *
 * # 변경
 * - `TryGet(FacingDirection, ...)` → `TryGet(StageSlot, ...)`.
 * - `DefaultSlot: FacingDirection` → `DefaultSlot: StageSlot`.
 *   `FacingDirection.Left` fallback → `StageSlot.Left`.
 *
 * # 이유
 * - AgentReview Warning #7. SlotConfig.Slot 타입 변경에 따른 연쇄 수정.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.17 (수정) :: TryGet(string) → TryGet(FacingDirection), DefaultSlotKey → DefaultSlot
 *
 * # 변경
 * - `TryGet(string key, ...)` → `TryGet(FacingDirection slot, ...)` — SlotConfig.Slot 비교.
 * - `DefaultSlotKey (string)` → `DefaultSlot (FacingDirection)`.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.15 StageLayoutSO 베이스 코드 생성
 *
 * # 목적
 * - HCUP-2.3.0 Phase 4-A — 씬별 스테이지 슬롯 배치 에셋 타입
 *
 * # 설계 결정
 * - DefaultSlotKey: 슬롯 키 미지정 시 첫 번째 슬롯 사용 (CharacterStageDirector fallback)
 *
 * =============================================================================
 */
#endif

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- DialogueWaitNode 전용 GraphView 시각 노드.
 *
 * 특징 / 지원기능 ::
 * + hdialogue-wait-node CSS (주황 테두리)
 * + 바디: WaitMode 표시 + Time→초수, Condition→conditionKey, UserInput→없음
 *
 * 주의사항 ::
 * UserInput 모드는 seconds/conditionKey 모두 무의미 — 모드명만 표시.
 * =========================================================
 */
#endif

using UnityEngine.UIElements;
using HWindows.Editor.NodeWindow;

namespace HDialogue.Editor {
    public sealed class HGraphDialogueWaitNode : HGraphNode {
        public HGraphDialogueWaitNode(DialogueWaitNode data, bool isRoot = false) : base(data, isRoot) {
            AddToClassList("hdialogue-wait-node");
            _AddDialogueStyleSheet();
            bodyArea.Clear();

            var modeLabel = new Label(data.Mode.ToString());
            modeLabel.AddToClassList("hdialogue-speaker-label");
            bodyArea.Add(modeLabel);

            string detail = data.Mode switch {
                WaitMode.Time      => $"{data.Seconds:F1}s",
                WaitMode.Condition => string.IsNullOrEmpty(data.ConditionKey) ? "(no key)" : data.ConditionKey,
                _                  => string.Empty
            };

            if (!string.IsNullOrEmpty(detail)) {
                var detailLabel = new Label(detail);
                detailLabel.AddToClassList("hdialogue-meta-label");
                bodyArea.Add(detailLabel);
            }
        }

        void _AddDialogueStyleSheet() {
            StyleSheet sheet = DialogueStyleSheetLoader.Get();
            if (sheet != null) styleSheets.Add(sheet);
        }
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.15 HGraphDialogueWaitNode 베이스 코드 생성
 *
 * # 목적
 * - HCUP-2.3.0 Phase 5 — DialogueWaitNode 전용 시각 노드
 *
 * # 설계 결정
 * - UserInput 모드: detail 없음 → detailLabel 생략. 빈 공간 낭비 방지.
 * - seconds F1 포맷: "1.0s" 형식으로 단위 명시
 *
 * =============================================================================
 */
#endif

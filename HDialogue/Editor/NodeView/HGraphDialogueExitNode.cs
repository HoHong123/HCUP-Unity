#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- DialogueExitNode 전용 GraphView 시각 노드.
 *
 * 특징 / 지원기능 ::
 * + hdialogue-exit-node CSS (어두운 회색 테두리)
 * + 바디: exitKey 표시 + clearStageOnExit == true 이면 "clear stage" 라벨 추가
 *
 * 주의사항 ::
 * exitKey 빈 문자열이면 "(default)" 표시.
 * =========================================================
 */
#endif

using UnityEngine.UIElements;
using HWindows.Editor.NodeWindow;

namespace HDialogue.Editor {
    public sealed class HGraphDialogueExitNode : HGraphNode {
        public HGraphDialogueExitNode(DialogueExitNode data, bool isRoot = false) : base(data, isRoot) {
            AddToClassList("hdialogue-exit-node");
            _AddDialogueStyleSheet();
            bodyArea.Clear();

            string exitKey = string.IsNullOrEmpty(data.ExitKey) ? "(default)" : data.ExitKey;
            var keyLabel = new Label(exitKey);
            keyLabel.AddToClassList("hdialogue-speaker-label");
            bodyArea.Add(keyLabel);

            if (data.ClearStageOnExit) {
                var clearLabel = new Label("clear stage");
                clearLabel.AddToClassList("hdialogue-meta-label");
                bodyArea.Add(clearLabel);
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
 * @Jason - PKH 2026.05.15 HGraphDialogueExitNode 베이스 코드 생성
 *
 * # 목적
 * - HCUP-2.3.0 Phase 5 — DialogueExitNode 전용 시각 노드
 *
 * # 설계 결정
 * - clearStageOnExit: true일 때만 라벨 추가. false인 경우 빈 공간 낭비 없음.
 * - "(default)": 빈 exitKey를 시각적으로 구분 — 그래프에서 "공백"인지 "의도적 기본"인지 명확히
 *
 * =============================================================================
 */
#endif

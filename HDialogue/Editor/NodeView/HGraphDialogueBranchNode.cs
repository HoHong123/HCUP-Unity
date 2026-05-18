#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- DialogueBranchNode 전용 GraphView 시각 노드.
 *
 * 특징 / 지원기능 ::
 * + HGraphHubNode 상속 — 다중 출구 포트 + 키 목록 바디 재사용
 * + hdialogue-branch-node CSS (보라 테두리)
 * + conditionKey + BranchMode 행을 hub-entry-list 위에 삽입
 *
 * 주의사항 ::
 * base 생성자가 hgraph-hub-node 클래스 추가 → 즉시 제거 후 branch 클래스 추가.
 * =========================================================
 */
#endif

using UnityEngine.UIElements;
using HWindows.Editor.NodeWindow;
using HWindows.NodeWindow;

namespace HDialogue.Editor {
    public sealed class HGraphDialogueBranchNode : HGraphHubNode {
        public HGraphDialogueBranchNode(DialogueBranchNode data, bool isRoot = false) : base(data, isRoot) {
            RemoveFromClassList("hgraph-hub-node");
            AddToClassList("hdialogue-branch-node");
            _AddDialogueStyleSheet();

            var row = new VisualElement();
            row.AddToClassList("hdialogue-meta-row");

            string condKey = string.IsNullOrEmpty(data.ConditionKey) ? "(no condition)" : data.ConditionKey;
            var condLabel = new Label(condKey);
            condLabel.AddToClassList("hdialogue-speaker-label");
            row.Add(condLabel);

            var modeLabel = new Label($"[{data.Mode}]");
            modeLabel.AddToClassList("hdialogue-facing-label");
            row.Add(modeLabel);

            bodyArea.Insert(0, row);
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
 * @Jason - PKH 2026.05.15 HGraphDialogueBranchNode 베이스 코드 생성
 *
 * # 목적
 * - HCUP-2.3.0 Phase 5 — DialogueBranchNode 전용 시각 노드
 *
 * # 설계 결정
 * - HGraphHubNode 상속: Boolean/IntRange/Switch 분기 키 목록 UI 재사용
 * - bodyArea.Insert(0, row): conditionKey+Mode 행을 hub-entry-list 앞에 삽입
 *
 * =============================================================================
 */
#endif

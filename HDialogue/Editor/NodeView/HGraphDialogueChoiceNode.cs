#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- DialogueChoiceNode 전용 GraphView 시각 노드.
 *
 * 특징 / 지원기능 ::
 * + HGraphHubNode 상속 — 다중 출구 포트 + 키 목록 바디 재사용
 * + hdialogue-choice-node CSS (노란 테두리)
 * + promptText 미리보기를 hub-entry-list 위에 삽입
 *
 * 주의사항 ::
 * base 생성자가 hgraph-hub-node 클래스 추가 → 즉시 제거 후 choice 클래스 추가.
 * bodyArea.Insert(0, ...) 로 프롬프트 행을 hub-entry-list 앞에 위치.
 * =========================================================
 */
#endif

using UnityEngine.UIElements;
using HWindows.Editor.NodeWindow;
using HWindows.NodeWindow;

namespace HDialogue.Editor {
    public sealed class HGraphDialogueChoiceNode : HGraphHubNode {
        public HGraphDialogueChoiceNode(DialogueChoiceNode data, bool isRoot = false) : base(data, isRoot) {
            RemoveFromClassList("hgraph-hub-node");
            AddToClassList("hdialogue-choice-node");
            _AddDialogueStyleSheet();

            if (!string.IsNullOrEmpty(data.PromptText)) {
                string plain = data.PromptText;
                if (plain.Length > 45) plain = plain[..45] + "…";
                var promptLabel = new Label($"\"{plain}\"");
                promptLabel.AddToClassList("hdialogue-text-preview");
                bodyArea.Insert(0, promptLabel);
            }
        }

        private void _AddDialogueStyleSheet() {
            StyleSheet sheet = DialogueStyleSheetLoader.Get();
            if (sheet != null) styleSheets.Add(sheet);
        }
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.15 HGraphDialogueChoiceNode 베이스 코드 생성
 *
 * # 목적
 * - HCUP-2.3.0 Phase 5 — DialogueChoiceNode 전용 시각 노드
 *
 * # 설계 결정
 * - HGraphHubNode 상속: choices 키 목록·Add/Remove UI 재사용
 * - RemoveFromClassList("hgraph-hub-node"): base 생성자 추가 클래스 제거 후 hdialogue-choice-node 적용
 * - bodyArea.Insert(0, promptLabel): hub-entry-list(index 0)보다 앞에 프롬프트 삽입
 *
 * =============================================================================
 */
#endif

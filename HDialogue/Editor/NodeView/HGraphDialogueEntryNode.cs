#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- DialogueEntryNode 전용 GraphView 시각 노드.
 *
 * 특징 / 지원기능 ::
 * + hdialogue-entry-node CSS (회색 테두리)
 * + 바디: 진입점 노드라 별도 데이터 없음 — 빈 바디
 *
 * 주의사항 ::
 * isRoot == true 이면 HGraphNode 기반 헤더가 루트 색(노란)으로 표시됨.
 * =========================================================
 */
#endif

using HWindows.Editor.NodeWindow;

namespace HDialogue.Editor {
    public sealed class HGraphDialogueEntryNode : HGraphNode {
        public HGraphDialogueEntryNode(DialogueEntryNode data, bool isRoot = false) : base(data, isRoot) {
            AddToClassList("hdialogue-entry-node");
            _AddDialogueStyleSheet();
            bodyArea.Clear();
        }

        private void _AddDialogueStyleSheet() {
            UnityEngine.UIElements.StyleSheet sheet = DialogueStyleSheetLoader.Get();
            if (sheet != null) styleSheets.Add(sheet);
        }
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.15 HGraphDialogueEntryNode 베이스 코드 생성
 *
 * # 목적
 * - HCUP-2.3.0 Phase 5 — DialogueEntryNode 전용 시각 노드
 *
 * # 설계 결정
 * - bodyArea.Clear() 유지: HGraphNode 기본 바디(DataNode.Title 라벨 등)를 제거해 헤더만 표시
 *
 * =============================================================================
 */
#endif

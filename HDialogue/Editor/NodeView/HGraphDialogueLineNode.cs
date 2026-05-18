#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- DialogueLineNode 전용 GraphView 시각 노드.
 *
 * 특징 / 지원기능 ::
 * + hdialogue-line-node CSS (파랑 테두리)
 * + 바디: 화자키 + 방향 화살표 + 텍스트 미리보기 + 슬롯/포즈 + 포트레이트 스트립
 *
 * 주의사항 ::
 * DialogueLineNodePreviewDrawer.Build — registry null 시 포트레이트 스트립 생략.
 * =========================================================
 */
#endif

using UnityEditor;
using UnityEngine.UIElements;
using HWindows.Editor.NodeWindow;
using HWindows.NodeWindow;

namespace HDialogue.Editor {
    public sealed class HGraphDialogueLineNode : HGraphNode {
        public HGraphDialogueLineNode(DialogueLineNode data, bool isRoot = false) : base(data, isRoot) {
            AddToClassList("hdialogue-line-node");
            _AddDialogueStyleSheet();
            bodyArea.Clear();
            DialogueLineNodePreviewDrawer.Build(data, bodyArea);
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
 * @Jason - PKH 2026.05.15 HGraphDialogueLineNode 베이스 코드 생성
 * - HCUP-2.3.0 Phase 5 — DialogueLineNode 전용 시각 노드
 * =============================================================================
 */
#endif

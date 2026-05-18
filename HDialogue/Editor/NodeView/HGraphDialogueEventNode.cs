#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- DialogueEventNode 전용 GraphView 시각 노드.
 *
 * 특징 / 지원기능 ::
 * + hdialogue-event-node CSS (초록 테두리)
 * + 바디: eventKey(eventArg) 형식 한 줄 표시
 *
 * 주의사항 ::
 * EventArg 없으면 괄호 생략 (EventKey만 표시).
 * =========================================================
 */
#endif

using UnityEngine.UIElements;
using HWindows.Editor.NodeWindow;

namespace HDialogue.Editor {
    public sealed class HGraphDialogueEventNode : HGraphNode {
        public HGraphDialogueEventNode(DialogueEventNode data, bool isRoot = false) : base(data, isRoot) {
            AddToClassList("hdialogue-event-node");
            _AddDialogueStyleSheet();
            bodyArea.Clear();

            string display = string.IsNullOrEmpty(data.EventKey) ? "(no event)" : data.EventKey;
            if (!string.IsNullOrEmpty(data.EventArg)) display += $"({data.EventArg})";

            var label = new Label(display);
            label.AddToClassList("hdialogue-text-preview");
            bodyArea.Add(label);
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
 * @Jason - PKH 2026.05.15 HGraphDialogueEventNode 베이스 코드 생성
 *
 * # 목적
 * - HCUP-2.3.0 Phase 5 — DialogueEventNode 전용 시각 노드
 *
 * # 설계 결정
 * - EventKey(EventArg) 단일 라벨: 이벤트 노드는 정보량이 적어 한 줄로 충분
 *
 * =============================================================================
 */
#endif

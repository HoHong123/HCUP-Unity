#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- DialogueCinematicNode 전용 GraphView 시각 노드.
 *
 * 특징 / 지원기능 ::
 * + 지시 목록 요약 표시: "Verb Target [arg]" 포맷 라벨
 * + 지시 0개이면 "(no instructions)" 표시
 * + waitForTransition == true 이면 "wait transition" 라벨 추가
 *
 * 주의사항 ::
 * targetCharacterKey 빈 문자열이면 "?" 로 표시.
 * =========================================================
 */
#endif

using UnityEngine.UIElements;
using HWindows.Editor.NodeWindow;

namespace HDialogue.Editor {
    public sealed class HGraphDialogueCinematicNode : HGraphNode {
        public HGraphDialogueCinematicNode(DialogueCinematicNode data, bool isRoot = false) : base(data, isRoot) {
            AddToClassList("hdialogue-cinematic-node");
            _AddDialogueStyleSheet();
            bodyArea.Clear();

            if (data.Instructions.Count == 0) {
                var emptyLabel = new Label("(no instructions)");
                emptyLabel.AddToClassList("hdialogue-meta-label");
                bodyArea.Add(emptyLabel);
            } else {
                foreach (CinematicInstruction ins in data.Instructions) {
                    string target = string.IsNullOrEmpty(ins.TargetCharacterKey) ? "?" : ins.TargetCharacterKey;
                    string text = string.IsNullOrEmpty(ins.Arg)
                        ? $"{ins.Verb}  {target}"
                        : $"{ins.Verb}  {target}  [{ins.Arg}]";
                    var label = new Label(text);
                    label.AddToClassList("hdialogue-meta-label");
                    bodyArea.Add(label);
                }
            }

            if (data.WaitForTransition) {
                var waitLabel = new Label("wait transition");
                waitLabel.AddToClassList("hdialogue-meta-label");
                bodyArea.Add(waitLabel);
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
 * @Jason - PKH 2026.05.16 HGraphDialogueCinematicNode 신규 생성
 *
 * # 목적
 * - HCUP-2.3.0 — DialogueCinematicNode 전용 그래프 뷰 노드
 *
 * # 설계 결정
 * - "Verb  Target  [arg]" 포맷: Inspector 없이 그래프에서 지시 내용 한눈에 파악.
 * - waitForTransition 라벨: 캔버스에서 흐름 타이밍 의도 시각화.
 *
 * =============================================================================
 */
#endif

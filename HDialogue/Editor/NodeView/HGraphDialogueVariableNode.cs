#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- DialogueVariableNode 전용 GraphView 시각 노드.
 *
 * 특징 / 지원기능 ::
 * + hdialogue-variable-node CSS (청록 테두리)
 * + 바디: variableKey / op + 활성 값 표시
 * + VariableOp 별 값 표현: SetBool→true/false, SetInt/AddInt→숫자, SetString→"문자열", ToggleBool→toggle
 *
 * 주의사항 ::
 * op에 무관한 값 필드는 무시 (Inspector와 동일 처리).
 * =========================================================
 */
#endif

using UnityEngine.UIElements;
using HWindows.Editor.NodeWindow;

namespace HDialogue.Editor {
    public sealed class HGraphDialogueVariableNode : HGraphNode {
        public HGraphDialogueVariableNode(DialogueVariableNode data, bool isRoot = false) : base(data, isRoot) {
            AddToClassList("hdialogue-variable-node");
            _AddDialogueStyleSheet();
            bodyArea.Clear();

            string varKey = string.IsNullOrEmpty(data.VariableKey) ? "(no variable)" : data.VariableKey;
            var keyLabel = new Label(varKey);
            keyLabel.AddToClassList("hdialogue-speaker-label");
            bodyArea.Add(keyLabel);

            string valueStr = data.Op switch {
                VariableOp.SetBool    => data.BoolValue.ToString().ToLowerInvariant(),
                VariableOp.ToggleBool => "toggle",
                VariableOp.SetInt     => data.IntValue.ToString(),
                VariableOp.AddInt     => (data.IntValue >= 0 ? "+" : "") + data.IntValue,
                VariableOp.SetString  => $"\"{data.StringValue}\"",
                _                     => string.Empty
            };

            var opLabel = new Label($"{data.Op} {valueStr}".Trim());
            opLabel.AddToClassList("hdialogue-meta-label");
            bodyArea.Add(opLabel);
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
 * @Jason - PKH 2026.05.15 HGraphDialogueVariableNode 베이스 코드 생성
 *
 * # 목적
 * - HCUP-2.3.0 Phase 5 — DialogueVariableNode 전용 시각 노드
 *
 * # 설계 결정
 * - switch 표현식으로 op별 값 문자열 분기: ToggleBool은 값 없으므로 "toggle" 리터럴 사용
 * - AddInt 양수 앞 "+" 명시: +5 / -3 형식으로 의도 직관적 표현
 *
 * =============================================================================
 */
#endif

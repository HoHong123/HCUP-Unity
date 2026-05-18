#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- IDialogueVariableContext 변수 조작 노드.
 *
 * 특징 / 지원기능 ::
 * + variableKey + op + 타입별 값 필드(string/int/bool)
 * + 즉시 통과 — variables null이면 워닝 + 무동작
 * + op 별 활성 값 필드: SetBool→boolValue, SetInt/AddInt→intValue, SetString→stringValue
 *
 * 주의사항 ::
 * Inspector에서 op와 무관한 값 필드를 임의 수정해도 무시됨 — op 기준으로만 처리.
 * =========================================================
 */
#endif

using HInspector;
using HWindows.NodeWindow;
using UnityEngine;

namespace HDialogue {
    public sealed class DialogueVariableNode : BaseNode {
        #region Fields
        [HTitle("Variable")]
        [SerializeField]
        string variableKey;
        [SerializeField]
        VariableOp op;

        [HTitle("Value")]
        [SerializeField]
        string stringValue;
        [SerializeField]
        int intValue;
        [SerializeField]
        bool boolValue;
        #endregion

        #region Properties
        public string VariableKey => variableKey;
        public VariableOp Op => op;
        public string StringValue => stringValue;
        public int IntValue => intValue;
        public bool BoolValue => boolValue;
        #endregion

        public override string ClipboardMagic => "HGRAPH_DIALOGUE_VARIABLE_NODE_V1";
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.15 DialogueVariableNode 베이스 코드 생성
 *
 * # 목적
 * - 그래프 내 변수 조작 노드 — HCUP-2.3.0 Phase 1
 *
 * # 설계 결정
 * - string/int/bool 값 필드 전부 선언: op 별 하나만 유효하나 Inspector에서 모두 표시
 *   → Phase 5에서 [ShowIf] 등으로 조건부 표시 개선 가능
 *
 * # 사용 흐름
 * - DialogueDirector._ProcessVariableNode → variables?.SetBool/SetInt/SetString/AddInt/ToggleBool
 * - variables == null → 워닝 1회 + 무동작
 *
 * =============================================================================
 */
#endif

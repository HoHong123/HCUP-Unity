#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 변수 기반 조건 분기 노드. HubNode 상속으로 다중 출구 포트 보유.
 *
 * 특징 / 지원기능 ::
 * + BranchMode.Boolean  - TryGetBool → "true" / "false" 포트 키 매칭
 * + BranchMode.IntRange - TryGetInt → "min_max" 구간 포트 키 매칭
 * + BranchMode.Switch   - TryGetString → 포트 키 직접 비교
 * + variables null → 워닝 + Boolean false 기준 처리
 *
 * 주의사항 ::
 * Boolean 모드 허브 포트 키는 "true" / "false" 두 개만 허용 (Phase 3 Validator 강제).
 * IntRange 포트 키 형식: "min_max" — 예: "0_5", "6_10", "11_99".
 * =========================================================
 */
#endif

using HInspector;
using HWindows.NodeWindow;
using UnityEngine;

namespace HDialogue {
    public sealed class DialogueBranchNode : HubNode {
        #region Fields
        [HTitle("Branch")]
        [SerializeField]
        string conditionKey;
        [SerializeField]
        BranchMode mode;
        #endregion

        #region Properties
        public string ConditionKey => conditionKey;
        public BranchMode Mode => mode;
        #endregion

        public override string ClipboardMagic => "HGRAPH_DIALOGUE_BRANCH_NODE_V1";
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.15 DialogueBranchNode 베이스 코드 생성
 *
 * # 목적
 * - IDialogueVariableContext 평가 기반 분기 허브 노드 — HCUP-2.3.0 Phase 1
 *
 * # 설계 결정
 * - HubNode 상속: BranchMode별 결과 키로 허브 포트 매칭
 * - OnValidate 없음: Boolean "true"/"false" 2포트 강제는 Phase 3 Validator 책임
 *   (entries mutation 불가 — HWindows internal 제약)
 *
 * # 사용 흐름
 * - DialogueDirector._ProcessBranchNode
 *   Boolean: TryGetBool → "true"/"false" HubNodeEdge 매칭
 *   IntRange: TryGetInt → "min_max" 구간 포트 탐색
 *   Switch: TryGetString → 포트 키 직접 비교
 * - 매칭 실패 → 워닝 + Finished
 *
 * =============================================================================
 */
#endif

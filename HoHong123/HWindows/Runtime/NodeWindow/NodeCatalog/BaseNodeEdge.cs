using System;
using HWindows.NodeWindow.Identity;
using UnityEngine;

namespace HWindows.NodeWindow {
    [Serializable]
    public abstract class BaseNodeEdge {
        #region Fields
        [SerializeField]
        NodeUID branchUID;
        [SerializeField]
        NodeUID leafUID;
        #endregion

        #region Properties
        public NodeUID BranchUID => branchUID;
        public NodeUID LeafUID => leafUID;
        #endregion

        #region Internal - Identity
        // Author 전용 진입점. 최초 1회만 endpoint 할당, 이후 재할당 차단.
        internal void AssignIdentity(NodeUID branch, NodeUID leaf) {
            if (branchUID.IsValid || leafUID.IsValid) return;
            branchUID = branch;
            leafUID = leaf;
        }
        #endregion

        #region Public - Summary
        public virtual string GetEdgeSummary() => $"{branchUID} → {leafUID}";
        #endregion
    }
}

#if UNITY_EDITOR
// =============================================================================
// Dev Log
// =============================================================================
// @Jason - PKH 2026-04-22 BaseNodeEdge 의 역할 - 그래프 routing meta plain class
//
//   [역할]
//   - 두 노드를 잇는 routing 정보 (branchUID + leafUID).
//   - 도메인 의미는 서브클래스가 추가 (예: 트리거 조건, 가중치, 페이로드).
//
//   [SO 가 아닌 이유 - Node 와의 비대칭 설계]
//   - Edge 는 catalog 내부 전용, 외부 GUID 참조 없음.
//   - 독립 Project window 선택 대상 아님.
//   - UnityEngine.Object lifecycle (OnEnable 등) 불필요.
//   - Plain class 가 메모리 (~50B vs ~200B SO 베이스) + 응집도 모두 더 적절.
//   + "노드가 SO 니까 엣지도 SO" 대칭은 설계 논거 아님.
//   + 참고: Unity Animator 자체가 같은 비대칭 — AnimatorState=SO,
//     AnimatorTransition=plain class.
//
//   [직렬화]
//   - catalog 가 [SerializeReference] List<BaseNodeEdge> 로 보유.
//   - SerializeReference 가 abstract base 의 polymorphic 직렬화 지원.
//
//   [리네임 취약성 - FQN 직렬화의 함정]
//   - SerializeReference 는 클래스 FQN 으로 .asset YAML 에 저장.
//   - 클래스 리네임/이동 시 데이터 유실 위험.
//   + 1차 방어: 신중한 네이밍으로 리네임 회피.
//   + 비상 대응: [UnityEngine.Scripting.APIUpdating.MovedFrom] (일상 도구 아님).
//
//   [유효성 invariants - Author 가 강제]
//   - branchUID != leafUID (self-loop 금지)
//   + 재참조가 필요하면 그래프 구조가 아닌 "참조 반복 호출" 같은 기능 레이어로.
//   - (branchUID, leafUID) pair 중복 불가 (parallel edge 금지)
//   + 중복 이벤트가 필요하면 엣지의 payload (메타데이터·이벤트 리스트) 확장.
//
//   [Author 진입점] AssignIdentity 는 internal, 최초 1회만 endpoint 할당.
// =============================================================================
#endif

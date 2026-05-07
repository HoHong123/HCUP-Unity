using HWindows.NodeWindow.Identity;
using UnityEngine;

namespace HWindows.NodeWindow {
    public abstract class BaseNode : ScriptableObject {
        #region Fields
        [SerializeField]
        NodeUID uid;
        [SerializeField]
        string title;
        #endregion

        #region Properties
        public NodeUID UID => uid;
        public string Title => title;
        #endregion

        #region Internal - Identity
        // Author 전용 진입점. 최초 1회만 UID/title 할당, 이후 재할당 차단.
        internal void AssignIdentity(NodeUID assigned, string initialTitle) {
            if (uid.IsValid) return;
            uid = assigned;
            title = initialTitle;
        }

        internal void SetTitle(string newTitle) {
            title = newTitle;
        }

        // Phase 1-D Duplicate 전용. ScriptableObject.Instantiate 후 원본 UID 가 복사된 상태에서
        // AssignIdentity 가 다시 호출 가능하도록 identity 만 비움. 도메인 데이터는 보존.
        internal void ResetIdentity() {
            uid = NodeUID.None;
            title = string.Empty;
        }
        #endregion

        #region Public - Summary
        public virtual string GetInspectorSummary(NodeCatalogSO catalog) {
            int incoming = 0;
            int outgoing = 0;
            foreach (BaseNodeEdge e in catalog.Edges) {
                if (e == null) continue;
                if (e.LeafUID == uid) incoming++;
                if (e.BranchUID == uid) outgoing++;
            }
            return $"[{GetType().Name}] {title} (UID={uid.Value}, ↓{outgoing} ↑{incoming})";
        }
        #endregion

        #region Public - Clipboard (Phase 1-D)
        // 도메인별 클립보드 식별자 (Cut/Copy wrapper 의 magic).
        // 서브 override 권장 — 도메인 추가 시 자기 magic 명시.
        // 명명 규칙: "HGRAPH_<DOMAIN>_NODE_V<N>" — 예: "HGRAPH_SIMPLE_NODE_V1".
        // 미override 시 BaseNode default 가 그대로 적용 (HGraphClipboard 의 prefix/suffix 패턴 통과).
        public virtual string ClipboardMagic => "HGRAPH_BASE_NODE_V1";
        #endregion
    }
}

#if UNITY_EDITOR
// =============================================================================
// Dev Log
// =============================================================================
// @Jason - PKH 2026-04-22 BaseNode 의 역할 - 그래프 정체성만 담는 추상 SO 베이스
//
//   [역할]
//   - 도메인 데이터 없는 그래프 정체성 베이스 (UID + Title 만).
//   - ScriptableObject 상속 → catalog 의 sub-asset 으로 저장됨.
//
//   [필드 경계 - 매우 중요]
//   - UID, Title 만 보유. 그 외 도메인 필드·인접 정보·에셋 참조 모두 금지.
//   + 인접 관계(Branch/Leaf 리스트) 미보유: catalog.edges 가 단일 source.
//   + 자기 이웃을 모름. catalog.GetIncomingEdges(uid) 등으로 외부 조회.
//   + 이 정책으로 양방향 싱크 책임이 구조적으로 사라짐.
//
//   [도메인 확장 정책]
//   - 도메인 SO 는 BaseNode 상속 (예: DialogueNode : BaseNode).
//   - 에셋 참조는 AssetReference (Addressables) 또는 string key 로 간접만.
//   + UnityEngine.Object 타입 직접 필드 금지 (Sprite/AnimationClip/AudioClip 등).
//   + HCUP AssetHandler 경로 강제 (CLAUDE.md 전역 규칙).
//   + SO 메모리 오버헤드 1000-3000배 감소 + 빌드 크기 분리 가능.
//
//   [Author 진입점]
//   - AssignIdentity / SetTitle 은 internal — InternalsVisibleTo 로 Editor
//     asmdef 의 Author 만 호출 가능. 외부 어셈블리 컴파일 차단.
//   - AssignIdentity 는 최초 1회만 (uid.IsValid 체크). 재할당 차단.
//
//   [GetInspectorSummary]
//   - virtual — 도메인 서브가 override 해 요약 확장.
//   - 기본 구현은 UID + Title + 연결 개수 (↓ outgoing / ↑ incoming).
//
//   [Phase 1-D 확장 - 2026-05-07/08]
//   - ResetIdentity() internal 추가 (Duplicate / Paste 공통 지원):
//   + ScriptableObject.Instantiate 가 readonly-style internal 필드 (uid/title) 복사 후
//     AssignIdentity 가 if (uid.IsValid) return 으로 차단됨.
//   + ResetIdentity 로 NodeUID.None 리셋 → AssignIdentity 새 UID 발급 가능.
//   + 도메인 데이터는 보존 (식별자만 비움). InternalsVisibleTo 로 Editor Author 만 호출 가능.
//   - ClipboardMagic virtual 추가 (사용자 결정 2026-05-08):
//   + Cut/Copy 의 wrapper magic. 도메인 서브 override 권장.
//   + 명명 규칙 "HGRAPH_<DOMAIN>_NODE_V<N>" — SimpleNode → "HGRAPH_SIMPLE_NODE_V1".
//   + Mixed 도메인 selection 시 HGraphClipboard.Serialize 가 일관성 검사로 거부.
//   + base default = "HGRAPH_BASE_NODE_V1" (서브 override 안 해도 패턴 정합 통과).
//   - 폐기: Phase 1-D 본 라운드 잠시 추가됐던 GetCopyText virtual + _AppendAdjacencySection helper.
//   + 사용자 결정 — Copy 도 JSON 으로 통일 (HGraphClipboard.Serialize 활용). ToString 텍스트 폐기.
//   + milestone §1-2-2 의 ToString 형식 명세는 후속 phase (디버그 콘솔 / Preview Phase 4) 재도입 가능.
// =============================================================================
#endif

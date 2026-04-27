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
// =============================================================================
#endif

using System.Collections.Generic;
using UnityEngine;
using HInspector;
using HCollection;
using HWindows.NodeWindow.Identity;

namespace HWindows.NodeWindow {
    [CreateAssetMenu(menuName = "HWindows/Node Catalog")]
    public class NodeCatalogSO : ScriptableObject, ISerializationCallbackReceiver {
        #region Fields
        [HTitle("Description")]
        [SerializeField, TextArea]
        string editorDescription;
        
        [HTitle("Nodes")]
        [SerializeField]
        NodeUID rootUID = NodeUID.None;
        [SerializeField]
        HDictionary<NodeUID, BaseNode> nodes = new();

        [HTitle("Edges")]
        [SerializeReference]
        List<BaseNodeEdge> edges = new();

#if UNITY_EDITOR
        [HTitle("Debug")]
        [SerializeField]
        HDictionary<NodeUID, Vector2> editorNodeLayouts = new();
#endif

        [System.NonSerialized]
        Dictionary<(NodeUID Branch, NodeUID Leaf), BaseNodeEdge> edgeByPair;
        #endregion

        #region Properties
        public string EditorDescription => editorDescription;
        public IReadOnlyDictionary<NodeUID, BaseNode> Nodes => nodes;
        public IReadOnlyList<BaseNodeEdge> Edges => edges;
        public IReadOnlyDictionary<(NodeUID, NodeUID), BaseNodeEdge> EdgeByPair {
            get {
                if (edgeByPair == null) _RebuildEdgeCache();
                return edgeByPair;
            }
        }
        public int NodeCount => nodes.Count;
        public int EdgeCount => edges.Count;
        public NodeUID RootUID => rootUID;
        public bool HasRoot => rootUID.IsValid;
#if UNITY_EDITOR
        public IReadOnlyDictionary<NodeUID, Vector2> EditorNodeLayouts => editorNodeLayouts;
#endif
        #endregion

        #region Public - Serialization
        public void OnBeforeSerialize() { }
        public void OnAfterDeserialize() {
            edgeByPair = null;
        }
        #endregion

        #region Public - Adjacency Queries
        public IEnumerable<BaseNodeEdge> GetIncomingEdges(NodeUID leaf) {
            foreach (BaseNodeEdge e in edges) {
                if (e != null && e.LeafUID == leaf) yield return e;
            }
        }

        public IEnumerable<BaseNodeEdge> GetOutgoingEdges(NodeUID branch) {
            foreach (BaseNodeEdge e in edges) {
                if (e != null && e.BranchUID == branch) yield return e;
            }
        }

        public IEnumerable<BaseNode> GetBranchNodes(NodeUID leaf) {
            foreach (BaseNodeEdge e in GetIncomingEdges(leaf)) {
                if (nodes.TryGetValue(e.BranchUID, out BaseNode n)) yield return n;
            }
        }

        public IEnumerable<BaseNode> GetLeafNodes(NodeUID branch) {
            foreach (BaseNodeEdge e in GetOutgoingEdges(branch)) {
                if (nodes.TryGetValue(e.LeafUID, out BaseNode n)) yield return n;
            }
        }

        public bool HasEdgeBetween(NodeUID branch, NodeUID leaf) =>
            EdgeByPair.ContainsKey((branch, leaf));

        public bool TryGetEdge(NodeUID branch, NodeUID leaf, out BaseNodeEdge edge) =>
            EdgeByPair.TryGetValue((branch, leaf), out edge);
        #endregion

        #region Internal - Mutation
        // 모든 mutation API는 Author 경유. InternalsVisibleTo로 Editor asmdef에만 노출.
        internal void InternalAddNode(BaseNode node) {
            nodes.Add(node.UID, node);
        }

        internal void InternalRemoveNode(NodeUID uid) {
            nodes.Remove(uid);
        }

        internal void InternalAddEdge(BaseNodeEdge edge) {
            edges.Add(edge);
            edgeByPair?.Add((edge.BranchUID, edge.LeafUID), edge);
        }

        internal void InternalRemoveEdge(NodeUID branch, NodeUID leaf) {
            int idx = edges.FindIndex(e => e != null && e.BranchUID == branch && e.LeafUID == leaf);
            if (idx < 0) return;
            edges.RemoveAt(idx);
            edgeByPair?.Remove((branch, leaf));
        }

        internal void InternalSetRoot(NodeUID newRoot) {
            rootUID = newRoot;
        }

        internal void InternalClearRoot() {
            rootUID = NodeUID.None;
        }

#if UNITY_EDITOR
        internal void InternalSetLayout(NodeUID uid, Vector2 pos) {
            editorNodeLayouts[uid] = pos;
        }

        internal void InternalRemoveLayout(NodeUID uid) {
            editorNodeLayouts.Remove(uid);
        }
#endif
        #endregion

        #region Private
        void _RebuildEdgeCache() {
            edgeByPair = new Dictionary<(NodeUID, NodeUID), BaseNodeEdge>(edges.Count);
            foreach (BaseNodeEdge e in edges) {
                if (e != null) edgeByPair[(e.BranchUID, e.LeafUID)] = e;
            }
        }
        #endregion
    }
}

#if UNITY_EDITOR
// =============================================================================
// Dev Log
// =============================================================================
// @Jason - PKH 2026-04-22 NodeCatalogSO 의 역할 - 노드/엣지/루트의 단일 데이터 소유자
//
//   [역할]
//   - 그래프 한 개 = catalog asset 한 개.
//   - 노드 / 엣지 / 루트 / 에디터 설명 텍스트의 단일 소유자.
//   - sub-asset 으로 BaseNode 인스턴스들을 응축 저장.
//
//   [데이터 구조]
//   - nodes: HDictionary<NodeUID, BaseNode>
//   + HCollection 이 List+Dict 싱크 / 중복 키 검증 / 빌드 메모리 최적화 자동 처리.
//   + HDictionaryValidator 가 중복 UID 시 Save/Play/Build 차단.
//   - edges: [SerializeReference] List<BaseNodeEdge>
//   + abstract base 의 polymorphic 직렬화 지원.
//   + edgeByPair 캐시는 OnAfterDeserialize 에서 null 처리, lazy rebuild.
//   - edgeByPair: [NonSerialized] Dictionary<(NodeUID, NodeUID), BaseNodeEdge>
//   + C# 7+ 값 튜플 키 — GetHashCode/Equals 자동.
//   + Author mutation 시 증분 동기 (InternalAddEdge/InternalRemoveEdge).
//   - rootUID: NodeUID = None — 0..1 카디널리티.
//
//   [Mutation 경로]
//   - 모든 Internal* 메서드는 internal — InternalsVisibleTo 로 Editor asmdef
//     의 Author 만 호출 가능.
//   - 외부 어셈블리 (HGame · 05_Study 등) 는 컴파일 타임 차단.
//
//   [Out-of-scope (Phase 0 한정)]
//   - DepthTree 참조 NodeSO 리스트 (milestone §0 필드 #2): Phase 3 이월.
//   - non-generic 채택: 도메인 서브 catalog 의 노드 타입 제약은 별도 검증 필요.
//   - 커스텀 Inspector 에디터 (sub-asset preview 리스트): Phase 4 이월.
//
//   [Phase 1+ 소비 API]
//   - 조회: Nodes / Edges / EdgeByPair / RootUID / Get*Edges / Get*Nodes /
//     HasEdgeBetween / TryGetEdge.
//   - 수정: NodeCatalogAuthor 정적 함수 5개 경유.
//
//   [Phase 1-A 확장 - 2026-04-24]
//   - editorNodeLayouts: HDictionary<NodeUID, Vector2> 추가 (#if UNITY_EDITOR 가드)
//   + 노드 위치 저장소. catalog 가 "노드 + 엣지 + 루트 + 레이아웃" 단일 소유자로 확장.
//   + Runtime 빌드 바이너리에 침투 X. Phase 0 "Runtime SO = 순수 데이터" 계약 유지.
//   - InternalSetLayout / InternalRemoveLayout: Author 전용 mutation 진입점 (Editor-only).
//   - EditorNodeLayouts 프로퍼티: read-only 외부 조회용 (HGraphCanvas Populate 가 소비).
// =============================================================================
#endif

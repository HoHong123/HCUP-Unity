using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HWindows.Editor.NodeWindow.Identity;
using HWindows.NodeWindow;
using HWindows.NodeWindow.Identity;
using HDiagnosis.Logger;

namespace HWindows.Editor.NodeWindow.Authoring {
    public static class NodeCatalogAuthor {
        #region Const
        // 신규 노드 자동 배치 간격 (X). HGraphCanvas 의 _Populate 와 공유.
        public const float AUTO_LAYOUT_STRIDE_X = 220f;
        #endregion

        #region Events
        /// <summary>
        /// Catalog 의 노드/엣지/루트 변경을 알리는 정적 이벤트.
        /// Author 의 5 mutation 메서드 + Editor 측 ObjectChangeWatcher (Inspector 직접 수정 감지)
        /// 두 경로 모두 이 이벤트로 통합 발송. HGraphCanvas 등 시각 레이어가 단일 구독점.
        /// SetLayout 은 빈도가 높아 발송 대상 제외 (드래그마다 broadcast 시 깜빡임 유발).
        /// </summary>
        public static event System.Action<NodeCatalogSO> CatalogMutated;

        /// <summary>
        /// Author 외부 (Editor watcher 등) 가 mutation 알림을 발송할 수 있는 진입점.
        /// 직접 catalog 의 internal 데이터를 변경하지 않으며, 단순히 이벤트만 발송.
        /// </summary>
        public static void NotifyExternalMutation(NodeCatalogSO catalog) {
            if (catalog == null) return;
            CatalogMutated?.Invoke(catalog);
        }

        private static void _NotifyMutated(NodeCatalogSO catalog) {
            CatalogMutated?.Invoke(catalog);
        }
        #endregion

        #region Public - Node Lifecycle
        public static T CreateNode<T>(NodeCatalogSO catalog, string title = null) where T : BaseNode {
            if (catalog == null) {
                HLogger.Error("[NodeCatalogAuthor] catalog is null");
                return null;
            }

            NodeUID uid = NodeUIDRegistry.instance.Issue();

            string finalTitle = string.IsNullOrWhiteSpace(title) ? $"Node_{uid.Value}" : title;

            T node = ScriptableObject.CreateInstance<T>();
            node.name = finalTitle;
            node.AssignIdentity(uid, finalTitle);

            AssetDatabase.AddObjectToAsset(node, catalog);
            catalog.InternalAddNode(node);

            if (!catalog.HasRoot) catalog.InternalSetRoot(uid);

#if UNITY_EDITOR
            // 신규 노드는 즉시 자동 layout 부여. catalog 가 항상 nodes 와 layouts 동기 상태 유지.
            // 공식 = (기존 노드 수 - 1) * stride. catalog.Nodes.Count 는 방금 InternalAddNode 후라 N.
            int autoIndex = catalog.Nodes.Count - 1;
            Vector2 autoPos = new Vector2(autoIndex * AUTO_LAYOUT_STRIDE_X, 0f);
            catalog.InternalSetLayout(uid, autoPos);
#endif

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            _NotifyMutated(catalog);
            return node;
        }

        public static bool RemoveNode(NodeCatalogSO catalog, NodeUID uid) {
            if (catalog == null || !catalog.Nodes.TryGetValue(uid, out BaseNode node)) return false;

            // 연결된 모든 엣지 쌍 수집 후 일괄 제거 (orphan 방지)
            List<(NodeUID, NodeUID)> touching = new();
            foreach (BaseNodeEdge e in catalog.Edges) {
                if (e == null) continue;
                if (e.BranchUID == uid || e.LeafUID == uid) touching.Add((e.BranchUID, e.LeafUID));
            }
            foreach ((NodeUID b, NodeUID l) in touching) DisconnectEdge(catalog, b, l);

            // Root 이전 (현재 root 노드 제거 시)
            if (catalog.RootUID == uid) {
                NodeUID fallback = _FindAnyOtherNode(catalog, uid);
                if (fallback.IsValid) catalog.InternalSetRoot(fallback);
                else catalog.InternalClearRoot();
            }

            catalog.InternalRemoveNode(uid);
            AssetDatabase.RemoveObjectFromAsset(node);
            Object.DestroyImmediate(node, allowDestroyingAssets: true);

#if UNITY_EDITOR
            catalog.InternalRemoveLayout(uid);
#endif

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            _NotifyMutated(catalog);
            return true;
        }
        #endregion

        #region Public - Edge Lifecycle
        public static TEdge ConnectEdge<TEdge>(NodeCatalogSO catalog, NodeUID branch, NodeUID leaf)
            where TEdge : BaseNodeEdge, new() {
            if (!_ValidateEdgeCreation(catalog, branch, leaf, out string reason)) {
                HLogger.Warning($"[NodeCatalogAuthor] Edge 생성 거부: {reason}");
                return null;
            }

            TEdge edge = new();
            edge.AssignIdentity(branch, leaf);

            catalog.InternalAddEdge(edge);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            _NotifyMutated(catalog);
            return edge;
        }

        public static bool DisconnectEdge(NodeCatalogSO catalog, NodeUID branch, NodeUID leaf) {
            if (catalog == null || !catalog.HasEdgeBetween(branch, leaf)) return false;
            catalog.InternalRemoveEdge(branch, leaf);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            _NotifyMutated(catalog);
            return true;
        }
        #endregion

        #region Public - Root
        public static bool SetRoot(NodeCatalogSO catalog, NodeUID uid) {
            if (catalog == null || !catalog.Nodes.ContainsKey(uid)) return false;
            catalog.InternalSetRoot(uid);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            _NotifyMutated(catalog);
            return true;
        }
        #endregion

        #region Public - Layout (Phase 1-A)
        /// <summary>
        /// 노드 위치를 catalog 의 editorNodeLayouts 에 반영. SetDirty 만 호출 (SaveAssets 생략).
        /// Phase 1-A 에서는 Undo 통합 없음 - Phase 1-F 에서 외부 wrapping 레이어로 처리.
        /// </summary>
        public static void SetLayout(NodeCatalogSO catalog, NodeUID uid, Vector2 pos) {
            if (catalog == null) {
                HLogger.Error("[NodeCatalogAuthor] catalog is null in SetLayout");
                return;
            }
            if (!catalog.Nodes.ContainsKey(uid)) {
                HLogger.Warning($"[NodeCatalogAuthor] SetLayout rejected: node {uid} not in catalog");
                return;
            }

#if UNITY_EDITOR
            catalog.InternalSetLayout(uid, pos);
            EditorUtility.SetDirty(catalog);
#endif
        }
        #endregion

        #region Private - Validation
        static bool _ValidateEdgeCreation(NodeCatalogSO catalog, NodeUID branch, NodeUID leaf, out string reason) {
            reason = null;
            if (catalog == null) {
                reason = "catalog is null";
                return false;
            }
            if (!branch.IsValid || !leaf.IsValid) {
                reason = "invalid UID";
                return false;
            }
            if (branch == leaf) {
                reason = "self-loop forbidden";
                return false;
            }
            if (!catalog.Nodes.ContainsKey(branch)) {
                reason = $"branch node {branch} not in catalog";
                return false;
            }
            if (!catalog.Nodes.ContainsKey(leaf)) {
                reason = $"leaf node {leaf} not in catalog";
                return false;
            }
            if (catalog.HasEdgeBetween(branch, leaf)) {
                reason = $"parallel edge forbidden: {branch} → {leaf} already exists";
                return false;
            }
            return true;
        }

        static NodeUID _FindAnyOtherNode(NodeCatalogSO catalog, NodeUID exclude) {
            foreach (NodeUID uid in catalog.Nodes.Keys) {
                if (uid != exclude) return uid;
            }
            return NodeUID.None;
        }
        #endregion
    }
}

#if UNITY_EDITOR
// =============================================================================
// Dev Log
// =============================================================================
// @Jason - PKH 2026-04-22 NodeCatalogAuthor 의 역할 - mutation 단일 게이트
//
//   [역할]
//   - catalog 변경의 모든 경로가 통과해야 하는 Editor-only 정적 게이트.
//   - 상태 0, 필드 0. 모든 컨텍스트는 파라미터로 전달.
//   - 존재 이유: Runtime SO 를 순수 데이터로 유지 + 깨진 상태 생성 경로 차단.
//
//   [Author 가 하는 일]
//   - UID 발급 (Registry 호출) + sub-asset 생성 + AssetDatabase.AddObjectToAsset
//   - catalog.Internal* 메서드 호출 (HDictionary / List / rootUID 갱신)
//   - Validation 강제 (5가지 규칙)
//   - Cascade delete (노드 삭제 시 관련 엣지 일괄)
//   - Root 자동 배정 (첫 노드) + Root 이전 (root 노드 삭제 시 다른 노드로)
//   - EditorUtility.SetDirty + AssetDatabase.SaveAssets 페어 호출
//
//   [Author 가 하지 않는 일]
//   + 데이터 소유 (catalog 자체 필드)
//   + 상태 보유 (static class, 필드 0)
//   + Runtime 호출 (Editor asmdef + Registry Editor-only 로 구조적 차단)
//   + Domain 해석 (서브클래스 책임)
//   + Inspector 렌더링 (Phase 4 의 CustomEditor 책임)
//   + Undo/Redo 관리 (Phase 1 GUI 레이어 책임)
//
//   [Validation Rules (강제 지점)]
//   - self-loop 금지: branch == leaf → _ValidateEdgeCreation 거부
//   - parallel edge 금지: HasEdgeBetween 체크 → 거부
//   - 노드 존재 확인: Nodes.ContainsKey
//   - UID 유효성: branch.IsValid && leaf.IsValid
//   - catalog null 거부
//   + Orphan edge 금지: RemoveNode 의 cascade 로 구조적 보장.
//   + Root 카디널리티 0..1: InternalSetRoot/Clear 단일 진입점.
//
//   [에러 처리 정책]
//   - 회복 불가 (catalog null) → HLogger.Error
//   - 사용자 의도 검증 실패 (self-loop 시도) → HLogger.Warning
//   - 예외 던지지 않음. bool/null 반환 + 로그에 사유 포함.
//   + 에디터 인터랙션 중단을 회피하기 위한 정책.
//
//   [핵심 호출 패턴 - 모든 mutation 함수가 따름]
//   - 검증 → mutation → EditorUtility.SetDirty(catalog) → AssetDatabase.SaveAssets()
//   - 마지막 두 줄을 빠뜨리면 변경이 다음 에디터 재시작에 유실.
//
//   [DestroyImmediate 주의]
//   - sub-asset 제거 시 allowDestroyingAssets: true 명시 필수.
//   - false (기본값) 면 InvalidOperationException.
//   - 호출 순서: AssetDatabase.RemoveObjectFromAsset → DestroyImmediate.
//     역순이면 "destroyed object inside asset" 잘못된 상태.
//
//   [Phase 1+ 소비] GraphView 가 사용자 액션을 받아 Author 호출 →
//     catalog 갱신 → GraphView 재렌더 + Undo 레이어가 호출 전후로 감쌈.
//
//   [Phase 1-A 확장 - 2026-04-24]
//   - CreateNode<T>: title 파라미터 default = null 추가. null/whitespace 시 $"Node_{uid.Value}" 자동 fallback.
//   + milestone §1 "기본노드이름 = Node_{UID}" 강제 규칙의 Author 내부 적용 지점.
//   + 기존 호출부 (title 명시) 는 하위호환 유지.
//   - RemoveNode: cascade 마지막 단계에 InternalRemoveLayout 한 줄 추가 (#if UNITY_EDITOR).
//   + 노드 삭제 시 layout 도 자동 제거되어 orphan layout 방지.
//   - SetLayout: 신설. SetDirty 만 호출 (SaveAssets 생략) - "고빈도 상태 업데이트" 분류.
//   + Phase 0 기존 5개 메서드 (Create/Remove/Connect/Disconnect/SetRoot) 는 "저빈도 구조 변경" 으로
//     SaveAssets 즉시 호출. SetLayout 은 다른 분류로 공존.
//   + Undo 통합은 Phase 1-F 로 이월 - Author 는 "원시 mutation" 역할만 유지.
// =============================================================================
#endif

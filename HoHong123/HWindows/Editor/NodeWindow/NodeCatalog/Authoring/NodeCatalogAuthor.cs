using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HWindows.Editor.NodeWindow.Identity;
using HWindows.NodeWindow;
using HWindows.NodeWindow.Identity;
using HDiagnosis.Logger;

namespace HWindows.Editor.NodeWindow.Authoring {
    public static class NodeCatalogAuthor {
        #region Public - Node Lifecycle
        public static T CreateNode<T>(NodeCatalogSO catalog, string title) where T : BaseNode {
            if (catalog == null) {
                HLogger.Error("[NodeCatalogAuthor] catalog is null");
                return null;
            }

            NodeUID uid = NodeUIDRegistry.instance.Issue();
            T node = ScriptableObject.CreateInstance<T>();
            node.name = $"Node_{uid.Value}_{title}";
            node.AssignIdentity(uid, title);

            AssetDatabase.AddObjectToAsset(node, catalog);
            catalog.InternalAddNode(node);

            if (!catalog.HasRoot) catalog.InternalSetRoot(uid);

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
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

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
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
            return edge;
        }

        public static bool DisconnectEdge(NodeCatalogSO catalog, NodeUID branch, NodeUID leaf) {
            if (catalog == null || !catalog.HasEdgeBetween(branch, leaf)) return false;
            catalog.InternalRemoveEdge(branch, leaf);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return true;
        }
        #endregion

        #region Public - Root
        public static bool SetRoot(NodeCatalogSO catalog, NodeUID uid) {
            if (catalog == null || !catalog.Nodes.ContainsKey(uid)) return false;
            catalog.InternalSetRoot(uid);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return true;
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
// =============================================================================
#endif

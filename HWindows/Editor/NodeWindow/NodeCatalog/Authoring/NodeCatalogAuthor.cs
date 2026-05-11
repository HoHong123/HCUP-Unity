#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 카탈로그 mutation 단일 게이트 (Editor-only static class).
 *
 * 특징 ::
 * 상태 0, 필드 0. 모든 컨텍스트는 파라미터로 전달.
 * + CreateNode / DuplicateNode / RemoveNode / ConnectEdge / DisconnectEdge / SetRoot
 * + Cut / Paste (JSON 클립보드 기반)
 * + SetLayout / SetFoldoutOpen / SetOpenSize (고빈도 상태)
 *
 * 주의사항 ::
 * 모든 mutation 후 SetDirty + SaveAssets 트리오 필수.
 * + SetLayout / SetFoldoutOpen / SetOpenSize 는 SaveAssets 미호출 (고빈도 분류).
 * =========================================================
 */
#endif
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HWindows.Editor.NodeWindow;
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

            NodeUID uid = NodeUID.New();

            string finalTitle = string.IsNullOrWhiteSpace(title) ? $"Node_{uid.Value[..8]}" : title;

            Undo.RecordObject(catalog, "Create Node");

            T node = ScriptableObject.CreateInstance<T>();
            node.name = finalTitle;
            node.AssignIdentity(uid, finalTitle);

            AssetDatabase.AddObjectToAsset(node, catalog);
            Undo.RegisterCreatedObjectUndo(node, "Create Node");
            catalog.InternalAddNode(node);

            if (!catalog.HasRoot) catalog.InternalSetRoot(uid);

#if UNITY_EDITOR
            // 신규 노드 자동 layout — node 자체에 저장 (Phase 1-F 이관).
            int autoIndex = catalog.Nodes.Count - 1;
            node.SetEditorPosition(new Vector2(autoIndex * AUTO_LAYOUT_STRIDE_X, 0f));
#endif

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            _NotifyMutated(catalog);
            return node;
        }

        // Phase 3 — NodeCatalogSO 를 canvas 특정 위치에 CatalogNode 로 생성.
        // HGraphWindow 드래그드롭 (카탈로그 이미 바인드 상태) 전용 진입점.
        // 1:1 양방향: referenced 에도 catalog 를 참조하는 CatalogNode 자동 생성 (미존재 시).
        public static CatalogNode CreateCatalogNodeAt(NodeCatalogSO catalog, NodeCatalogSO referenced, Vector2 dropPosition) {
            if (catalog == null) {
                HLogger.Error("[NodeCatalogAuthor] catalog is null in CreateCatalogNodeAt");
                return null;
            }

            CatalogNode forwardNode = _CreateCatalogNodeCore(catalog, referenced, dropPosition);

            // 양방향: referenced 카탈로그에 역방향 CatalogNode(catalog) 미존재 시 자동 생성.
            if (referenced != null && referenced != catalog && !_HasCatalogNodeFor(referenced, catalog)) {
                _CreateCatalogNodeCore(referenced, catalog, new Vector2(100f, 100f));
            }

            return forwardNode;
        }

        // 단일 CatalogNode 생성 코어 — dirty/save/notify 포함. CreateCatalogNodeAt 와 양방향 생성만 사용.
        private static CatalogNode _CreateCatalogNodeCore(NodeCatalogSO catalog, NodeCatalogSO referenced, Vector2 position) {
            NodeUID uid = NodeUID.New();
            string title = referenced != null ? referenced.name : $"Node_{uid.Value[..8]}";

            Undo.RecordObject(catalog, "Create Catalog Node");

            CatalogNode node = ScriptableObject.CreateInstance<CatalogNode>();
            node.name = title;
            node.AssignIdentity(uid, title);

#if UNITY_EDITOR
            node.SetReferencedCatalog(referenced);
            node.SetEditorPosition(position);
#endif

            AssetDatabase.AddObjectToAsset(node, catalog);
            Undo.RegisterCreatedObjectUndo(node, "Create Catalog Node");
            catalog.InternalAddNode(node);

            if (!catalog.HasRoot) catalog.InternalSetRoot(uid);

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            _NotifyMutated(catalog);
            return node;
        }

        // referenced 카탈로그에 target 을 참조하는 CatalogNode 가 이미 있는지 확인.
        private static bool _HasCatalogNodeFor(NodeCatalogSO catalog, NodeCatalogSO target) {
            foreach (BaseNode node in catalog.Nodes.Values) {
#if UNITY_EDITOR
                if (node is CatalogNode cn && cn.ReferencedCatalog == target) return true;
#endif
            }
            return false;
        }

        public static T DuplicateNode<T>(NodeCatalogSO catalog, NodeUID sourceUID) where T : BaseNode {
            if (catalog == null) {
                HLogger.Error("[NodeCatalogAuthor] catalog is null in DuplicateNode");
                return null;
            }
            if (!catalog.Nodes.TryGetValue(sourceUID, out BaseNode source)) {
                HLogger.Warning($"[NodeCatalogAuthor] DuplicateNode rejected: source UID {sourceUID} not in catalog");
                return null;
            }
            T sourceTyped = source as T;
            if (sourceTyped == null) {
                HLogger.Warning($"[NodeCatalogAuthor] DuplicateNode rejected: source type mismatch (expected {typeof(T).Name}, got {source.GetType().Name})");
                return null;
            }

            NodeUID newUID = NodeUID.New();
            string baseTitle = $"Node_{newUID.Value[..8]}";

            Undo.RecordObject(catalog, "Duplicate Node");

            // ScriptableObject.Instantiate (P1D-c) — Unity 가 SerializedObject 복사 자동 처리.
            T duplicate = UnityEngine.Object.Instantiate(sourceTyped);
            duplicate.name = baseTitle;
            // 원본 UID/Title 이 복사된 상태 → ResetIdentity 후 AssignIdentity 새 UID 강제.
            duplicate.ResetIdentity();
            duplicate.AssignIdentity(newUID, baseTitle);

            AssetDatabase.AddObjectToAsset(duplicate, catalog);
            Undo.RegisterCreatedObjectUndo(duplicate, "Duplicate Node");
            catalog.InternalAddNode(duplicate);

#if UNITY_EDITOR
            // 위치 offset (40, 40) — 원본과 겹침 회피.
            duplicate.SetEditorPosition(sourceTyped.EditorPosition + new Vector2(40f, 40f));
#endif

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            _NotifyMutated(catalog);
            return duplicate;
        }

        public static bool RemoveNode(NodeCatalogSO catalog, NodeUID uid) {
            if (catalog == null || !catalog.Nodes.TryGetValue(uid, out BaseNode node)) return false;

            Undo.RecordObject(catalog, "Remove Node");

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
            // Undo.DestroyObjectImmediate: AssetDatabase.RemoveObjectFromAsset + DestroyImmediate 를 대체.
            // undo 시 sub-asset(editorPosition/FoldoutOpen/OpenSize 포함) 을 원자 복원.
            Undo.DestroyObjectImmediate(node);

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            _NotifyMutated(catalog);
            return true;
        }
        #endregion

        #region Public - HubNode Lifecycle
        // HubNode 생성 — CreateNode<HubNode> 와 같은 패턴이지만 포트 항목 없이 시작.
        public static HubNode CreateHubNode(NodeCatalogSO catalog, Vector2 position) {
            if (catalog == null) {
                HLogger.Error("[NodeCatalogAuthor] catalog is null in CreateHubNode");
                return null;
            }

            NodeUID uid = NodeUID.New();
            string title = $"Hub_{uid.Value[..8]}";

            Undo.RecordObject(catalog, "Create Hub Node");

            HubNode node = ScriptableObject.CreateInstance<HubNode>();
            node.name = title;
            node.AssignIdentity(uid, title);
#if UNITY_EDITOR
            node.SetEditorPosition(position);
#endif
            AssetDatabase.AddObjectToAsset(node, catalog);
            Undo.RegisterCreatedObjectUndo(node, "Create Hub Node");
            catalog.InternalAddNode(node);

            if (!catalog.HasRoot) catalog.InternalSetRoot(uid);

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            _NotifyMutated(catalog);
            return node;
        }

        // HubNode 에 출구 포트 항목 추가 — 키값 중복 여부는 사용자가 판단.
        public static bool AddHubEntry(NodeCatalogSO catalog, NodeUID hubUID, string key) {
            if (catalog == null || !catalog.Nodes.TryGetValue(hubUID, out BaseNode node)) {
                HLogger.Warning("[NodeCatalogAuthor] AddHubEntry: catalog or node not found");
                return false;
            }
            if (node is not HubNode hub) {
                HLogger.Warning("[NodeCatalogAuthor] AddHubEntry: node is not HubNode");
                return false;
            }

            Undo.RecordObject(hub, "Add Hub Entry");
            hub.AddEntry(key);
            EditorUtility.SetDirty(hub);
            AssetDatabase.SaveAssets();
            _NotifyMutated(catalog);
            return true;
        }

        // HubNode 의 출구 포트 항목 제거 — 해당 항목에 연결된 엣지도 cascade 제거.
        public static bool RemoveHubEntry(NodeCatalogSO catalog, NodeUID hubUID, int entryIndex) {
            if (catalog == null || !catalog.Nodes.TryGetValue(hubUID, out BaseNode node)) {
                HLogger.Warning("[NodeCatalogAuthor] RemoveHubEntry: catalog or node not found");
                return false;
            }
            if (node is not HubNode hub) {
                HLogger.Warning("[NodeCatalogAuthor] RemoveHubEntry: node is not HubNode");
                return false;
            }
            if (entryIndex < 0 || entryIndex >= hub.Entries.Count) {
                HLogger.Warning($"[NodeCatalogAuthor] RemoveHubEntry: index {entryIndex} out of range");
                return false;
            }

            string removedKey = hub.Entries[entryIndex].Key;

            // 해당 키를 사용하는 엣지 cascade 제거.
            List<(NodeUID, NodeUID)> toRemove = new();
            foreach (BaseNodeEdge edge in catalog.Edges) {
                if (edge is HubNodeEdge he && he.BranchUID == hubUID && he.BranchPortKey == removedKey)
                    toRemove.Add((he.BranchUID, he.LeafUID));
            }
            foreach ((NodeUID b, NodeUID l) in toRemove) DisconnectEdge(catalog, b, l);

            Undo.RecordObject(hub, "Remove Hub Entry");
            hub.RemoveEntry(entryIndex);
            EditorUtility.SetDirty(hub);
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

            Undo.RecordObject(catalog, "Connect Edge");

            TEdge edge = new();
            edge.AssignIdentity(branch, leaf);

            catalog.InternalAddEdge(edge);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            _NotifyMutated(catalog);
            return edge;
        }

        // HubNode 전용 ConnectEdge — portKey 를 HubNodeEdge 에 저장.
        public static HubNodeEdge ConnectHubEdge(NodeCatalogSO catalog, NodeUID branch, NodeUID leaf, string portKey) {
            if (!_ValidateEdgeCreation(catalog, branch, leaf, out string reason)) {
                HLogger.Warning($"[NodeCatalogAuthor] HubEdge 생성 거부: {reason}");
                return null;
            }

            Undo.RecordObject(catalog, "Connect Hub Edge");

            HubNodeEdge edge = new HubNodeEdge();
            edge.AssignIdentity(branch, leaf);
#if UNITY_EDITOR
            edge.SetPortKey(portKey);
#endif
            catalog.InternalAddEdge(edge);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            _NotifyMutated(catalog);
            return edge;
        }

        public static bool DisconnectEdge(NodeCatalogSO catalog, NodeUID branch, NodeUID leaf) {
            if (catalog == null || !catalog.HasEdgeBetween(branch, leaf)) return false;
            Undo.RecordObject(catalog, "Disconnect Edge");
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
            Undo.RecordObject(catalog, "Set Root");
            catalog.InternalSetRoot(uid);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            _NotifyMutated(catalog);
            return true;
        }
        #endregion

        #region Public - Layout (Phase 1-A)
        /// <summary>
        /// 노드 위치를 node.editorPosition 에 반영. SetDirty 만 호출 (SaveAssets 생략, 고빈도 분류).
        /// </summary>
        public static void SetLayout(NodeCatalogSO catalog, NodeUID uid, Vector2 pos) {
            if (catalog == null) {
                HLogger.Error("[NodeCatalogAuthor] catalog is null in SetLayout");
                return;
            }
#if UNITY_EDITOR
            if (!catalog.Nodes.TryGetValue(uid, out BaseNode node)) {
                HLogger.Warning($"[NodeCatalogAuthor] SetLayout rejected: node {uid} not in catalog");
                return;
            }
            node.SetEditorPosition(pos);
            EditorUtility.SetDirty(node);
#endif
        }
        #endregion

        #region Public - Cut / Paste (Phase 1-D 확장)
        // 선택 노드(들)를 JSON 직렬 + catalog 에서 제거. JSON 반환 — caller (HGraphNode 등) 가
        // GUIUtility.systemCopyBuffer 에 저장 책임. mixed 도메인 selection 은 Serialize 가 거부.
        public static string CutNodes(NodeCatalogSO catalog, IReadOnlyList<NodeUID> uids) {
            if (catalog == null) {
                HLogger.Error("[NodeCatalogAuthor] catalog is null in CutNodes");
                return null;
            }
            if (uids == null || uids.Count == 0) {
                HLogger.Warning("[NodeCatalogAuthor] CutNodes rejected: empty uids");
                return null;
            }

            // 직렬화 — 노드 제거 전 데이터 수집.
            List<BaseNode> nodes = new List<BaseNode>(uids.Count);
            for (int k = 0; k < uids.Count; k++) {
                if (catalog.Nodes.TryGetValue(uids[k], out BaseNode node)) nodes.Add(node);
            }
            if (nodes.Count == 0) {
                HLogger.Warning("[NodeCatalogAuthor] CutNodes rejected: no valid nodes for given uids");
                return null;
            }

            string json = HGraphClipboard.Serialize(catalog, nodes);
            if (string.IsNullOrEmpty(json)) {
                HLogger.Warning("[NodeCatalogAuthor] CutNodes rejected: serialization failed (mixed domain types?)");
                return null;
            }

            // 노드 제거 — RemoveNode cascade (엣지 자동 + layout/foldout/openSize 자동 정리).
            int removed = 0;
            for (int k = 0; k < uids.Count; k++) {
                if (RemoveNode(catalog, uids[k])) removed++;
            }

            HLogger.Log($"[NodeCatalogAuthor] CutNodes: serialized {nodes.Count}, removed {removed}");
            return json;
        }

        // JSON 파싱 + 검증 → 같은 UID 로 노드 복원. UID 충돌 / 타입 미스매치 entry 는 skip + Warning.
        // 반환: 복원된 노드 수.
        public static int PasteNodes(NodeCatalogSO catalog, string clipboardJson) {
            if (catalog == null) {
                HLogger.Error("[NodeCatalogAuthor] catalog is null in PasteNodes");
                return 0;
            }
            if (!HGraphClipboard.TryParse(clipboardJson, out HGraphClipboard.Payload payload)) {
                HLogger.Warning("[NodeCatalogAuthor] PasteNodes rejected: invalid clipboard format");
                return 0;
            }

            Undo.RecordObject(catalog, "Paste Nodes");
            Undo.SetCurrentGroupName("Paste Nodes");

            int restored = 0;
            for (int k = 0; k < payload.entries.Length; k++) {
                BaseNode r = _RestoreFromEntry(catalog, payload.entries[k]);
                if (r != null) restored++;
            }

            if (restored > 0) {
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssets();
                _NotifyMutated(catalog);
            }
            return restored;
        }

        static BaseNode _RestoreFromEntry(NodeCatalogSO catalog, HGraphClipboard.Entry entry) {
            Type type = Type.GetType(entry.typeName);
            if (type == null) {
                HLogger.Warning($"[NodeCatalogAuthor] PasteNodes skipped: type '{entry.typeName}' not found");
                return null;
            }
            if (!typeof(BaseNode).IsAssignableFrom(type)) {
                HLogger.Warning($"[NodeCatalogAuthor] PasteNodes skipped: type '{entry.typeName}' not BaseNode");
                return null;
            }

            BaseNode node = ScriptableObject.CreateInstance(type) as BaseNode;
            if (node == null) {
                HLogger.Warning($"[NodeCatalogAuthor] PasteNodes skipped: CreateInstance failed for '{entry.typeName}'");
                return null;
            }
            JsonUtility.FromJsonOverwrite(entry.nodeJson, node);

            // 사용자 결정 (2026-05-08) — Paste 는 항상 새 UID 발급.
            // 두 catalog 가 같은 UID 노드를 보유 가능한 환경 → UID 충돌 회피 + 데이터 무결성 우선.
            // 원본 title 은 보존 (사용자가 의도적으로 변경한 title 존중). 빈 문자열이면 Node_{newUID} fallback.
            NodeUID newUID = NodeUID.New();
            string preservedTitle = node.Title;
            if (string.IsNullOrEmpty(preservedTitle)) preservedTitle = $"Node_{newUID.Value[..8]}";
            node.ResetIdentity();
            node.AssignIdentity(newUID, preservedTitle);

            node.name = preservedTitle;
            // editorPosition / editorFoldoutOpen / editorOpenSize 는 FromJsonOverwrite 가 이미 복원.
            // catalog.InternalSet* 불필요 (Phase 1-F 이관).
            AssetDatabase.AddObjectToAsset(node, catalog);
            Undo.RegisterCreatedObjectUndo(node, "Paste Nodes");
            catalog.InternalAddNode(node);

            return node;
        }
        #endregion

        #region Public - Foldout / OpenSize (Phase 1-B)
        /// <summary>
        /// 노드 Foldout 열림/닫힘 상태를 node.editorFoldoutOpen 에 반영.
        /// "고빈도 상태 업데이트" 분류 - SetDirty 만 (SaveAssets 생략).
        /// </summary>
        public static void SetFoldoutOpen(NodeCatalogSO catalog, NodeUID uid, bool open) {
            if (catalog == null) {
                HLogger.Error("[NodeCatalogAuthor] catalog is null in SetFoldoutOpen");
                return;
            }
#if UNITY_EDITOR
            if (!catalog.Nodes.TryGetValue(uid, out BaseNode node)) {
                HLogger.Warning($"[NodeCatalogAuthor] SetFoldoutOpen rejected: node {uid} not in catalog");
                return;
            }
            node.SetEditorFoldoutOpen(open);
            EditorUtility.SetDirty(node);
#endif
        }

        /// <summary>
        /// 노드 Open 크기를 node.editorOpenSize 에 반영.
        /// Resize Manipulator 의 MouseUp 시점에서만 호출 (드래그 중 매 프레임 호출 X, 고빈도 분류).
        /// </summary>
        public static void SetOpenSize(NodeCatalogSO catalog, NodeUID uid, Vector2 size) {
            if (catalog == null) {
                HLogger.Error("[NodeCatalogAuthor] catalog is null in SetOpenSize");
                return;
            }
#if UNITY_EDITOR
            if (!catalog.Nodes.TryGetValue(uid, out BaseNode node)) {
                HLogger.Warning($"[NodeCatalogAuthor] SetOpenSize rejected: node {uid} not in catalog");
                return;
            }
            node.SetEditorOpenSize(size);
            EditorUtility.SetDirty(node);
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
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.10 Phase 3+ — HubNode API 추가 (CreateHubNode / AddHubEntry / RemoveHubEntry / ConnectHubEdge)
 *
 * # 변경
 * - CreateHubNode(catalog, position): HubNode SO 생성 + sub-asset 등록. 초기 entries 없음.
 * - AddHubEntry(catalog, hubUID, key): hub.AddEntry → SetDirty(hub) → CatalogMutated.
 * - RemoveHubEntry(catalog, hubUID, index): 해당 key 사용 HubNodeEdge cascade 제거 후 hub.RemoveEntry.
 * - ConnectHubEdge(catalog, branch, leaf, portKey): HubNodeEdge 생성 + portKey 할당 + edge 등록.
 *   기존 ConnectEdge<TEdge> 와 별개 — portKey 할당이 필요하므로 분리.
 *
 * # 이유
 * - HubNode 는 포트 항목 수만큼 동적으로 output port 를 시각화.
 *   AddEntry/RemoveEntry 가 CatalogMutated 를 발화해야 repopulate 가 포트 수 갱신.
 * - HubNodeEdge.BranchPortKey 로 repopulate 시 key → Port 역매핑 (암묵 인덱스 불필요).
 *
 * =============================================================================
 * @Jason - PKH 2026.05.10 Phase 3+ — 양방향 CatalogNode 생성 + _CreateCatalogNodeCore 추출
 *
 * # 변경
 * - CreateCatalogNodeAt: 기존 로직 → _CreateCatalogNodeCore 로 위임.
 *   referenced 에 catalog 를 역방향으로 참조하는 CatalogNode 미존재 시 자동 생성 (1:1 양방향).
 * - _CreateCatalogNodeCore: 단일 CatalogNode 생성 코어 (dirty/save/notify 포함).
 * - _HasCatalogNodeFor(catalog, target): catalog 에 target 을 참조하는 CatalogNode 존재 여부 확인.
 *
 * # 이유
 * - 사양: A→B 연결 시 B에 A CatalogNode 자동 생성 (1:1 관계).
 *   동일 catalog 또는 already-exists 체크로 무한루프·중복 방지.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.10 Phase 3 — CreateCatalogNodeAt 추가
 *
 * # 변경
 * - CreateCatalogNodeAt(catalog, referenced, dropPosition): CatalogNode 생성 전용 진입점.
 *   CreateNode<T> 와 달리 참조 카탈로그 설정 + 드롭 위치를 단일 Undo 그룹으로 처리.
 *   HGraphWindow 드래그드롭 (카탈로그 이미 바인드 상태) 에서 호출.
 *
 * # 이유
 * - CreateNode<CatalogNode> 후 SetReferencedCatalog 하면 CatalogMutated가 두 번 발화해
 *   repopulate 가 두 번 일어남 (위치 flicker 유발). 단일 메서드로 원자 처리.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.09 Phase 1-F — 에디터 상태 이관 (catalog HDictionary → BaseNode)
 *
 * # 변경
 * - SetLayout / SetFoldoutOpen / SetOpenSize: catalog.InternalSet* → node.Set* + SetDirty(node)
 * - CreateNode 자동 배치: catalog.InternalSetLayout → node.SetEditorPosition 직접 호출
 * - DuplicateNode 위치 읽기: catalog.EditorNodeLayouts[sourceUID] → sourceTyped.EditorPosition
 * - RemoveNode cascade: InternalRemoveLayout/FoldoutOpen/OpenSize 3줄 제거 (node 소멸로 자동 처리)
 * - _RestoreFromEntry: catalog.InternalSet* 3줄 제거 — FromJsonOverwrite 가 nodeJson 에서 자동 복원
 *
 * # 이유
 * - 삭제 Undo 후 노드 위치 (0,0) 리셋 버그: catalog HDictionary Undo 복원이
 *   DisconnectEdge cascade 의 AssetDatabase.SaveAssets 호출로 손상됨.
 * - 이관 후 Undo.DestroyObjectImmediate(node) 가 editorPosition/FoldoutOpen/OpenSize
 *   포함 노드 전체 상태를 원자 복원 → catalog 단계 Undo 불필요.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.09 Phase 1-F — Undo 레이어 추가
 *
 * # 변경
 * - CreateNode: Undo.RecordObject(catalog) + Undo.RegisterCreatedObjectUndo(node)
 * - DuplicateNode: 동일 패턴
 * - RemoveNode: Undo.RecordObject(catalog) + Undo.DestroyObjectImmediate(node)
 *   (AssetDatabase.RemoveObjectFromAsset + DestroyImmediate 대체)
 * - ConnectEdge / DisconnectEdge / SetRoot: Undo.RecordObject(catalog)
 * - PasteNodes: Undo.RecordObject(catalog) + SetCurrentGroupName("Paste Nodes")
 * - _RestoreFromEntry: Undo.RegisterCreatedObjectUndo(node, "Paste Nodes")
 * - 고빈도 (SetLayout / SetFoldoutOpen / SetOpenSize): Undo 미적용 유지 (고빈도 히스토리 오염 방지)
 *
 * =============================================================================
 * @Jason - PKH 2026.05.09 NodeUID.New() 전환 — NodeUIDRegistry 의존 제거
 *
 * # 변경
 * - CreateNode / DuplicateNode / _RestoreFromEntry 의 NodeUIDRegistry.instance.Issue() → NodeUID.New()
 * - 자동 타이틀 fallback: $"Node_{uid.Value}" → $"Node_{uid.Value[..8]}" (GUID 8자 단축)
 * - using HWindows.Editor.NodeWindow.Identity 제거 (Registry 삭제로 불필요)
 *
 * =============================================================================
 */
// =============================================================================
// (이하 이전 엔트리 — 원래 형식 보존)
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
//
//   [Phase 1-D 확장 - 2026-05-07]
//   - DuplicateNode<T> 신설 (P1D-c):
//   + ScriptableObject.Instantiate (Object.Instantiate) 로 도메인 데이터 자동 복사.
//   + ResetIdentity + AssignIdentity 새 UID 부여로 identity 만 갱신, 도메인 보존.
//   + 위치 = 원본 + (40, 40) 자동 layout. 엣지 연결은 복제 X (사용자가 새로 연결).
//   + Phase 0 의 5 mutation 패턴 정합 (SetDirty + SaveAssets + _NotifyMutated 트리오).
//   + 잘못된 source UID / 타입 미스매치는 진입 단계에서 Warning + null 반환.
//
//   [Phase 1-D Cut/Paste 확장 - 2026-05-08]
//   - CutNodes(catalog, uids) → string JSON:
//   + HGraphClipboard.Serialize 로 직렬 (도메인 magic 일관성 검사 포함, mixed 거부).
//   + 직렬 성공 후 RemoveNode cascade 로 노드 + 엣지 + 보조 맵 일괄 제거.
//   + JSON 반환 — caller (HGraphNode 등) 가 GUIUtility.systemCopyBuffer 에 저장 책임.
//   - PasteNodes(catalog, clipboardJson) → int restoredCount:
//   + HGraphClipboard.TryParse 로 magic prefix/suffix + version + entries 검증.
//   + entry 별 _RestoreFromEntry — Type.GetType + ScriptableObject.CreateInstance + JsonUtility.FromJsonOverwrite.
//   + 도메인 호환 검증: typeof(BaseNode).IsAssignableFrom(type) — 잘못된 타입 entry skip.
//   + 복원된 entry > 0 일 때만 SetDirty + SaveAssets + _NotifyMutated (배치 처리).
//   + Phase 1-A 인접 보조 맵 (layout / foldoutOpen / openSize) 도 entry 의 값으로 함께 복원.
//   - UID 처리 정책 (사용자 결정 2026-05-08, 이전 결정 폐기):
//   + 이전: "UID 보존 — 충돌 시 거부" (Q3-A) → 두 catalog 가 같은 UID 노드 보유 가능한 환경에서
//     Paste 거부 발생, 사용자 의도 (catalog 간 데이터 이전) 달성 못함.
//   + 변경: "항상 새 UID 발급" — NodeUIDRegistry.instance.Issue() 호출 후 ResetIdentity + AssignIdentity.
//     UID 충돌 회피 + 데이터 무결성 우선. Cut 의 "이동" 의미는 데이터 복사로 재정의.
//   + title 은 entry 의 원본 보존 (사용자 의도 존중). 빈 문자열 fallback = Node_{newUID}.
//   + Cut 으로 사라진 원본 UID 는 NodeUIDRegistry 단조 증가 룰로 영원히 unique 한 객체 식별자 유지.
//
//   [Phase 1-B 확장 - 2026-05-07]
//   - SetFoldoutOpen / SetOpenSize 신설. SetLayout 과 같은 "고빈도 상태 업데이트" 분류.
//   + 둘 다 SetDirty 만 호출, SaveAssets 미호출 (P1B-i / P1-g 정합).
//   + null catalog → Error, 존재하지 않는 노드 UID → Warning. 무효 입력은 진입 단계에서 거부.
//   - RemoveNode cascade: InternalRemoveFoldoutOpen + InternalRemoveOpenSize 두 줄 추가 (P1B-6).
//   + 노드 삭제 시 layout / foldout / openSize 3개 보조 맵에서 자동 제거. orphan 방지.
//   + "별도 맵 패턴" 의 cascade 비용 - 보조 맵이 추가될 때마다 한 줄씩 가산. 의미 영역 분리의 가시 비용.
//   - CreateNode 자동 초기화 미적용 (spec §4) - read-side TryGetValue fallback 으로 충분.
//     자동 초기화는 직렬화 부피만 늘림 (HDictionary entries 증가).
// =============================================================================
#endif

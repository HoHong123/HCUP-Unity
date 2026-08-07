#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 카탈로그 mutation 단일 게이트 (Editor-only static class).
 *
 * 특징 ::
 * 상태 0, 필드 0. 모든 컨텍스트는 파라미터로 전달.
 * + CreateNode / DuplicateNode / RemoveNode / ConnectEdge / DisconnectEdge / SetRoot
 * + Cut / Paste (JSON 클립보드 기반)
 * + SetLayout / SetFoldoutOpen (고빈도 상태)
 * + PurgeNullNodes (sub-asset 외부 삭제 고스트 UID 자동 정리)
 *
 * 주의사항 ::
 * 모든 mutation 후 SetDirty + SaveAssets 트리오 필수.
 * + SetLayout / SetFoldoutOpen 는 SaveAssets 미호출 (고빈도 분류).
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

        // 위치 인식 오버로드 — 우클릭 컨텍스트 메뉴 등 명시 좌표가 있을 때 사용.
        // 자동 배치(auto-layout) 대신 전달된 position 을 node.EditorPosition 에 직접 설정.
        public static T CreateNode<T>(NodeCatalogSO catalog, Vector2 position, string title = null) where T : BaseNode {
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
            node.SetEditorPosition(position);
#endif

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            _NotifyMutated(catalog);
            return node;
        }

        // Phase 3 — NodeCatalogSO 를 canvas 특정 위치에 CatalogNode 로 생성.
        // HGraphWindow 드래그드롭 (카탈로그 이미 바인드 상태) 전용 진입점.
        // 1:1 양방향: referenced 에도 catalog 를 참조하는 CatalogNode 자동 생성 (미존재 시).
        // 단일 제한: 같은 catalog 안에 동일 referenced 를 가리키는 CatalogNode 는 최대 1개.
        public static CatalogNode CreateCatalogNodeAt(NodeCatalogSO catalog, NodeCatalogSO referenced, Vector2 dropPosition) {
            if (catalog == null) {
                HLogger.Error("[NodeCatalogAuthor] catalog is null in CreateCatalogNodeAt");
                return null;
            }

            // 중복 거부: catalog 에 referenced 를 가리키는 CatalogNode 가 이미 존재하면 null 반환.
            if (referenced != null && _HasCatalogNodeFor(catalog, referenced)) {
                HLogger.Warning($"[NodeCatalogAuthor] CreateCatalogNodeAt rejected: '{catalog.name}' already contains a CatalogNode for '{referenced.name}'");
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

            // CatalogNode는 루트 후보 제외.

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            _NotifyMutated(catalog);
            return node;
        }

        // catalog 안에 target 을 참조하는 CatalogNode 가 이미 있는지 확인.
        // 순방향 중복 거부 + 역방향 자동 생성 가드 양쪽에서 사용.
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
            if (catalog.Nodes[uid] is CatalogNode) {
                HLogger.Warning("[NodeCatalogAuthor] SetRoot rejected: CatalogNode cannot be the root node.");
                return false;
            }
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

        #endregion

        #region Public - Repair
        // catalog.Nodes 에서 value 가 null(sub-asset 외부 삭제 고스트 UID)인 항목을 일괄 제거.
        // 연결된 엣지 + RootUID 이전 처리 포함. SetDirty + SaveAssets 호출.
        // _NotifyMutated 는 미호출 — 호출자(HGraphCanvas._PopulateInternal)가 이미 populate 중이므로
        // SaveAssets 가 ObjectChangeWatcher 를 통해 다음 repopulate 를 자동으로 예약함.
        // 반환: 제거한 항목 수. 0 이면 catalog 변경 없음.
        public static int PurgeNullNodes(NodeCatalogSO catalog) {
            if (catalog == null) return 0;

            List<NodeUID> nullUIDs = null;
            foreach (KeyValuePair<NodeUID, BaseNode> pair in catalog.Nodes) {
                if (pair.Value == null) {
                    if (nullUIDs == null) nullUIDs = new List<NodeUID>();
                    nullUIDs.Add(pair.Key);
                }
            }

            if (nullUIDs == null) return 0;

            HLogger.Warning($"[NodeCatalogAuthor] PurgeNullNodes: {nullUIDs.Count} ghost UID(s) detected. Purging.");

            bool rootAffected = false;
            foreach (NodeUID uid in nullUIDs) {
                List<(NodeUID, NodeUID)> touching = null;
                foreach (BaseNodeEdge e in catalog.Edges) {
                    if (e == null) continue;
                    if (e.BranchUID == uid || e.LeafUID == uid) {
                        if (touching == null) touching = new List<(NodeUID, NodeUID)>();
                        touching.Add((e.BranchUID, e.LeafUID));
                    }
                }
                if (touching != null) {
                    foreach ((NodeUID b, NodeUID l) in touching) catalog.InternalRemoveEdge(b, l);
                }

                if (catalog.RootUID == uid) rootAffected = true;
                catalog.InternalRemoveNode(uid);
            }

            if (rootAffected) {
                // 모든 null 항목 제거 후 첫 번째 유효 노드를 Root 로 이전.
                NodeUID fallback = _FindAnyOtherNode(catalog, NodeUID.None);
                if (fallback.IsValid) catalog.InternalSetRoot(fallback);
                else catalog.InternalClearRoot();
            }

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return nullUIDs.Count;
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
            if (!catalog.Nodes.TryGetValue(branch, out BaseNode branchNode) || branchNode == null) {
                reason = $"branch node {branch} not in catalog";
                return false;
            }
            if (!catalog.Nodes.TryGetValue(leaf, out BaseNode leafNode) || leafNode == null) {
                reason = $"leaf node {leaf} not in catalog";
                return false;
            }
            if (catalog.HasEdgeBetween(branch, leaf)) {
                reason = $"parallel edge forbidden: {branch} → {leaf} already exists";
                return false;
            }
            return true;
        }

        // 루트 자동 이전용 후보 탐색. CatalogNode는 SetRoot 타입 가드로 루트 지정이 금지된
        // 노드라, 여기서 걸러내지 않으면 RemoveNode/PurgeNullNodes가 InternalSetRoot를
        // 직접 호출해 그 가드를 우회하게 된다 (LOG-20260511-3 참고).
        static NodeUID _FindAnyOtherNode(NodeCatalogSO catalog, NodeUID exclude) {
            foreach (var pair in catalog.Nodes) {
                if (pair.Key == exclude) continue;
                if (pair.Value is CatalogNode) continue;
                return pair.Key;
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
 * [LOG-20260807-1] _FindAnyOtherNode 루트 이전 후보에서 CatalogNode 제외
 * [LOG-20260512-1] PurgeNullNodes + _ValidateEdgeCreation null 가드
 * [LOG-20260511-3] CatalogNode 루트 설정 제약 (SetRoot 타입 가드 + 자동 루트 스킵)
 * → 전체 이력: docs/history/HWindows/Editor/NodeWindow/NodeCatalog/Authoring/NodeCatalogAuthor.md
 * =============================================================================
 */
#endif

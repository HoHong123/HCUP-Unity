#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- GraphView 기반 노드 캔버스 (에디터 전용).
 *
 * 특징 ::
 * Bind(catalog) / Unbind() 로 catalog 주입. 하이브리드 Populate 전략.
 * + nodeLookup: NodeUID → HGraphNode 역매핑. 선택/하이라이트/Floating GUI 경로 활용.
 * + edgeLookup: (Branch, Leaf) → HGraphEdge 역매핑. 엣지 시각 관리.
 * + graphViewChanged 훅으로 드래그 이동 + 엣지 생성 처리.
 *
 * 주의사항 ::
 * StretchToParentSize() 제거됨 — HGraphWindow 가 flexGrow=1 로 영역 할당.
 * + BuildContextualMenu override 필수 — GraphView 기본 메뉴 항목 차단.
 * + edgesToCreate.Clear() 필수 — ConnectEdge 동기 repopulate 로 HGraphEdge 이미 추가됨.
 * =========================================================
 */
#endif
using System;
using System.Collections.Generic;
using HDiagnosis.Logger;
using HWindows.Editor.NodeWindow.Authoring;
using HWindows.Editor.NodeWindow.Settings;
using HWindows.NodeWindow;
using HWindows.NodeWindow.Identity;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace HWindows.Editor.NodeWindow {
    public sealed class HGraphCanvas : GraphView {
        #region Const
        const string USS_ASSET_NAME = "HGraphWindow";
        #endregion

        #region Fields
        NodeCatalogSO currentCatalog;
        VisualElement emptyStateHint;
        readonly Dictionary<NodeUID, HGraphNode> nodeLookup = new();
        readonly Dictionary<(NodeUID Branch, NodeUID Leaf), HGraphEdge> edgeLookup = new();
        int lastCatalogHash;
        GridBackground gridBackground;  // Phase 1-E P1E-ε: field 로 끌어올림 (ShowGrid 동기화용)
        #endregion

        #region Constructor + Lifecycle
        public HGraphCanvas() {
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            gridBackground = new GridBackground();
            Insert(0, gridBackground);
            gridBackground.StretchToParentSize();
            gridBackground.visible = NodeSnapSettings.instance.ShowGrid;

            _LoadStyleSheet();

            _BuildEmptyStateHint();
            graphViewChanged = _OnGraphViewChanged;

            // catalog mutation 이벤트 구독 (Author 호출 + Inspector 직접 수정 watcher 모두 통합).
            // VisualElement detach 시 unsubscribe 로 메모리 누수 방지.
            NodeCatalogAuthor.CatalogMutated += _OnCatalogMutated;

            // hash polling fallback: ObjectChangeEvents 가 즉시 발송 안 되는 케이스 대응.
            // 매 frame hash 비교 후 변경 시에만 Repopulate. 계산 비용 미미 (노드 N=100 ≈ 3μs).
            EditorApplication.update += _PollCatalogChanges;

            // Phase 1-D Cut/Paste 단축키 (사용자 결정 2026-05-08).
            // Ctrl+C / Ctrl+X / Ctrl+V (Mac 은 Cmd) — actionKey 가 platform 추상화.
            RegisterCallback<KeyDownEvent>(_OnKeyDown);

            // Phase 1-E P1E-ε: ShowGrid 변경 시 GridBackground.visible 동기화.
            NodeWindowSettingsProvider.SnapSettingsChanged += _OnSnapSettingsChanged;

            // Phase 1-F: Undo/Redo 수행 후 canvas 상태 재동기화.
            Undo.undoRedoPerformed += _OnUndoRedo;

            RegisterCallback<DetachFromPanelEvent>(_ => {
                NodeCatalogAuthor.CatalogMutated -= _OnCatalogMutated;
                EditorApplication.update -= _PollCatalogChanges;
                NodeWindowSettingsProvider.SnapSettingsChanged -= _OnSnapSettingsChanged;
                Undo.undoRedoPerformed -= _OnUndoRedo;
            });
        }

        private void _PollCatalogChanges() {
            if (currentCatalog == null) return;
            int currentHash = _CalculateCatalogHash();
            if (currentHash != lastCatalogHash) {
                lastCatalogHash = currentHash;
                _RepopulateNoViewportReset();
            }
        }

        private int _CalculateCatalogHash() {
            if (currentCatalog == null) return 0;
            int hash = 17;
            hash = hash * 31 + currentCatalog.NodeCount;
            hash = hash * 31 + currentCatalog.EdgeCount;   // Phase 2 — 엣지 변경 감지
            hash = hash * 31 + currentCatalog.RootUID.GetHashCode();
            foreach (KeyValuePair<NodeUID, BaseNode> pair in currentCatalog.Nodes) {
                hash = hash * 31 + pair.Key.GetHashCode();
                if (pair.Value != null) {
                    hash = hash * 31 + (pair.Value.Title ?? string.Empty).GetHashCode();
                }
            }
            return hash;
        }

        private void _OnCatalogMutated(NodeCatalogSO catalog) {
            if (currentCatalog == null || catalog != currentCatalog) return;
            // viewport 위치는 사용자가 조정해 둔 상태일 수 있으므로 _Populate 가
            // 강제 리셋하지 않도록 별도 경로. 현재 구현은 _Populate 가 viewport 리셋을 포함하므로
            // 빈번한 mutation 에서 깜빡일 가능성 있음. 필요 시 viewport 보존 분기 추가.
            _RepopulateNoViewportReset();
        }

        public Vector2 GetViewportCenterWorld() {
            Vector3 vt = viewTransform.position;
            float scale = viewTransform.scale.x;
            if (scale == 0f) scale = 1f;
            Rect rect = contentRect;
            Vector2 screenCenter = new Vector2(rect.width / 2f, rect.height / 2f);
            return (screenCenter - new Vector2(vt.x, vt.y)) / scale;
        }
        #endregion

        #region Public - Catalog Switch (Phase 3)
        // HGraphCatalogNode 더블클릭 → HGraphWindow._BindCatalog 경로 연결용 이벤트.
        // HGraphWindow 가 CreateGUI 시 구독, 카탈로그 전환 진입점 역할.
        public event Action<NodeCatalogSO> CatalogSwitchRequested;

        // HGraphCatalogNode.OnHeaderDoubleClick 이 호출. 이벤트 발화만 담당.
        internal void RequestCatalogSwitch(NodeCatalogSO catalog) {
            CatalogSwitchRequested?.Invoke(catalog);
        }

        // canvas 로컬 좌표(panel-space) → 그래프 월드 좌표 변환.
        // HGraphWindow 드래그드롭 드롭 위치 계산에 사용.
        public Vector2 ToGraphPosition(Vector2 canvasLocalPos) {
            return this.ChangeCoordinatesTo(contentViewContainer, canvasLocalPos);
        }
        #endregion

        #region Public - Port Compatibility (Phase 2)
        // 출력 포트 → 입력 포트 (방향 반대) + 다른 노드 조합만 허용.
        // self-loop / 같은 방향 포트 연결 차단. 타입 제약 없음 (모든 bool 포트 호환).
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter) {
            List<Port> result = new();
            foreach (Port port in ports.ToList()) {
                if (port.node == startPort.node) continue;
                if (port.direction == startPort.direction) continue;
                result.Add(port);
            }
            return result;
        }
        #endregion

        #region Public - Context Menu (Phase 1-D)
        // GraphView 기본 selection 메뉴 (Cut/Copy/Paste/Duplicate/Delete) 자동 추가 차단.
        // ContextualMenu 는 leaf (HGraphNode) → parent (HGraphCanvas) 양쪽 BuildContextualMenu
        // 모두 호출되어 evt.menu 에 누적되므로, GraphElement.capabilities 차단만으로는 부족.
        // base 호출 생략으로 GraphView 측 자동 추가 차단.
        // 빈 캔버스 우클릭 시 Paste 메뉴 (Phase 1-D Cut/Paste 확장) — 노드 위 우클릭은 HGraphNode 가 처리.
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt) {
            if (currentCatalog == null) return;

            // 노드/엣지 위 우클릭 시 해당 요소의 BuildContextualMenu 가 항목 추가 — 중복 회피.
            VisualElement target = evt.target as VisualElement;
            while (target != null && target != this) {
                if (target is HGraphNode) return;
                if (target is HGraphEdge) return;  // Phase 2 — 엣지 우클릭 시 캔버스 메뉴 차단
                target = target.parent;
            }

            // 빈 캔버스 우클릭 — 노드 생성 / Paste / 모두 선택.
            evt.menu.AppendAction("허브 노드 생성 (Create Hub Node)",
                action => {
                    Vector2 pos = ToGraphPosition(action.eventInfo.localMousePosition);
                    NodeCatalogAuthor.CreateHubNode(currentCatalog, pos);
                },
                DropdownMenuAction.Status.Normal);

            evt.menu.AppendSeparator();

            DropdownMenuAction.Status pasteStatus = HGraphClipboard.IsValid(GUIUtility.systemCopyBuffer)
                ? DropdownMenuAction.Status.Normal
                : DropdownMenuAction.Status.Disabled;
            evt.menu.AppendAction("붙여넣기 (Paste)",
                action => _OnContextPaste(),
                pasteStatus);

            // Phase 1-E P1E-8: 노드가 하나라도 있을 때만 활성. HGraphNode 위 우클릭은 노드 메뉴 (6 항목) 유지.
            bool hasAnyNode = false;
            foreach (GraphElement elem in graphElements) {
                if (elem is HGraphNode) { hasAnyNode = true; break; }
            }
            DropdownMenuAction.Status selectAllStatus = hasAnyNode
                ? DropdownMenuAction.Status.Normal
                : DropdownMenuAction.Status.Disabled;
            evt.menu.AppendAction("모두 선택 (Select All)",
                action => SelectAllNodes(),
                selectAllStatus);
        }

        private void _OnContextPaste() {
            PasteFromClipboard();
        }
        #endregion

        #region Clipboard Actions (Phase 1-D 단축키 + 메뉴 helper)
        // 단축키와 우클릭 메뉴 핸들러가 공유하는 진입점. catalog null 검사 + 직렬 + 클립보드 입출력.

        // 노드 리스트 → JSON 직렬 → systemCopyBuffer (원본 유지). mixed 도메인 시 false.
        internal bool CopyNodes(IReadOnlyList<HGraphNode> nodes) {
            if (currentCatalog == null) return false;
            if (nodes == null || nodes.Count == 0) return false;

            List<BaseNode> dataNodes = new List<BaseNode>(nodes.Count);
            for (int k = 0; k < nodes.Count; k++) dataNodes.Add(nodes[k].DataNode);
            string json = HGraphClipboard.Serialize(currentCatalog, dataNodes);
            if (string.IsNullOrEmpty(json)) {
                HLogger.Warning("[HGraphCanvas] Copy failed (mixed domain types in selection?)");
                return false;
            }
            GUIUtility.systemCopyBuffer = json;
            HLogger.Log($"[HGraphCanvas] Copied {nodes.Count} node(s) JSON ({json.Length} chars)");
            return true;
        }

        // 노드 리스트 → JSON 직렬 + catalog 제거 → systemCopyBuffer. Author.CutNodes 가 mixed 거부.
        internal bool CutNodes(IReadOnlyList<HGraphNode> nodes) {
            if (currentCatalog == null) return false;
            if (nodes == null || nodes.Count == 0) return false;

            List<NodeUID> uids = new List<NodeUID>(nodes.Count);
            for (int k = 0; k < nodes.Count; k++) uids.Add(nodes[k].UID);
            string json = NodeCatalogAuthor.CutNodes(currentCatalog, uids);
            if (string.IsNullOrEmpty(json)) return false;
            GUIUtility.systemCopyBuffer = json;
            HLogger.Log($"[HGraphCanvas] Cut {uids.Count} node(s) to clipboard");
            return true;
        }

        // systemCopyBuffer → JSON 파싱 → catalog 에 복원 (새 UID 발급).
        internal int PasteFromClipboard() {
            if (currentCatalog == null) return 0;
            int count = NodeCatalogAuthor.PasteNodes(currentCatalog, GUIUtility.systemCopyBuffer);
            if (count == 0) HLogger.Warning("[HGraphCanvas] Paste: 0 nodes restored");
            else HLogger.Log($"[HGraphCanvas] Pasted {count} node(s)");
            return count;
        }

        // 노드 리스트 → 각각 Author.DuplicateNode (새 UID + 위치 offset). 반환: 복제 성공 개수.
        internal int DuplicateNodes(IReadOnlyList<HGraphNode> nodes) {
            if (currentCatalog == null) return 0;
            if (nodes == null || nodes.Count == 0) return 0;

            int duplicated = 0;
            for (int k = 0; k < nodes.Count; k++) {
                if (NodeCatalogAuthor.DuplicateNode<BaseNode>(currentCatalog, nodes[k].UID) != null) duplicated++;
            }
            if (duplicated == 0) HLogger.Warning("[HGraphCanvas] Duplicate: 0 nodes duplicated");
            else HLogger.Log($"[HGraphCanvas] Duplicated {duplicated} node(s)");
            return duplicated;
        }

        // 노드 리스트 → 각각 Author.RemoveNode (cascade 자동). 반환: 삭제 성공 개수.
        internal int DeleteNodes(IReadOnlyList<HGraphNode> nodes) {
            if (currentCatalog == null) return 0;
            if (nodes == null || nodes.Count == 0) return 0;

            int deleted = 0;
            for (int k = 0; k < nodes.Count; k++) {
                if (NodeCatalogAuthor.RemoveNode(currentCatalog, nodes[k].UID)) deleted++;
            }
            if (deleted == 0) HLogger.Warning("[HGraphCanvas] Delete: 0 nodes deleted");
            else HLogger.Log($"[HGraphCanvas] Deleted {deleted} node(s)");
            return deleted;
        }

        // Phase 2 — selection 에서 HGraphEdge 만 추려 DisconnectEdge 일괄 호출.
        // HGraphEdge.Capabilities.Deletable 비활성이므로 graphViewChanged 경로 없음 — 직접 처리.
        private void _DeleteSelectedEdges() {
            if (currentCatalog == null) return;
            List<HGraphEdge> toDelete = new();
            foreach (ISelectable s in selection) {
                if (s is HGraphEdge e) toDelete.Add(e);
            }
            foreach (HGraphEdge e in toDelete) {
                NodeCatalogAuthor.DisconnectEdge(currentCatalog, e.BranchUID, e.LeafUID);
            }
        }
        #endregion

        #region Keyboard Shortcuts (Phase 1-D)
        // Ctrl+C / Ctrl+X / Ctrl+V / Ctrl+D (Win/Linux) + Cmd 변형 (Mac) + Delete 단독.
        // actionKey = platform 추상화 (Mac Cmd, 그 외 Ctrl). capabilities 차단과 별개 path.
        // Delete 는 modifier 없는 단독 키 — actionKey 분기 외부 처리.
        private void _OnKeyDown(KeyDownEvent evt) {
            if (currentCatalog == null) return;

            if (evt.actionKey) {
                switch (evt.keyCode) {
                    case KeyCode.C:
                        CopyNodes(GetSelectedNodes());
                        evt.StopPropagation();
                        return;
                    case KeyCode.X:
                        CutNodes(GetSelectedNodes());
                        evt.StopPropagation();
                        return;
                    case KeyCode.V:
                        PasteFromClipboard();
                        evt.StopPropagation();
                        return;
                    case KeyCode.D:
                        DuplicateNodes(GetSelectedNodes());
                        evt.StopPropagation();
                        return;
                    case KeyCode.A:
                        SelectAllNodes();
                        evt.StopPropagation();
                        return;
                    case KeyCode.Z:
                        // Phase 1-F: GraphView 포커스 시 Unity 전역 단축키가 UIElements 에 막힘.
                        // Ctrl+Z = Undo, Ctrl+Shift+Z = Redo (Mac Cmd 동일). Windows Ctrl+Y 는 아래 별도.
                        if (evt.shiftKey) Undo.PerformRedo();
                        else Undo.PerformUndo();
                        evt.StopPropagation();
                        return;
                    case KeyCode.Y:
                        // Ctrl+Y = Redo (Windows 관용).
                        Undo.PerformRedo();
                        evt.StopPropagation();
                        return;
                }
                return;
            }

            // modifier 없는 단독 키.
            if (evt.keyCode == KeyCode.Delete) {
                DeleteNodes(GetSelectedNodes());
                // Phase 2: 선택된 엣지도 함께 삭제. HGraphEdge.Capabilities.Deletable 비활성이므로
                // graphViewChanged.elementsToRemove 경로가 없음 — 여기서 직접 처리.
                _DeleteSelectedEdges();
                evt.StopPropagation();
            }
        }
        #endregion

        #region Multi-Select (Phase 1-E / 1-F)
        // 화면에 그려진 모든 HGraphNode 선택. graphElements 순회 (Q7 B 채택 — 시각 진실성).
        // Phase 3 DepthTree 진입 시 "활성 layer 만" 의미 자동 정합 (코드 변경 0).
        internal int SelectAllNodes() {
            ClearSelection();
            int count = 0;
            foreach (GraphElement elem in graphElements) {
                if (elem is HGraphNode node) {
                    AddToSelection(node);
                    count++;
                }
            }
            return count;
        }

        // Phase 1-F: 화면에 그려진 모든 HGraphNode 의 foldout 을 닫음. HGraphWindow 툴바 "Close All" 버튼 진입점.
        // 이미 닫힌 노드는 CloseIfExpanded 가 early return — 불필요한 FoldoutChanged 발화 없음.
        internal int CloseAllFoldouts() {
            int count = 0;
            foreach (GraphElement elem in graphElements) {
                if (elem is HGraphNode node && node.IsExpanded) {
                    node.CloseIfExpanded();
                    count++;
                }
            }
            return count;
        }
        #endregion

        #region Settings Sync (Phase 1-E P1E-ε)
        void _OnSnapSettingsChanged() {
            if (gridBackground != null) {
                gridBackground.visible = NodeSnapSettings.instance.ShowGrid;
            }
            MarkDirtyRepaint();
        }
        #endregion

        #region Undo / Redo (Phase 1-F)
        // Undo.undoRedoPerformed 콜백 — catalog 직렬화 상태가 외부에서 변경됐으므로 강제 repopulate.
        // NotifyExternalMutation → CatalogMutated → _OnCatalogMutated → _RepopulateNoViewportReset.
        private void _OnUndoRedo() {
            if (currentCatalog == null) return;
            NodeCatalogAuthor.NotifyExternalMutation(currentCatalog);
        }
        #endregion

        #region Navigation (Phase 1-C)
        // 지정 graph 좌표를 viewport 중앙으로 pan 이동. zoom (viewTransform.scale) 은 보존.
        // GetViewportCenterWorld 의 역수 : graphPos = (screenPos - position) / scale 을
        // position = screenPos - graphPos * scale 로 풀어 새 pan 계산.
        public void CenterViewportOn(Vector2 worldPos) {
            Rect rect = contentRect;
            Vector2 screenCenter = new Vector2(rect.width / 2f, rect.height / 2f);
            float scale = viewTransform.scale.x;
            if (scale == 0f) scale = 1f;
            Vector3 newPos = new Vector3(
                screenCenter.x - worldPos.x * scale,
                screenCenter.y - worldPos.y * scale,
                0f);
            UpdateViewTransform(newPos, viewTransform.scale);
        }

        // 현재 catalog 의 RootUID layout 으로 viewport 이동. 루트 미보유 시 false.
        public bool GoToRoot() {
            if (currentCatalog == null || !currentCatalog.HasRoot) return false;
            NodeUID root = currentCatalog.RootUID;
            Vector2 pos = Vector2.zero;
#if UNITY_EDITOR
            if (currentCatalog.Nodes.TryGetValue(root, out BaseNode rootNode) && rootNode != null)
                pos = rootNode.EditorPosition;
#endif
            CenterViewportOn(pos);
            return true;
        }

        // selection 에서 HGraphNode 만 추려 반환. Window 측이 ISelectable (Experimental
        // .GraphView 타입) 을 직접 다루지 않도록 어댑터 경계 (P1-3) 를 보존하는 헬퍼.
        public IReadOnlyList<HGraphNode> GetSelectedNodes() {
            List<HGraphNode> result = new();
            foreach (ISelectable s in selection) {
                if (s is HGraphNode n) result.Add(n);
            }
            return result;
        }

        // Phase 2 — UID 로 노드 VisualElement 를 찾아 시점 중앙으로 이동.
        // HGraphEdge 우클릭 메뉴에서 브랜치 / 리프 노드 시점 이동 진입점.
        internal void CenterOnNode(NodeUID uid) {
            if (!nodeLookup.TryGetValue(uid, out HGraphNode node)) return;
            CenterViewportOn(node.GetPosition().position);
        }

        // Phase 2 — 엣지 연결 시 브랜치 + 리프 노드에 하이라이트 CSS 토글.
        // HGraphEdge.OnSelected / OnUnselected 에서 호출.
        internal void HighlightNodesByEdge(NodeUID branch, NodeUID leaf, bool on) {
            const string CSS = "hgraph-node--edge-highlight";
            if (nodeLookup.TryGetValue(branch, out HGraphNode b)) {
                if (on) b.AddToClassList(CSS);
                else b.RemoveFromClassList(CSS);
            }
            if (nodeLookup.TryGetValue(leaf, out HGraphNode l)) {
                if (on) l.AddToClassList(CSS);
                else l.RemoveFromClassList(CSS);
            }
        }
        #endregion

        #region USS
        private void _LoadStyleSheet() {
            string[] guids = AssetDatabase.FindAssets($"t:StyleSheet {USS_ASSET_NAME}");
            if (guids.Length == 0) {
                HLogger.Warning(
                    $"[HWindows.NodeWindow] StyleSheet '{USS_ASSET_NAME}' not found in project. " +
                    "Grid/style will fall back to GraphView defaults. " +
                    "Verify USS file presence or asset name.");
                return;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            StyleSheet sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            if (sheet == null) {
                HLogger.Warning(
                    $"[HWindows.NodeWindow] StyleSheet at '{path}' failed to load. " +
                    "Grid/style will fall back to GraphView defaults.");
                return;
            }

            styleSheets.Add(sheet);
        }
        #endregion

        #region Empty State
        private void _BuildEmptyStateHint() {
            emptyStateHint = new VisualElement();
            emptyStateHint.style.position = Position.Absolute;
            emptyStateHint.style.left = 0;
            emptyStateHint.style.right = 0;
            emptyStateHint.style.top = 0;
            emptyStateHint.style.bottom = 0;
            emptyStateHint.style.alignItems = Align.Center;
            emptyStateHint.style.justifyContent = Justify.Center;
            emptyStateHint.pickingMode = PickingMode.Ignore;

            Label hintLabel = new Label(
                "No Node Catalog bound.\n\nDrop a Node Catalog here,\nor use the toolbar catalog field.");
            hintLabel.style.color = new StyleColor(new Color(0.6f, 0.6f, 0.6f));
            hintLabel.style.fontSize = 14;
            hintLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            hintLabel.style.whiteSpace = WhiteSpace.Normal;
            emptyStateHint.Add(hintLabel);

            Add(emptyStateHint);
        }

        private void _ShowEmptyStateHint() {
            emptyStateHint.style.display = DisplayStyle.Flex;
        }

        private void _HideEmptyStateHint() {
            emptyStateHint.style.display = DisplayStyle.None;
        }
        #endregion

        #region Bind
        public void Bind(NodeCatalogSO catalog) {
            if (currentCatalog == catalog) return;
            currentCatalog = catalog;
            _Populate();
        }

        public void Unbind() {
            Bind(null);
        }
        #endregion

        #region Populate
        private void _Populate() {
            _PopulateInternal();
            lastCatalogHash = _CalculateCatalogHash();
            // 새 Bind 직후에만 viewport 를 원점으로 리셋. 자동 배치 노드 영역이 보이도록 보장.
            UpdateViewTransform(Vector3.zero, Vector3.one);
        }

        private void _RepopulateNoViewportReset() {
            // CatalogMutated 이벤트 또는 polling 후 호출. 사용자의 viewport 팬/줌 상태 보존.
            _PopulateInternal();
            lastCatalogHash = _CalculateCatalogHash();
        }

        private void _PopulateInternal() {
            _ClearAll();

            if (currentCatalog == null) {
                _ShowEmptyStateHint();
                return;
            }

            _HideEmptyStateHint();

            foreach (KeyValuePair<NodeUID, BaseNode> pair in currentCatalog.Nodes) {
                BaseNode data = pair.Value;
                if (data == null) {
                    HLogger.Warning($"[HGraphCanvas] Null BaseNode at UID {pair.Key}, skipped.");
                    continue;
                }

                // Phase 1-F: 에디터 상태를 catalog 딕셔너리가 아닌 node 자체에서 읽음.
                // Undo.DestroyObjectImmediate(node) 가 editorPosition/FoldoutOpen/OpenSize 를 포함해
                // 원자 복원하므로 삭제 Undo 후에도 위치가 올바르게 복원됨.
#if UNITY_EDITOR
                Vector2 pos = data.EditorPosition;
                bool isExpanded = data.EditorFoldoutOpen;
                Vector2 openSize = data.EditorOpenSize;
#else
                Vector2 pos = Vector2.zero;
                bool isExpanded = false;
                Vector2 openSize = Vector2.zero;
#endif

                bool isRoot = pair.Key == currentCatalog.RootUID;
                // Phase 3: CatalogNode / HubNode 는 전용 시각 클래스로 생성.
                HGraphNode view = data switch {
                    CatalogNode catalogData => new HGraphCatalogNode(catalogData, isRoot),
                    HubNode hubData        => new HGraphHubNode(hubData, isRoot),
                    _                      => new HGraphNode(data, isRoot)
                };
                view.Catalog = currentCatalog;
                view.SetPosition(new Rect(pos, Vector2.zero));
                view.ApplyEditorState(isExpanded, openSize);

                // 이벤트 구독으로 catalog 갱신 진입점 통합 (Phase 1-A 의 graphViewChanged 와 같은 책임 분리).
                // closure 캡처용 로컬 변수 - foreach 변수 직접 캡처는 일부 C# 버전에서 stale 가능.
                NodeUID uid = pair.Key;
                view.FoldoutChanged += isOpen => NodeCatalogAuthor.SetFoldoutOpen(currentCatalog, uid, isOpen);
                view.OpenSizeChanged += size => NodeCatalogAuthor.SetOpenSize(currentCatalog, uid, size);

                AddElement(view);
                nodeLookup[pair.Key] = view;
            }

            // Phase 3+ — HubNode 출력 포트 사전 생성.
            // 엣지 연결 전 HubNode 의 entries 수 기반으로 EnsureOutputPorts 호출.
            // CatalogNode 는 단순 1 input + 1 output — 사전 포트 생성 불필요.
            foreach (var (uid, view) in nodeLookup) {
                if (view is HGraphHubNode hubView && hubView.DataNode is HubNode hub) {
                    hubView.EnsureOutputPorts(hub.PortCount);
                }
            }

            // Phase 2 — 엣지 populate.
            // 노드 VisualElement 가 모두 추가된 후에 엣지를 연결해야 포트 참조가 유효.
            // HubNode 엣지(HubNodeEdge): BranchPortKey 로 정확한 출력 포트 조회.
            // 일반 노드 / CatalogNode 엣지: 단일 OutputPort / InputPort 사용.
            foreach (BaseNodeEdge dataEdge in currentCatalog.Edges) {
                if (dataEdge == null) continue;
                if (!nodeLookup.TryGetValue(dataEdge.BranchUID, out HGraphNode branchView)) {
                    HLogger.Warning($"[HGraphCanvas] Edge 브랜치 노드 {dataEdge.BranchUID} 미발견, 스킵.");
                    continue;
                }
                if (!nodeLookup.TryGetValue(dataEdge.LeafUID, out HGraphNode leafView)) {
                    HLogger.Warning($"[HGraphCanvas] Edge 리프 노드 {dataEdge.LeafUID} 미발견, 스킵.");
                    continue;
                }

                Port outputPort;
                if (branchView is HGraphHubNode hubBranch && dataEdge is HubNodeEdge hubEdge) {
                    outputPort = hubBranch.GetOutputPortByKey(hubEdge.BranchPortKey);
                    if (outputPort == null) {
                        HLogger.Warning($"[HGraphCanvas] HubNode OutputPort 키 '{hubEdge.BranchPortKey}' 미발견, 스킵.");
                        continue;
                    }
                } else {
                    outputPort = branchView.OutputPort;
                }

                if (outputPort == null) {
                    HLogger.Warning($"[HGraphCanvas] OutputPort null: branchUID={dataEdge.BranchUID}, 스킵.");
                    continue;
                }

                HGraphEdge edgeView = new HGraphEdge(dataEdge.BranchUID, dataEdge.LeafUID);
                edgeView.Catalog = currentCatalog;
                edgeView.output = outputPort;
                edgeView.input = leafView.InputPort;
                outputPort.Connect(edgeView);
                leafView.InputPort.Connect(edgeView);

                AddElement(edgeView);
                edgeLookup[(dataEdge.BranchUID, dataEdge.LeafUID)] = edgeView;
            }

            // GraphView 가 자식 변경 후 자동 redraw 안 하는 케이스 방지.
            // CatalogMutated 이벤트로 호출된 Repopulate 가 다음 사용자 인터랙션까지
            // 미뤄지지 않도록 명시적으로 dirty 표시.
            MarkDirtyRepaint();
        }

        private void _ClearAll() {
            // 엣지 먼저 제거 — 포트 참조가 살아 있는 상태에서 노드 제거 시
            // GraphView 내부 포트 정리가 엣지 상태와 충돌 가능.
            foreach (HGraphEdge edge in edgeLookup.Values) {
                RemoveElement(edge);
            }
            edgeLookup.Clear();

            foreach (HGraphNode node in nodeLookup.Values) {
                RemoveElement(node);
            }
            nodeLookup.Clear();
        }
        #endregion

        #region GraphView Change Hook
        private GraphViewChange _OnGraphViewChanged(GraphViewChange change) {
            if (currentCatalog == null) return change;

            // 노드 이동 (고빈도 — SetLayout 만 호출, CatalogMutated 발송 X).
            if (change.movedElements != null) {
                foreach (GraphElement elem in change.movedElements) {
                    if (elem is HGraphNode node) {
                        Vector2 newPos = node.GetPosition().position;
                        NodeCatalogAuthor.SetLayout(currentCatalog, node.UID, newPos);
                    }
                }
            }

            // Phase 2 — 엣지 생성: 사용자가 포트 드래그 완료 시 edgesToCreate 에 raw Edge 전달.
            // edgesToCreate 를 clear 해 GraphView 의 자체 시각 추가를 막고,
            // ConnectEdge → CatalogMutated → _RepopulateNoViewportReset 이 HGraphEdge 를 생성.
            // HubNode 출력 포트 드래그 시 portKey 를 캡처해 ConnectHubEdge 로 HubNodeEdge 생성.
            if (change.edgesToCreate != null && change.edgesToCreate.Count > 0) {
                foreach (Edge gvEdge in change.edgesToCreate) {
                    if (gvEdge.output?.node is HGraphNode branchNode &&
                        gvEdge.input?.node is HGraphNode leafNode) {
                        if (branchNode is HGraphHubNode hubBranch) {
                            string portKey = hubBranch.GetOutputPortKey(gvEdge.output);
                            NodeCatalogAuthor.ConnectHubEdge(
                                currentCatalog, branchNode.UID, leafNode.UID, portKey);
                        } else {
                            NodeCatalogAuthor.ConnectEdge<SimpleNodeEdge>(
                                currentCatalog, branchNode.UID, leafNode.UID);
                        }
                    }
                }
                change.edgesToCreate.Clear();
            }

            return change;
        }
        #endregion
    }
}

/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.10 Phase 3+ — HubNode 지원 + CatalogNode 단순화
 *
 * # 변경
 * - _PopulateInternal 노드 생성 분기: data switch 로 HubNode → HGraphHubNode 추가.
 * - _PopulateInternal HubNode 포트 사전 생성: HGraphHubNode.EnsureOutputPorts(hub.PortCount).
 *   CatalogNode 관련 catalogIncoming / catalogOutgoing 집계 로직 전면 제거.
 * - _PopulateInternal 엣지 연결: HubNodeEdge 검출 → GetOutputPortByKey(portKey) 로 정확한 포트 조회.
 *   일반 노드 / CatalogNode 는 단일 OutputPort.
 * - _OnGraphViewChanged: HubNode output port 드래그 시 GetOutputPortKey → ConnectHubEdge.
 *   CatalogNode AddSpareOutputPort 호출 제거 (CatalogNode 단일 output port 로 복귀).
 *
 * # 이유
 * - CatalogNode 는 외부 카탈로그 연결 표시용 단순 노드 (1 in + 1 out). 다중 포트 역할 폐기.
 * - 다중 포트 라우팅은 HubNode 전담. 역할 단일화로 버그 원인(incoming 집계 오류) 구조적 제거.
 * - HubNodeEdge.BranchPortKey 직렬화로 repopulate 시 key → Port 결정론적 매핑.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.10 Phase 3+ — CatalogNode 동적 포트 버그픽스 (incoming 연결 카운팅)
 *
 * # 변경
 * - _PopulateInternal: catalogOutCount(outgoing 전용) → catalogIncoming + catalogOutgoing 두 dict 로 분리.
 *   EnsureOutputPorts(inc + out + 1) 로 들어오는/나가는 연결 합산 포트 생성.
 *   엣지 연결 루프: CatalogNode 가 LeafUID 인 경우 출력 포트 UI 에 "← branch 이름" 레이블 반영 후
 *   GraphView 엣지는 InputPort 에 연결. CatalogNode 가 BranchUID 인 경우 incOffset+outIdx 로 포트 인덱스
 *   결정 + "→ leaf 이름" 레이블.
 *
 * # 이유
 * - 버그: 사용자가 CatalogNode 에 연결선을 드래그(CatalogNode = LeafUID)할 때 outgoing 카운터는
 *   증가하지 않아 포트가 1개(스페어)에서 증가하지 않음. incoming 집계가 누락된 원인.
 * - 포트 UI 배치 암묵 규칙: [0..inc-1] = 들어오는 연결 레이블 전용, [inc..inc+out-1] = 나가는 연결.
 *   BaseNodeEdge 에 portIndex 필드 추가 없이 repopulate 시 결정론적 매핑 유지.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.10 Phase 3+ — CatalogNode 동적 포트 지원
 *
 * # 변경
 * - _PopulateInternal: CatalogNode 별 outgoing 엣지 수 집계 → EnsureOutputPorts(N+1) 사전 생성.
 *   portIndexOf dict 로 CatalogNode 에 대한 엣지-포트 인덱스 카운터 관리.
 *   GetOutputPort(index) + UpdateOutputPortLabel(index, title) 로 각 포트에 레이블 적용.
 * - _OnGraphViewChanged: CatalogNode 스페어 포트 소진 시 AddSpareOutputPort() 추가 (repopulate 전 임시).
 *   ConnectEdge 가 repopulate 를 발동하므로 최종 포트 배치는 repopulate 담당.
 *
 * # 이유
 * - "포트 인덱스 = catalog.Edges 에서 이 노드가 브랜치인 엣지 순서" 암묵 정의.
 *   BaseNodeEdge 에 branchPortIndex 필드 추가 없이 결정론적 매핑 가능.
 * - 모든 수정은 CatalogMutated → repopulate 를 거치므로 포트 상태는 항상 repopulate 가 단일 소스.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.10 Phase 3 — CatalogNode 지원 (Populate 분기 + 스위치 이벤트)
 *
 * # 변경
 * - _PopulateInternal: data is CatalogNode 분기 추가 → HGraphCatalogNode 생성.
 * - CatalogSwitchRequested 이벤트 + RequestCatalogSwitch 메서드 추가.
 *   HGraphCatalogNode.OnHeaderDoubleClick → canvas.RequestCatalogSwitch → HGraphWindow._BindCatalog.
 * - ToGraphPosition(Vector2): canvas 로컬 → 그래프 월드 좌표 변환 헬퍼.
 *   HGraphWindow 드래그드롭 드롭 위치 변환용 (pan/zoom 보정 포함).
 * - 빈 캔버스 힌트 텍스트 갱신: "Open Catalog button" → "toolbar catalog field" (ObjectField 전환 반영).
 *
 * # 이유
 * - nodeLookup(Dictionary<NodeUID, HGraphNode>)은 변경 불필요 — HGraphCatalogNode is-a HGraphNode.
 * - contentViewContainer.WorldToLocal(LocalToWorld(pos)) = 올바른 pan/zoom 보정 경로.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.10 Phase 2 — 연결선 시스템 (Port + HGraphEdge + GetCompatiblePorts)
 *
 * # 변경
 * - edgeLookup: (BranchUID, LeafUID) → HGraphEdge 역매핑 추가.
 * - GetCompatiblePorts override: 다른 노드 + 반대 방향 포트 조합만 허용 (self-loop 차단).
 * - _ClearAllNodes → _ClearAll: 엣지 먼저 제거 후 노드 제거 (포트 참조 안전 정리).
 * - _PopulateInternal: 노드 추가 후 엣지 populate (HGraphEdge 생성 + 포트 Connect).
 * - _OnGraphViewChanged: edgesToCreate 처리 — ConnectEdge<SimpleNodeEdge> 호출 후
 *   edgesToCreate.Clear() (GraphView 자체 시각 추가 차단, repopulate 가 HGraphEdge 생성).
 * - _DeleteSelectedEdges: selection 에서 HGraphEdge 추려 DisconnectEdge 일괄 호출.
 * - Delete 키: DeleteNodes + _DeleteSelectedEdges 동시 처리.
 * - CenterOnNode(uid): nodeLookup 조회 후 CenterViewportOn 진입점.
 * - HighlightNodesByEdge: 브랜치 + 리프 노드에 "hgraph-node--edge-highlight" CSS 토글.
 * - _CalculateCatalogHash: EdgeCount 포함 (엣지 변경 polling 감지).
 *
 * # 이유
 * - edgesToCreate.Clear() 필수: ConnectEdge 가 CatalogMutated → _RepopulateNoViewportReset 를
 *   동기 발동. repopulate 가 HGraphEdge 를 이미 추가했으므로 GraphView 자체 추가 차단 않으면
 *   시각 엣지 중복 발생.
 * - _DeleteSelectedEdges 직접 구현 필요: HGraphEdge.Capabilities.Deletable 비활성이므로
 *   graphViewChanged.elementsToRemove 경로로 엣지 삭제 이벤트 미발송.
 *
 * @Jason - PKH 2026.05.09 Phase 1-F — _PopulateInternal + GoToRoot 이관 (catalog → BaseNode)
 *
 * # 변경
 * - _PopulateInternal: catalog.EditorNodeLayouts / FoldoutOpen / OpenSizes TryGetValue
 *   → data.EditorPosition / data.EditorFoldoutOpen / data.EditorOpenSize 직접 읽기
 * - GoToRoot: catalog.EditorNodeLayouts.TryGetValue(root, out saved)
 *   → catalog.Nodes.TryGetValue(root, out rootNode) + rootNode.EditorPosition
 *   (컴파일 오류 수정 — EditorNodeLayouts 프로퍼티가 NodeCatalogSO 에서 제거됨)
 *
 * # 이유
 * - NodeCatalogSO Phase 1-F 에서 editor HDictionary 3개 전부 제거에 따른 소비처 갱신.
 * - GoToRoot 는 제거된 프로퍼티를 직접 호출해 컴파일 오류를 유발하므로 즉시 수정 필수.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.09 Phase 1-F — Close All + Undo/Redo 인프라
 *
 * # 변경
 * - Undo.undoRedoPerformed += _OnUndoRedo (constructor) / -= (DetachFromPanelEvent)
 * - _OnUndoRedo: NotifyExternalMutation → canvas repopulate
 * - _OnKeyDown: Ctrl+Z (Undo) / Ctrl+Shift+Z + Ctrl+Y (Redo) 케이스 추가
 *   GraphView 포커스 시 UIElements 가 Unity 전역 단축키 차단 → 직접 호출 필요
 * - CloseAllFoldouts(): graphElements 순회 + CloseIfExpanded 일괄 호출
 *
 * =============================================================================
 * @Jason - PKH 2026.05.09 hash 계산 int .Value → .GetHashCode() 전환
 *
 * # 변경
 * - _CalculateCatalogHash: RootUID.Value + pair.Key.Value (int 산술)
 *   → RootUID.GetHashCode() + pair.Key.GetHashCode() (string 호환)
 * - NodeUID.Value 반환형 int → string 으로 바뀜에 따른 컴파일 오류 수정
 *
 * =============================================================================
 */
// ─────────────────────────────────────────────────────────────────────────────
// Dev Log
// ─────────────────────────────────────────────────────────────────────────────
// 2026-04-21 · USS 로드 전략 메모
//
//   [현재 방식] AssetDatabase.FindAssets($"t:StyleSheet {USS_ASSET_NAME}")
//              · 이름 기반 검색. 경로/리네임/UPM 이전에 전부 생존.
//              · L1 베이스 수준에서 충분히 견고함.
//
//   [추후 전환 요청] Option 3 — 고정 GUID 상수 방식
//              · 왜 전환 필요:
//                 1. HWindows 내 USS 자산이 2개 이상으로 늘어날 때 동명 충돌 방지 필요
//                 2. 자산 참조 계약을 엄격화(리뷰 게이트, 계약적 참조)해야 할 때
//                 3. 프로덕션 품질 수준에서 Unity 자산 시스템과 정합(GUID는 일등 시민)
//              · 전환 절차:
//                 (a) HGraphWindow.uss.meta 에서 guid 값 확인
//                 (b) private const string USS_GUID = "<해당 guid>"; 로 교체
//                 (c) _LoadStyleSheet 내부 FindAssets 호출을
//                     AssetDatabase.GUIDToAssetPath(USS_GUID) 로 치환
//                 (d) USS_ASSET_NAME 상수 제거
//              · 장점: 동명 자산 애매성 0, 리네임·이동 완전 무관, 계약 명시적.
//              · 단점: .meta 재생성 등 드문 상황에서 GUID 수동 업데이트 필요.
// ─────────────────────────────────────────────────────────────────────────────

// =============================================================================
// Dev Log - Phase 1-A 확장 (2026-04-24)
// =============================================================================
// - Bind(catalog) / Unbind(): catalog 주입 + Populate 트리거. 같은 catalog 재진입 시 조기 return.
// - _Populate(): 하이브리드 전략 - 전체 재구성 (기존 HGraphNode 전부 제거 후 catalog.Nodes 순회 생성).
//   + Bind 는 드문 이벤트 (Selection 변경/드래그드롭/Open 버튼) 라 매번 전체 재구성해도 체감 지연 0.
//   + 드래그 이동 같은 고빈도 변경은 graphViewChanged 훅에서 위치만 증분 반영.
// - _OnGraphViewChanged: GraphView.graphViewChanged 에 등록된 콜백.
//   change.movedElements 순회하며 HGraphNode 의 새 위치를 Author.SetLayout 으로 catalog 에 반영.
// - _emptyStateHint: catalog 미바인드 상태에서 중앙에 "Drop a Node Catalog here..." 안내 표시.
//   pickingMode=Ignore 로 드래그드롭/클릭 이벤트를 하단 GraphView 로 pass-through.
// - _nodeLookup: UID -> HGraphNode 역매핑. Phase 1-D 선택 하이라이트/Phase 1-G Floating GUI
//   에서 "UID 로 VisualElement 찾기" 경로에 활용.
//
// [Stage 4 검증 보정 - 2026-04-25]
// - 자동 배치 분산: 스펙 P1-e 의 (0, 0) 고정에서 (autoIndex * 220, 0) 분산으로 변경.
//   + 다중 노드를 한 번에 Bind 하면 모두 같은 좌표에 겹쳐 사용자가 식별 못 하던 문제 해소.
//   + 220 = USS min-width 180 + 여백 40. 노드끼리 안 겹치는 최소 간격.
//   + saved layout 이 있는 노드는 그대로 사용. 신규 노드만 분산 인덱스 증가.
// - viewport 원점 리셋: Populate 끝에 UpdateViewTransform(Vector3.zero, Vector3.one) 호출.
//   + 새 catalog 를 Bind 한 직후 viewport 가 어디인지 모호한 상태를 차단.
//   + 자동 배치 노드들이 (0~N*220, 0) 영역에 위치하므로 원점 viewport 에서 보임.
//   + Phase 1-C "Go To Root" 가 들어오면 더 정교한 framing 으로 대체될 수 있음.
//
// [StretchToParentSize 제거 이유]
// - L1 에서는 생성자 마지막에 this.StretchToParentSize() 호출 (Canvas 가 혼자 root 를 채움).
// - Phase 1-A 에서 HGraphWindow 에 Toolbar 가 추가되어 root 가 Column flex 레이아웃이 됨.
// - StretchToParentSize() = position:absolute + left/right/top/bottom:0 → flex 레이아웃 무시, root 전체 덮음.
// - 결과: Toolbar 가 Canvas 아래 숨어 보이지 않음.
// - 해결: StretchToParentSize() 제거. HGraphWindow 가 canvas.style.flexGrow = 1 로 영역 할당.
// - 내부 GridBackground 의 StretchToParentSize() 는 canvas 내부를 채우는 용도로 유지.
// =============================================================================

// =============================================================================
// Dev Log - Phase 1-D 추가 (2026-05-07/08)
// =============================================================================
// - BuildContextualMenu override (base 호출 생략):
//   + GraphView 기본 selection 메뉴 (Cut/Copy/Paste/Duplicate/Delete) 자동 추가 차단.
//   + HGraphNode.capabilities 차단 (Copiable | Deletable) 만으로는 GraphView 측 메뉴가 살아남음.
//     UI Toolkit 의 ContextualMenu propagation 이 leaf + parent 양쪽 BuildContextualMenu 호출.
//   + 두 layer 양쪽 차단 필요 — leaf (HGraphNode capabilities) + parent (본 override) 모두.
//   + Phase 1-D Stage 2 검증 도중 사용자 보고 ("Duplicate 중복 유지") 로 발견된 함정.
//
// - Paste 메뉴 추가 (Phase 1-D Cut/Paste 확장 - 2026-05-08):
//   + 빈 캔버스 우클릭 시 Paste 만 표시. 다른 GraphView 자동 항목은 base 미호출로 차단 유지.
//   + 노드 위 우클릭 시는 HGraphNode 가 Paste 추가 — evt.target 의 ancestor chain 검사로 중복 회피.
//   + Paste 활성/비활성 = HGraphClipboard.IsValid(systemCopyBuffer) — 우리 형식 magic 검사.
//
// - Clipboard Actions helper + 키보드 단축키 (Phase 1-D 단축키 - 사용자 결정 2026-05-08):
//   + Copy/Cut/Paste/Duplicate/Delete 5 helper (CopyNodes / CutNodes / PasteFromClipboard /
//     DuplicateNodes / DeleteNodes) internal — 단축키 + HGraphNode 우클릭 메뉴가 공유 진입점.
//   + DRY — 메뉴 핸들러 (HGraphNode._OnContext*) 모두 본 helper 호출로 단순화.
//   + KeyDownEvent 핸들러 (_OnKeyDown) — actionKey + (C/X/V/D) + 단독 Delete 키.
//   + actionKey = platform 추상화 — Mac Cmd / 그 외 Ctrl 자동 매핑.
//   + capabilities 차단 (Copiable | Deletable) 과 별개 event path — 우리 핸들러는 capabilities 무관 동작.
//   + Paste 단축키는 selection 무관, Copy/Cut/Duplicate/Delete 단축키는 selection 기반 (0 이면 무반응).
//   + Delete 는 modifier 없는 단독 키 — actionKey 분기 외부에서 처리.
//   + macOS 의 main "delete" 키는 KeyCode.Backspace (Forward Delete = KeyCode.Delete) — 사용자
//     spec 그대로 Delete 만 처리. Backspace 추가 의향 시 한 줄 추가로 대응 가능.
//   + KeyDownEvent 의 element callback 은 panel detach 시 자동 정리 — 명시 unregister 불필요.
// =============================================================================

// =============================================================================
// Dev Log - Phase 1-E 추가 (2026-05-08)
// =============================================================================
// - gridBackground field 끌어올림 (Phase 1-E P1E-ε):
//   + 기존 로컬 변수를 fields region 의 instance field 로 변경.
//   + ShowGrid 동기화 위해 인스턴스 보존 필요 — visible 속성 동적 갱신.
//   + 생성자에서 초기 ShowGrid 동기화 (NodeSnapSettings.instance.ShowGrid).
//
// - SnapSettingsChanged 구독 (Phase 1-E P1E-ε):
//   + NodeWindowSettingsProvider 의 static event 구독으로 settings 변경 시 GridBackground.visible
//     + MarkDirtyRepaint 호출.
//   + DetachFromPanelEvent 에서 unsubscribe — 메모리 누수 방지.
//
// - Ctrl+A / Cmd+A 단축키 (Phase 1-E P1E-α + P1E-5):
//   + _OnKeyDown 의 actionKey 분기에 KeyCode.A 추가 — Phase 1-D 의 C/X/V/D 와 같은 path.
//   + evt.StopPropagation() 호출 — Unity Edit > Select All 충돌 방지 (P1E-5).
//
// - SelectAllNodes (Phase 1-E Q7 B):
//   + graphElements 순회 + is HGraphNode 검사 (시각 진실성).
//   + ClearSelection + AddToSelection 으로 selection state 재구성.
//   + 비용: O(M), M ≈ N + 3~4. N=100 시 ~1μs (Q7 비용 분석 참조).
//   + Phase 3 DepthTree 도입 시 "활성 layer 만" 의미 자동 정합 (graphElements = 화면 표시 요소).
//
// - BuildContextualMenu "모두 선택" 항목 (Phase 1-E P1E-8):
//   + 빈 캔버스 우클릭 시점만 표시 — HGraphNode 위 우클릭은 노드 메뉴 (Phase 1-D) 6 항목 유지.
//   + graphElements foreach + is + break — first-match short-circuit.
// =============================================================================

// =============================================================================
// Dev Log - Phase 1-B 확장 (2026-05-07)
// =============================================================================
// - _PopulateInternal: 노드 생성 시 catalog 의 두 보조 맵 적용 (P1B-b, c).
//   + EditorNodeFoldoutOpen 에서 isExpanded 읽어 ApplyEditorState 호출.
//   + EditorNodeOpenSizes 에서 openSize 읽어 ApplyEditorState 호출.
//   + 두 맵 미보유 시 (false, Vector2.zero) fallback. CreateNode 자동 초기화 미적용 정책과 정합.
// - HGraphNode 의 두 이벤트 구독:
//   + FoldoutChanged → Author.SetFoldoutOpen(catalog, uid, open) 호출.
//   + OpenSizeChanged → Author.SetOpenSize(catalog, uid, size) 호출.
//   + Phase 1-A 의 graphViewChanged 가 layout 갱신을 단일 진입점으로 흡수한 것과 같은 분리.
//     Foldout/OpenSize 갱신도 이벤트 구독을 단일 진입점으로 통합.
//   + closure 캡처 안전성을 위해 foreach 안에서 NodeUID uid = pair.Key 로컬 변수 명시.
// - hash polling 영향:
//   + Author.SetFoldoutOpen / SetOpenSize 는 _NotifyMutated 호출 없음 (P1B-i 고빈도 분류).
//   + Foldout 토글은 본인 노드만 갱신하므로 hash polling 진입 불필요.
//   + Inspector 에서 editorNodeFoldoutOpen 직접 수정은 ObjectChangeWatcher 가 처리.
// =============================================================================

// =============================================================================
// Dev Log - Phase 1-C 확장 (2026-05-07)
// =============================================================================
// - CenterViewportOn(worldPos) : 지정 graph 좌표를 viewport 중앙으로 pan 이동.
//   + GetViewportCenterWorld 의 역수 (position = screenCenter - graphPos * scale).
//   + viewTransform.scale 보존 — 줌 상태 유지하며 pan 만 갱신해 사용자 컨텍스트 깨지 않음.
// - GoToRoot() : currentCatalog.RootUID 의 EditorNodeLayouts 위치로 CenterViewportOn 호출.
//   + 루트 미보유 시 false — 호출자(HGraphWindow) 가 Warning 으로 사용자 피드백.
//   + layout 미보유 fallback (0,0) — Author.CreateNode 가 자동 layout 부여하므로
//     실 발생 케이스는 데이터 호환성 이슈 한정.
//   + Phase 5 메뉴바 이관 시 본 메서드 시그니처 그대로, 호출 진입점만 메뉴로 교체.
// - GetSelectedNodes() : selection 에서 HGraphNode 만 추려 IReadOnlyList 반환.
//   + 목적 = 어댑터 경계 (P1-3) 보존. Window 가 ISelectable (Experimental.GraphView 타입)
//     을 직접 다루지 않도록 한 겹의 헬퍼로 캡슐화.
//   + Phase 1-D 우클릭 메뉴, Phase 1-E 다중 선택 일괄 처리에서도 같은 헬퍼 재사용 예정.
// =============================================================================

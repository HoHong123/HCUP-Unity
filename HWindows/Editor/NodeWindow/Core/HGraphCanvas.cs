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
 * + SearchNodes/AdvanceSearch/ClearSearch: 타이틀 검색 + CSS hgraph-node--search-active 토글.
 * + GetSingleSelectedHGraphNode / SetSelectedAsRoot(UID): 메뉴바 Set as Root 진입점 헬퍼.
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
        const string CSS_SEARCH_ACTIVE = "hgraph-node--search-active";
        #endregion

        #region Fields
        NodeCatalogSO currentCatalog;
        VisualElement emptyStateHint;
        // 윈도우별 추가 우클릭 메뉴 항목. DialogueNodeWindow 등이 캔버스 인스턴스 생성 후 주입.
        // static 이 아닌 인스턴스 필드 — 윈도우마다 독립적인 메뉴 항목 구성 보장.
        public Action<ContextualMenuPopulateEvent> AdditionalContextMenuActions;
        readonly Dictionary<NodeUID, HGraphNode> nodeLookup = new();
        readonly Dictionary<(NodeUID Branch, NodeUID Leaf), HGraphEdge> edgeLookup = new();
        int lastCatalogHash;
        GridBackground gridBackground;  // Phase 1-E P1E-ε: field 로 끌어올림 (ShowGrid 동기화용)
        // Phase 4 — Search
        string _searchQuery = string.Empty;
        readonly List<HGraphNode> _searchResults = new();
        int _searchIndex = -1;
        // PurgeNullNodes 가 SaveAssets → ObjectChangeWatcher → _OnCatalogMutated 재진입 방지.
        bool _isPopulating;
        // 도메인별 노드 시각 팩토리. RegisterNodeViewFactory 로 등록, _PopulateInternal 에서 조회.
        // static: 도메인 리로드마다 InitializeOnLoadMethod 가 재등록 → 인스턴스 간 공유.
        static readonly Dictionary<Type, Func<BaseNode, bool, HGraphNode>> _externalFactories = new();
        #endregion

        #region Constructor + Lifecycle
        public HGraphCanvas() {
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            gridBackground = new GridBackground();
            Insert(0, gridBackground);
            gridBackground.style.position = Position.Absolute;
            gridBackground.style.left = 0;
            gridBackground.style.top = 0;
            gridBackground.style.right = 0;
            gridBackground.style.bottom = 0;
            gridBackground.style.display = NodeSnapSettings.instance.ShowGrid
                ? DisplayStyle.Flex
                : DisplayStyle.None;

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
            if (_isPopulating) return;
            // viewport 위치는 사용자가 조정해 둔 상태일 수 있으므로 _Populate 가
            // 강제 리셋하지 않도록 별도 경로. 현재 구현은 _Populate 가 viewport 리셋을 포함하므로
            // 빈번한 mutation 에서 깜빡일 가능성 있음. 필요 시 viewport 보존 분기 추가.
            _RepopulateNoViewportReset();
        }

        public Vector2 GetViewportCenterWorld() {
            Translate vt = contentViewContainer.resolvedStyle.translate;
            float scale = contentViewContainer.resolvedStyle.scale.value.x;
            if (scale == 0f) scale = 1f;
            Rect rect = contentRect;
            Vector2 screenCenter = new Vector2(rect.width / 2f, rect.height / 2f);
            return (screenCenter - new Vector2(vt.x.value, vt.y.value)) / scale;
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

            // 빈 캔버스 우클릭 — 기능별 노드 생성(AdditionalContextMenuActions 경유) / Paste / 모두 선택.
            AdditionalContextMenuActions?.Invoke(evt);

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
                    case KeyCode.Home:
                        // Phase 5: Ctrl+Home = Go To Root. 메뉴바 [View → Go To Root] 와 동일 진입점.
                        GoToRoot();
                        evt.StopPropagation();
                        return;
                    case KeyCode.Alpha0:
                        // Phase 5: Ctrl+0 = Close All Foldouts. 메뉴바 [View → Close All] 와 동일 진입점.
                        CloseAllFoldouts();
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
        // showGrid 는 NodeWindowSettingsProvider 에서 SerializedProperty 기준으로 캡처한 값 — 타이밍 이슈 없음.
        // style.display 사용: GraphView 렌더 패스에서 visibility:hidden 미반영 케이스 방지.
        void _OnSnapSettingsChanged(bool showGrid) {
            if (gridBackground != null) {
                gridBackground.style.display = showGrid ? DisplayStyle.Flex : DisplayStyle.None;
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
            float scale = contentViewContainer.resolvedStyle.scale.value.x;
            if (scale == 0f) scale = 1f;
            Vector3 newPos = new Vector3(
                screenCenter.x - worldPos.x * scale,
                screenCenter.y - worldPos.y * scale,
                0f);
            UpdateViewTransform(newPos, contentViewContainer.resolvedStyle.scale.value);
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

        // Phase 5 — selection 에서 HGraphNode 가 정확히 1 개이면 반환, 0 개 또는 2 개 이상이면 null.
        // 메뉴바 [Edit → Set as Root] status callback + SetSelectedAsRoot() 두 곳에서 사용.
        internal HGraphNode GetSingleSelectedHGraphNode() {
            HGraphNode result = null;
            foreach (ISelectable s in selection) {
                if (s is HGraphNode n) {
                    if (result != null) return null;
                    result = n;
                }
            }
            return result;
        }

        // Phase 5 (메뉴바 진입점) — selection 에서 단일 HGraphNode 의 UID 로 루트 재설정.
        // 반환: 성공 true. catalog null / 단일 미선택 / SetRoot 실패 → false.
        internal bool SetSelectedAsRoot() {
            HGraphNode node = GetSingleSelectedHGraphNode();
            return node != null && SetSelectedAsRoot(node.UID);
        }

        // Phase 5 (우클릭 메뉴 위임 진입점) — 명시 UID 로 루트 재설정.
        // HGraphNode._OnContextSetAsRoot 가 selection 동기화 우려 없이 this.UID 를 직접 전달.
        internal bool SetSelectedAsRoot(NodeUID uid) {
            if (currentCatalog == null) return false;
            bool ok = NodeCatalogAuthor.SetRoot(currentCatalog, uid);
            if (!ok) HLogger.Warning($"[HGraphCanvas] SetSelectedAsRoot failed for UID {uid.Value}");
            return ok;
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

        #region Public - Node View Factory (Phase 5)
        // 도메인 어셈블리(HDialogue.Editor 등)가 자신의 노드 타입과 시각 클래스를 등록.
        // [InitializeOnLoadMethod] 로 도메인 리로드마다 재등록 필수.
        public static void RegisterNodeViewFactory(Type nodeType, Func<BaseNode, bool, HGraphNode> factory) {
            if (nodeType != null && factory != null) _externalFactories[nodeType] = factory;
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
            ClearSearch();  // Phase 4: node 인스턴스 전면 교체 전 stale ref 방지
            _ClearAll();

            if (currentCatalog == null) {
                _ShowEmptyStateHint();
                return;
            }

            _HideEmptyStateHint();

            _isPopulating = true;
            // PurgeNullNodes: sub-asset 외부 삭제로 발생한 ghost UID 를 dict + edge 에서 제거.
            // SaveAssets → ObjectChangeWatcher 동기 재진입은 _isPopulating 가드로 차단.
            NodeCatalogAuthor.PurgeNullNodes(currentCatalog);

            foreach (KeyValuePair<NodeUID, BaseNode> pair in currentCatalog.Nodes) {
                BaseNode data = pair.Value;
                if (data == null) {
                    HLogger.Warning($"[HGraphCanvas] Null BaseNode at UID {pair.Key}, skipped.");
                    continue;
                }

                // Phase 1-F: 에디터 상태를 catalog 딕셔너리가 아닌 node 자체에서 읽음.
                // Undo.DestroyObjectImmediate(node) 가 editorPosition/FoldoutOpen 을 포함해
                // 원자 복원하므로 삭제 Undo 후에도 위치가 올바르게 복원됨.
#if UNITY_EDITOR
                Vector2 pos = data.EditorPosition;
                bool isExpanded = data.EditorFoldoutOpen;
#else
                Vector2 pos = Vector2.zero;
                bool isExpanded = false;
#endif

                bool isRoot = pair.Key == currentCatalog.RootUID;
                // Phase 3 + Phase 5: 도메인 등록 팩토리 우선. CatalogNode / HubNode 폴백.
                HGraphNode view;
                if (_externalFactories.TryGetValue(data.GetType(), out Func<BaseNode, bool, HGraphNode> extFactory)) {
                    view = extFactory(data, isRoot);
                } else {
                    view = data switch {
                        CatalogNode catalogData => new HGraphCatalogNode(catalogData, isRoot),
                        HubNode hubData        => new HGraphHubNode(hubData, isRoot),
                        _                      => new HGraphNode(data, isRoot)
                    };
                }
                view.Catalog = currentCatalog;
                view.SetPosition(new Rect(pos, Vector2.zero));
                view.ApplyEditorState(isExpanded);

                // 이벤트 구독으로 catalog 갱신 진입점 통합 (Phase 1-A 의 graphViewChanged 와 같은 책임 분리).
                // closure 캡처용 로컬 변수 - foreach 변수 직접 캡처는 일부 C# 버전에서 stale 가능.
                NodeUID uid = pair.Key;
                view.FoldoutChanged += isOpen => NodeCatalogAuthor.SetFoldoutOpen(currentCatalog, uid, isOpen);

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

            // 모든 엣지 연결 완료 후 포트 라벨에 연결 수 반영 (Input/Output/Hub 키 + (N)).
            // Port.connections 는 Connect() 호출 즉시 반영되므로 이 시점 카운트가 정확.
            foreach (HGraphNode view in nodeLookup.Values) {
                view.RefreshPortLabels();
            }

            // GraphView 가 자식 변경 후 자동 redraw 안 하는 케이스 방지.
            // CatalogMutated 이벤트로 호출된 Repopulate 가 다음 사용자 인터랙션까지
            // 미뤄지지 않도록 명시적으로 dirty 표시.
            MarkDirtyRepaint();
            _isPopulating = false;
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

        #region Search (Phase 4)
        // query 가 변경될 때마다 결과 목록 재구성 + index 0 으로 이동. 첫 결과 없으면 (0, 0) 반환.
        // HGraphWindow 의 TextField RegisterValueChangedCallback 에서 호출.
        internal (int count, int current) SearchNodes(string query) {
            if (string.IsNullOrEmpty(query)) { ClearSearch(); return (0, 0); }

            _ClearSearchHighlights();
            _searchResults.Clear();
            _searchQuery = query;

            string lower = query.ToLowerInvariant();
            foreach (GraphElement elem in graphElements) {
                if (elem is HGraphNode node &&
                    (node.DataNode.Title ?? string.Empty).ToLowerInvariant().Contains(lower)) {
                    _searchResults.Add(node);
                }
            }

            _searchIndex = _searchResults.Count > 0 ? 0 : -1;
            _ApplySearchHighlight();
            return (_searchResults.Count, _searchIndex >= 0 ? _searchIndex + 1 : 0);
        }

        // 기존 결과 내에서 다음 항목으로 순환. HGraphWindow 의 Enter 키 핸들러에서 호출.
        // 결과 없음 (ClearSearch 또는 repopulate 후) → (0, 0) 반환.
        internal (int count, int current) AdvanceSearch() {
            if (_searchResults.Count == 0) return (0, 0);
            _searchIndex = (_searchIndex + 1) % _searchResults.Count;
            _ApplySearchHighlight();
            return (_searchResults.Count, _searchIndex + 1);
        }

        // 검색 상태 완전 초기화. _PopulateInternal + 카탈로그 전환 + ESC 진입점.
        internal void ClearSearch() {
            _ClearSearchHighlights();
            _searchQuery = string.Empty;
            _searchResults.Clear();
            _searchIndex = -1;
        }

        private void _ClearSearchHighlights() {
            foreach (HGraphNode n in _searchResults) n.RemoveFromClassList(CSS_SEARCH_ACTIVE);
        }

        private void _ApplySearchHighlight() {
            _ClearSearchHighlights();
            if (_searchIndex < 0 || _searchIndex >= _searchResults.Count) return;
            HGraphNode target = _searchResults[_searchIndex];
            target.AddToClassList(CSS_SEARCH_ACTIVE);
            CenterViewportOn(target.GetPosition().position);
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

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.15 "허브 노드 생성" 우클릭 메뉴 항목 제거
 *
 * # 변경
 * - BuildContextualMenu 빈 캔버스 우클릭에서 "허브 노드 생성 (Create Hub Node)" AppendAction 블록 제거
 * - HubNode 직접 생성 지원 중단 — AdditionalContextMenuActions 경유 기능별 파생 노드 생성으로 전환
 *
 * =============================================================================
 * [LOG-20260512-2] PurgeNullNodes 호출 + _isPopulating 재진입 가드
 * [LOG-20260512-1] Show Grid 미적용 버그픽스
 * [LOG-20260511-5] Phase 5 — 메뉴바 단축키 + Single-Selection 헬퍼 신설
 * → 전체 이력: docs/history/HWindows/Editor/NodeWindow/Core/HGraphCanvas.md
 * =============================================================================
 */
#endif

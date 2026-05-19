#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- BaseNode 1개에 대응하는 GraphView 시각 노드.
 *
 * 특징 ::
 * 헤더(색·아이콘·타이틀 2행) + 바디 + 입출력 포트(portRow, 항상 표시) + 리사이즈 핸들 + 폴드아웃 토글.
 * + Snap-to-Grid 드래그, 우클릭 컨텍스트 메뉴 (루트 전환 / 삭제 / 연결선 끊기).
 * + editorPosition / foldoutOpen / openSize — BaseNode 에 직접 저장 (Phase 1-F 이후).
 * + OnSelected → Selection.activeObject = DataNode → Inspector 자동 동기화.
 * + 입구/출구 포트 portName = "Input (N)" / "Output (N)" — RefreshPortLabels 가 연결 수 갱신.
 *
 * 주의사항 ::
 * Capabilities.Copiable | Deletable 비활성 — 복사/삭제는 HGraphCanvas._OnKeyDown 집중.
 * + UnityEditor.Experimental.GraphView 경계 파일 — 빌드 제외 필수.
 * =========================================================
 */
#endif
using System;
using System.Linq;
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
    public class HGraphNode : Node {
        #region Const
        const string USS_ASSET_NAME = "HGraphNode";
        const string ARROW_OPEN = "▼";
        const string ARROW_CLOSED = "▶";
        const string CSS_ACTIVE = "hgraph-node--active";
        #endregion

        #region Fields
        readonly BaseNode dataNode;
        readonly bool isRoot;
        VisualElement headerBar;
        Label titleLabel;
        Label toggleArrow;
        protected VisualElement bodyArea;
        protected Port inputPort;
        protected Port outputPort;
        protected VisualElement portRow;
        #endregion

        #region Properties
        public BaseNode DataNode => dataNode;
        public NodeUID UID => dataNode.UID;
        public bool IsRoot => isRoot;
        public Port InputPort => inputPort;
        public Port OutputPort => outputPort;
        // CatalogNode 다중 출력 포트 인터페이스. 기본 노드는 index 무관 단일 outputPort 반환.
        public virtual Port GetOutputPort(int index) => outputPort;

        // 포트 라벨에 연결 수 표시. canvas._PopulateInternal 의 edges 루프 종료 직후 호출.
        // base: "Input (N)" / "Output (N)" 고정. Hub override 는 키별 카운트 표시.
        public virtual void RefreshPortLabels() {
            if (inputPort != null) inputPort.portName = $"Input ({inputPort.connections.Count()})";
            if (outputPort != null) outputPort.portName = $"Output ({outputPort.connections.Count()})";
        }
        #endregion

        #region Events
        // Foldout 토글 시 발화. HGraphCanvas 가 구독해 Author.SetFoldoutOpen 호출.
        public event Action<bool> FoldoutChanged;
        #endregion

        #region Constructor
        public HGraphNode(BaseNode dataNode, bool isRoot = false) {
            this.dataNode = dataNode;
            this.isRoot = isRoot;

            // GraphView 기본 메뉴의 Cut/Copy/Duplicate/Delete 자동 추가 차단 (P1D-a 정공).
            // capabilities 비트 플래그 꺼서 base.BuildContextualMenu 가 해당 항목을 생략하게 만듦.
            // Copiable 한 플래그가 Cut + Copy + Duplicate 세 항목 통제, Deletable 이 Delete 통제.
            // 트레이드오프: 키보드 Delete 키 / Ctrl+D / Ctrl+C 단축키도 차단됨.
            // → Phase 1-E (Undo + 키보드 단축키) 에서 우리 핸들러로 정식 라우팅 예정.
            capabilities &= ~(Capabilities.Copiable | Capabilities.Deletable);

            _LoadStyleSheet();
            AddToClassList("hgraph-node");

            _BuildHeader();
            _BuildBody();
            _BuildPorts();

            expanded = false;
            RefreshExpandedState();
            RefreshPorts();
        }
        #endregion

        #region Public - Editor State
        // HGraphCanvas Populate 가 catalog 에서 읽은 상태를 노드에 적용.
        public void ApplyEditorState(bool isExpanded) {
            expanded = isExpanded;
            if (toggleArrow != null) toggleArrow.text = _GetToggleSymbol();
            RefreshExpandedState();
        }

        // HCUP-2.7.0 Phase 1 — HGraphCanvas.HighlightActiveNode / ClearActiveHighlight 에서 호출.
        public void SetActive(bool isActive) {
            if (isActive) AddToClassList(CSS_ACTIVE);
            else RemoveFromClassList(CSS_ACTIVE);
        }
        #endregion

        #region Public - Selection (Phase 1-B-3)
        // GraphElement 가상 메서드. GraphView.AddToSelection / RemoveFromSelection 시 자동 호출 (P1B-k).
        // base 호출 유지 — Unity 의 internal selection 추적이 끊어지면 selection 상태 disconnect.
        public override void OnSelected() {
            base.OnSelected();
            AddToClassList("hgraph-node--selected");
            Selection.activeObject = dataNode;
        }

        public override void OnUnselected() {
            base.OnUnselected();
            RemoveFromClassList("hgraph-node--selected");
        }
        #endregion

        #region Public - GraphView Override
        // SelectionDragger 가 매 frame 호출하는 위치 갱신 진입점.
        // NodeSnapSettings 의 Mode + Event.current.shift 분기로 quantize 적용 (좌상단 기준, milestone §1-2-3).
        public override void SetPosition(Rect newPos) {
            Rect quantized = _ApplySnap(newPos);
            base.SetPosition(quantized);
        }

        #endregion

        #region Private - Snap (Phase 1-E)
        Rect _ApplySnap(Rect r) {
            NodeSnapSettings s = NodeSnapSettings.instance;
            bool shouldSnap = s.Mode == SnapMode.Always
                           || (s.Mode == SnapMode.OnShiftHold
                               && Event.current != null
                               && Event.current.shift);
            if (!shouldSnap) return r;
            int u = s.GridUnit;
            if (u <= 0) return r;  // P1E-4 DivByZero 가드
            return new Rect(
                Mathf.Round(r.x / u) * u,
                Mathf.Round(r.y / u) * u,
                r.width,
                r.height);
        }
        #endregion

        #region Internal - Catalog Reference (Phase 1-D)
        // HGraphCanvas Populate 시점에 currentCatalog 주입. BuildContextualMenu 의 핸들러가 catalog 조회용.
        internal NodeCatalogSO Catalog { get; set; }
        #endregion

        #region Internal - Foldout State (Phase 1-F)
        // HGraphCanvas.CloseAllFoldouts 가 일괄 닫기 시 호출.
        internal bool IsExpanded => expanded;

        internal void CloseIfExpanded() {
            if (!expanded) return;
            expanded = false;
            if (toggleArrow != null) toggleArrow.text = _GetToggleSymbol();
            RefreshExpandedState();
            FoldoutChanged?.Invoke(false);
        }
        #endregion

        #region Public - Context Menu (Phase 1-D)
        // GraphView ContextualMenuManipulator 자동 호출 (P1D-a). 메뉴 6 항목 + 구분선 + destructive 마지막.
        // 다중 선택 시 selection 일괄 처리 (Cut/Copy/Duplicate/Delete), 루트 재설정은 단일만 의미.
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt) {
            base.BuildContextualMenu(evt);

            NodeCatalogSO catalog = Catalog;
            if (catalog == null) return;

            List<HGraphNode> targets = _GetEffectiveTargets();
            bool isMulti = targets.Count > 1;

            evt.menu.AppendAction("복사 (Copy)",
                action => _OnContextCopy(catalog, targets),
                DropdownMenuAction.Status.Normal);

            evt.menu.AppendAction("잘라내기 (Cut)",
                action => _OnContextCut(catalog, targets),
                DropdownMenuAction.Status.Normal);

            // Paste 는 클립보드에 우리 형식 JSON 이 있을 때만 활성 (Q9).
            DropdownMenuAction.Status pasteStatus = HGraphClipboard.IsValid(GUIUtility.systemCopyBuffer)
                ? DropdownMenuAction.Status.Normal
                : DropdownMenuAction.Status.Disabled;
            evt.menu.AppendAction("붙여넣기 (Paste)",
                action => _OnContextPaste(catalog),
                pasteStatus);

            evt.menu.AppendAction("복제 (Duplicate)",
                action => _OnContextDuplicate(catalog, targets),
                DropdownMenuAction.Status.Normal);

            // 다중 선택 또는 이미 root 인 노드면 "루트 재설정" 비활성 (P1D-b + Q7).
            DropdownMenuAction.Status setRootStatus = (isMulti || UID == catalog.RootUID)
                ? DropdownMenuAction.Status.Disabled
                : DropdownMenuAction.Status.Normal;
            evt.menu.AppendAction("루트 노드 재설정 (Set as Root)",
                action => _OnContextSetAsRoot(catalog),
                setRootStatus);

            evt.menu.AppendSeparator();

            evt.menu.AppendAction("삭제 (Delete)",
                action => _OnContextDelete(catalog, targets),
                DropdownMenuAction.Status.Normal);
        }

        // 우클릭한 노드 + 현재 selection 으로 작업 대상 결정.
        // selection 이 본 노드 포함하면 selection 일괄, 미포함이면 본 노드 1 개만.
        // GraphView 가 우클릭 시 selection 변경 X 라 우클릭한 노드가 selection 미포함 케이스 가능.
        private List<HGraphNode> _GetEffectiveTargets() {
            HGraphCanvas canvas = GetFirstAncestorOfType<HGraphCanvas>();
            IReadOnlyList<HGraphNode> selected = canvas?.GetSelectedNodes();
            if (selected != null && selected.Count > 0 && selected.Contains(this)) {
                return new List<HGraphNode>(selected);
            }
            return new List<HGraphNode> { this };
        }
        #endregion

        #region Private - Context Menu Handlers (Phase 1-D)
        // Copy / Cut / Paste 는 HGraphCanvas 의 helper 로 위임 — 단축키와 같은 진입점 공유 (DRY).
        private void _OnContextCopy(NodeCatalogSO catalog, List<HGraphNode> targets) {
            HGraphCanvas canvas = GetFirstAncestorOfType<HGraphCanvas>();
            canvas?.CopyNodes(targets);
        }

        private void _OnContextCut(NodeCatalogSO catalog, List<HGraphNode> targets) {
            HGraphCanvas canvas = GetFirstAncestorOfType<HGraphCanvas>();
            canvas?.CutNodes(targets);
        }

        private void _OnContextPaste(NodeCatalogSO catalog) {
            HGraphCanvas canvas = GetFirstAncestorOfType<HGraphCanvas>();
            canvas?.PasteFromClipboard();
        }

        private void _OnContextDuplicate(NodeCatalogSO catalog, List<HGraphNode> targets) {
            HGraphCanvas canvas = GetFirstAncestorOfType<HGraphCanvas>();
            canvas?.DuplicateNodes(targets);
        }

        private void _OnContextSetAsRoot(NodeCatalogSO catalog) {
            // Phase 5: canvas.SetSelectedAsRoot(UID) 로 위임. 명시 UID 오버로드 사용 —
            // GraphView 가 우클릭 시 selection 갱신 안 하므로 selection 기반 오버로드와 분리.
            HGraphCanvas canvas = GetFirstAncestorOfType<HGraphCanvas>();
            if (canvas != null) {
                canvas.SetSelectedAsRoot(UID);
            } else {
                bool ok = NodeCatalogAuthor.SetRoot(catalog, UID);
                if (!ok) HLogger.Warning($"[HGraphNode] SetRoot failed for UID {UID.Value}");
            }
        }

        private void _OnContextDelete(NodeCatalogSO catalog, List<HGraphNode> targets) {
            HGraphCanvas canvas = GetFirstAncestorOfType<HGraphCanvas>();
            canvas?.DeleteNodes(targets);
        }
        #endregion

        #region Private - UI Build
        private void _BuildHeader() {
            headerBar = new VisualElement();
            headerBar.AddToClassList("hgraph-node-header");

            // 루트 노드는 도메인 커스터마이즈와 무관하게 항상 RootHeaderColor (사용자 규칙).
            Color headerColor = isRoot
                ? HGraphNodeStyles.RootHeaderColor
                : HGraphNodeStyles.GetHeaderColorFor(dataNode.GetType());
            headerBar.style.backgroundColor = new StyleColor(headerColor);

            // 상단 행: 토글 화살표 + 타입명.
            VisualElement headerRow = new VisualElement();
            headerRow.AddToClassList("hgraph-node-header-row");

            // 토글 진입점 (1) — 헤더 좌측 ▶/▼ 아이콘 클릭 (P1B-e A).
            toggleArrow = new Label(_GetToggleSymbol());
            toggleArrow.AddToClassList("hgraph-node-toggle-arrow");
            toggleArrow.RegisterCallback<MouseDownEvent>(_OnToggleArrowMouseDown);
            headerRow.Add(toggleArrow);

            string headerText = isRoot
                ? $"{dataNode.GetType().Name}  [ROOT]"
                : dataNode.GetType().Name;
            Label headerLabel = new Label(headerText);
            headerRow.Add(headerLabel);
            headerBar.Add(headerRow);

            // 타이틀 행 — 헤더 내 두 번째 행 (닫힘/열림 무관하게 항상 표시).
            titleLabel = new Label(dataNode.Title);
            titleLabel.AddToClassList("hgraph-node-title");
            headerBar.Add(titleLabel);

            // 토글 진입점 (2) — 헤더 더블클릭 (P1B-e B). toggleArrow 위 클릭은 (1) 이 먼저 잡음.
            headerBar.RegisterCallback<MouseDownEvent>(_OnHeaderMouseDown);

            mainContainer.Insert(0, headerBar);
        }

        private void _BuildBody() {
            bodyArea = new VisualElement();
            bodyArea.AddToClassList("hgraph-node-body");

            // Phase 1-B 의 placeholder. 도메인 서브 노드가 자체 Body 콘텐츠를 채울 확장 지점.
            Label bodyPlaceholder = new Label($"UID : {UID.Value}");
            bodyPlaceholder.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
            bodyPlaceholder.style.fontSize = 11;
            bodyArea.Add(bodyPlaceholder);

            // GraphView.Node 의 extensionContainer 는 expanded=false 시 내장 hide 동작 (P1B-a).
            extensionContainer.Add(bodyArea);
        }

        protected virtual void _BuildPorts() {
            // 입력 포트(리프 측)와 출력 포트(브랜치 측).
            // mainContainer 직속 portRow 에 배치 — GraphView collapse 관리 범위 밖이라 항상 표시됨.
            inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            inputPort.portName = "Input";

            outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
            outputPort.portName = "Output";

            portRow = new VisualElement();
            portRow.AddToClassList("hgraph-node-port-row");
            portRow.Add(inputPort);
            portRow.Add(outputPort);

            // headerBar 는 index 0. portRow 를 index 1 에 삽입해 헤더와 바디(extensionContainer) 사이에 배치.
            mainContainer.Insert(1, portRow);
        }

        private void _LoadStyleSheet() {
            string[] guids = AssetDatabase.FindAssets($"t:StyleSheet {USS_ASSET_NAME}");
            if (guids.Length == 0) return;

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            StyleSheet sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            if (sheet == null) return;

            styleSheets.Add(sheet);
        }
        #endregion

        #region Private - Foldout Toggle
        private string _GetToggleSymbol() => expanded ? ARROW_OPEN : ARROW_CLOSED;

        private void _OnToggleArrowMouseDown(MouseDownEvent evt) {
            if (evt.button != 0) return;
            _ToggleExpanded();
            evt.StopPropagation();
        }

        private void _OnHeaderMouseDown(MouseDownEvent evt) {
            if (evt.button != 0) return;
            if (evt.clickCount != 2) return;
            OnHeaderDoubleClick(evt);
        }

        // 더블클릭 액션. 파생 타입(HGraphCatalogNode 등)이 override해 다른 동작 삽입 가능.
        protected virtual void OnHeaderDoubleClick(MouseDownEvent evt) {
            _ToggleExpanded();
            evt.StopPropagation();
        }

        private void _ToggleExpanded() {
            expanded = !expanded;
            if (toggleArrow != null) toggleArrow.text = _GetToggleSymbol();
            RefreshExpandedState();
            FoldoutChanged?.Invoke(expanded);
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * [LOG-20260511-3] 리사이즈 시스템 전면 제거 (최후순위 이월)
 * [LOG-20260511-2] RefreshPortLabels() virtual 추가
 * [LOG-20260511-1] 포트 portName 기본값 부여 (Input/Output)
 * → 전체 이력: docs/history/HWindows/Editor/NodeWindow/Core/HGraphNode.md
 * =============================================================================
 */
#endif

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
 * @Jason - PKH 2026.05.11 — 리사이즈 시스템 전면 제거 (최후순위 이월)
 *
 * # 변경
 * - MIN_RESIZE_WIDTH / MIN_RESIZE_HEIGHT 상수 제거.
 * - resizeHandle / openSize / isResizing / resizeStartMousePos / resizeStartSize 필드 제거.
 * - ResizeHandle / OpenSize 프로퍼티 제거.
 * - OpenSizeChanged 이벤트 제거.
 * - NotifyResizeFinished / _BuildResizeHandle / _ApplyOpenSize / _ApplyResizeHandleVisibility
 *   / _OnResizeHandleMouseDown/Move/Up / _GetGraphViewScale 메서드 제거.
 * - _ToggleExpanded / CloseIfExpanded / ApplyEditorState: openSize 파라미터·호출 제거.
 * - ApplyEditorState 시그니처 (bool isExpanded) 로 단순화.
 *
 * # 이유
 * - GraphView.Node 의 contentContainer/mainContainer 레이아웃 제약(flex-basis:auto)으로
 *   핸들 위치·FoldOut 이벤트 차단·크기 복원 등 3중 충돌 발생. 단기 해결 복잡도 > 기능 가치.
 * - 기능 상세 및 시도 이력은 이 엔트리 아래 세 개 이전 엔트리 (삭제됨) 참고.
 * - 이월 항목: 메모리 project_deferred_features.md 에 등록.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.11 — RefreshPortLabels() virtual 추가
 *
 * # 변경
 * - public virtual RefreshPortLabels(): inputPort/outputPort 의 portName 을
 *   "Input (N)" / "Output (N)" 형식으로 갱신. N = port.connections.Count().
 *
 * # 이유
 * - 포트 라벨에 연결 엣지 수를 시각 노출 — 노드 연결 상태 한눈에 파악.
 * - virtual — HGraphHubNode 가 키별 카운트 형식으로 override.
 * - HGraphCanvas._PopulateInternal 의 edges 루프 종료 직후 단일 호출 지점.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.11 — 포트 portName 기본값 부여 (Input/Output)
 *
 * # 변경
 * - _BuildPorts(): inputPort.portName = "Input", outputPort.portName = "Output".
 *   기존 "" 빈 문자열은 USS 라벨 노출 후 빈 공간으로 남아 도트 외관이 어색해짐.
 *
 * # 이유
 * - HGraphNode.uss 의 `.port > #type { display: none }` 전역 규칙 제거와 짝.
 *   SimpleNode / CatalogNode 가 본 베이스를 그대로 사용하므로 양쪽이 동시에 라벨을 표시.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.10 — 노드 선택 시 Inspector 동기화
 *
 * # 변경
 * - OnSelected(): Selection.activeObject = dataNode 추가.
 *   노드 클릭 시 대응 BaseNode ScriptableObject 가 Unity Inspector 에 표시됨.
 *
 * # 이유
 * - GraphView 선택 상태와 Unity 전역 Selection 이 분리되어 있어 Inspector 에 아무것도 표시되지 않았음.
 *   단 한 줄로 Inspector 동기화를 달성할 수 있는 가장 직접적인 진입점.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.10 Phase 3+ — 동적 출력 포트 확장점 (inputPort/outputPort/portRow protected, _BuildPorts virtual)
 *
 * # 변경
 * - inputPort / outputPort / portRow : private → protected (파생 클래스 직접 접근용).
 * - _BuildPorts() : private → protected virtual → HGraphCatalogNode 가 override 가능.
 * - portRow : _BuildPorts 로컬 변수 → 클래스 필드 (파생 클래스에서 Add 가능).
 * - GetOutputPort(int index) public virtual 추가 : 기본 노드는 index 무관 outputPort 반환.
 *   CatalogNode 가 override 해 인덱스별 동적 포트 반환.
 *
 * # 이유
 * - CatalogNode 는 출구 포트 N개(연결선 수 + 1 스페어)를 동적으로 생성.
 *   파생 클래스가 _BuildPorts 를 override 해 portRow 에 포트를 추가/제거해야 함.
 * - HGraphCanvas 엣지 연결 시 GetOutputPort(index) 로 포트를 조회 → 단일 노드/다중 포트 노드 통합.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.10 Phase 3 — 파생 타입 확장점 추가 (sealed 제거)
 *
 * # 변경
 * - sealed 제거 → HGraphCatalogNode 등 도메인별 파생 타입 상속 허용.
 * - bodyArea: private → protected → 파생 타입이 body 콘텐츠 교체 가능.
 * - _OnHeaderMouseDown: 더블클릭 동작을 OnHeaderDoubleClick(protected virtual) 로 위임.
 *   기본 동작 = _ToggleExpanded (기존과 동일). 파생 타입은 override 로 교체.
 *
 * # 이유
 * - CatalogNode 는 헤더 더블클릭 시 foldout 토글 대신 카탈로그 전환 동작 필요.
 * - body 에 ObjectField 를 표시하려면 base._BuildBody 가 추가한 UID Label 을
 *   파생 타입에서 Clear 후 재구성 가능해야 함.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.10 포트 위치 수정 + 타이틀 헤더 이동
 *
 * # 변경
 * - _BuildPorts(): inputContainer/outputContainer → mainContainer 직속 portRow 로 이동.
 *   mainContainer.Insert(1, portRow) — headerBar(0) 바로 다음, 바디(extensionContainer) 위.
 * - _ForcePortContainersVisible() 헬퍼 및 4개 호출 지점(생성자/ApplyEditorState/CloseIfExpanded/_ToggleExpanded) 전부 제거.
 * - _BuildHeader(): column 레이아웃 전환. headerRow(화살표+타입명) + titleLabel 2행 구조.
 * - _BuildTitle() 메서드 제거. titleLabel 이 headerBar 안으로 이동.
 *
 * # 이유
 * - _ForcePortContainersVisible() 는 GraphView 내부 레이아웃 패스와 싸우는 임시 우회.
 *   portRow → mainContainer 이동이 닫힌 노드 엣지 어긋남 이슈의 구조적 해결.
 *   mainContainer 는 GraphView 의 expanded 상태 변화 영향 밖 → worldBound 항상 유효.
 * - 타이틀을 headerBar 안으로 이동해 닫힘/열림 무관 노드 정보를 헤더에 집중.
 *
 * @Jason - PKH 2026.05.10 닫힌 노드 포트 항상 표시 + 엣지 worldBound 보장
 *
 * # 변경
 * - _ForcePortContainersVisible() 헬퍼 추가.
 *   RefreshExpandedState() 직후 호출로 top / inputContainer / outputContainer 를 Flex 강제 복원.
 *   RefreshExpandedState() 4개 호출 지점(생성자 / ApplyEditorState / CloseIfExpanded / _ToggleExpanded) 모두 적용.
 * - 이전 RefreshExpandedState() override 제거 (CS0506 — Node 베이스에서 virtual 미노출).
 * - Public - GraphView Override 영역 이름에서 "(Phase 1-E 실시간 스냅)" 접미 제거.
 *
 * # 이유
 * - GraphView base.RefreshExpandedState() 가 collapsed 시 top 컨테이너를 숨김.
 *   포트가 hidden 부모 안에 있으면 worldBound = {0,0,0,0} → 엣지가 (0,0)으로 잘못 연결.
 *   (Repopulate 후 닫힌 노드 엣지 어긋남 이슈의 직접 원인)
 * - 닫힌 상태에서도 포트 dot 를 표시해 연결 상태 시각 확인 + 드래그 연결 가능.
 *
 * @Jason - PKH 2026.05.10 Phase 2 — Port 추가 (inputPort / outputPort)
 *
 * # 추가
 * - _BuildPorts(): InputPort(Direction.Input) + OutputPort(Direction.Output) 생성.
 *   Orientation.Horizontal, Capacity.Multi, portName="" (레이블 숨김).
 *   inputContainer(좌) / outputContainer(우) 에 각각 Add.
 * - InputPort / OutputPort public 프로퍼티 노출 (HGraphCanvas 포트 연결용).
 *
 * # 이유
 * - HGraphCanvas._PopulateInternal 이 HGraphEdge.input / output 에 포트를 할당해야 함.
 * - portName="" 로 레이블 숨겨 컴팩트 노드 외관 유지.
 *
 * =============================================================================
 */

// =============================================================================
// (이하 이전 엔트리 — 원래 형식 보존)
// =============================================================================
// @Jason - PKH 2026-04-24 HGraphNode 의 역할 - BaseNode 1개에 대응하는 시각 객체
//
//   [역할]
//   - catalog.Nodes 의 BaseNode 1개 = HGraphNode VisualElement 1개.
//   - GraphView.Node 상속으로 Manipulator 자동 인식 (Selection·Drag·RectSelect).
//   - 도메인 데이터와 시각 레이어를 이어주는 얇은 어댑터.
//
//   [Experimental API 어댑터 경계 2파일 확장]
//   - L1 에서는 HGraphCanvas.cs 1파일이 유일한 Experimental using 지점이었음.
//   + Phase 1-A 에서 HGraphNode.cs 도 Experimental.GraphView.Node 상속 필수.
//   + 원칙 위반이 아닌 예외적 확장 (Q3 A 안 채택 - 대안 비용 폭증 때문).
//   + grep 회귀 가드: "UnityEditor.Experimental" 참조가 이 2파일로만 국한.
//
//   [UI 구조]
//   - mainContainer (GraphView.Node 내장) 에:
//     - _headerBar (VisualElement, 상단 컬러 헤더) - 클래스명 표시
//     - _titleLabel (Label, 본체 제목) - BaseNode.Title 표시
//   - 배경/모서리/색은 HGraphNode.uss 에서 정의.
//
//   [USS 로드 전략]
//   - L1 HGraphCanvas 와 동일 방식: AssetDatabase.FindAssets 이름 기반 검색.
//   + UPM 이전·리네임·경로 이동에 전부 생존.
//   + 누락 시 GraphView 기본 외형 fallback (경고 로그 없음 - 노드마다 경고 스팸 방지).
//
//   [BaseNode 참조 저장]
//   - dataNode 필드로 저장, DataNode 프로퍼티로 외부 조회.
//   + Phase 1-D 우클릭 메뉴에서 "이 GUI 가 어떤 data node 에 대응" 즉시 조회.
//   + UID 는 dataNode.UID 위임.
//
//   [도메인 서브 확장]
//   - 헤더 텍스트: dataNode.GetType().Name (예: "SimpleNode", 미래 "DialogueNode").
//   - 헤더 색: HGraphNodeStyles.GetHeaderColorFor(type) - Phase 1-A 는 기본색, 확장은 stub.
//
//   [Phase 1-B 예고]
//   - Foldout 열림 시 _headerBar 아래 Body 영역 추가.
//   - 노드 개별 Open size 를 HGraphNode 의 필드로 보유하거나 catalog 의 보조 맵에 저장 (Phase 1-B 결정).
//
//   [Phase 1-B 확장 - 2026-05-07]
//   - extensionContainer 에 bodyArea 추가 + GraphView.Node 의 expanded 활성화 (P1B-a, d).
//   + 닫힘 시 GraphView 내장 hide 동작으로 자연 숨김. Phase 1-A 에서 호출만 하던
//     RefreshExpandedState 의 hook 가 본 Phase 에서 활성화됨.
//   - 토글 진입점 2종 (P1B-e A+B 둘 다 활성):
//   + (1) 헤더 좌측 ▶/▼ 아이콘 클릭 (toggleArrow.MouseDown 캡처)
//   + (2) 헤더 더블클릭 (headerBar.MouseDown clickCount==2 캡처)
//   + 두 진입점 모두 _ToggleExpanded() 통과 → expanded 토글 + ApplyOpenSize + 이벤트.
//   - openSize 필드 + ApplyEditorState + _ApplyOpenSize:
//   + HGraphCanvas Populate 가 catalog.EditorNodeOpenSizes 에서 읽어 ApplyEditorState 호출.
//   + 열림 + openSize 보유 시 style.width/height 명시. 닫힘 시 StyleKeyword.Null 로 USS 복귀 (P1B-f).
//   - resizeHandle (Task F 의존):
//   + 본 Task D 에선 VisualElement + USS 클래스 + 가시성 토글만. Manipulator 동작은 Task F.
//   + 열림 시 PickingMode.Position + display:Flex, 닫힘 시 Ignore + display:None (P1B-h).
//   - FoldoutChanged / OpenSizeChanged 이벤트:
//   + HGraphCanvas 가 구독해 Author.SetFoldoutOpen / SetOpenSize 호출.
//   + Phase 1-A 의 graphViewChanged 패턴과 같은 책임 분리. HGraphNode 는 자기 상태만,
//     catalog 갱신은 Canvas 가 단일 진입점.
//   - NotifyResizeFinished (internal):
//   + Task F 의 Resize Manipulator 가 MouseUp 시점에 호출. openSize 갱신 + OpenSizeChanged 발화.
//   + Task D 시점에 시그니처 미리 노출해 Task F 가 한 줄 호출만 추가하면 동작.
//
//   [Phase 1-D 추가 - 2026-05-07]
//   - Catalog internal property — HGraphCanvas Populate 시점에 currentCatalog 주입.
//   + BuildContextualMenu 의 핸들러가 catalog 인자 조회 (HGraphClipboard.Serialize 호출용 — 2026-05-08 갱신).
//   + Phase 1-A 의 책임 분리 정합 — HGraphNode 는 자기 상태만, Author 호출은 핸들러에서.
//   - BuildContextualMenu override (P1D-a):
//   + GraphView ContextualMenuManipulator 자동 호출. ESC/외부 클릭 자동 닫힘.
//   + 메뉴 4 항목 (복사 / 복제 / 루트 재설정 / 삭제) + destructive 마지막 + 구분선.
//   + 이미 root 인 노드의 "루트 재설정" 은 DropdownMenuAction.Status.Disabled (P1D-b).
//   - capabilities 플래그 차단 (Copiable | Deletable):
//   + GraphView 기본 메뉴 Cut/Copy/Duplicate/Delete 자동 추가 막음. 우리 커스텀 항목과 중복 회피.
//   + Copiable 하나가 Cut + Copy + Duplicate 셋 통제 (Cut = Copy + Delete 합성, Duplicate = 내부 paste).
//   + 키보드 단축키 (Delete 키 / Ctrl+D / Ctrl+C) 도 같이 차단 — Phase 1-E 단축키 도입 시 라우팅.
//
//   [Phase 1-D Cut/Paste 확장 - 2026-05-08]
//   - 메뉴 6 항목으로 확장 (잘라내기 + 붙여넣기 추가): 복사 / 잘라내기 / 붙여넣기 / 복제 / 루트 재설정 / 삭제.
//   - Copy 의미 변경 — Phase 1-D 본 라운드의 ToString 텍스트 → JSON 직렬 (사용자 결정).
//   + Cut 과 같은 HGraphClipboard.Serialize 호출. 차이는 "원본 유지 vs 제거" 한 가지뿐.
//   + Paste 가 Copy/Cut 둘 다 처리 — 다른 catalog 로 데이터 이전 호환 보장.
//   - Cut/Paste 핸들러:
//   + Copy / Cut / Paste 모두 HGraphCanvas 의 helper (CopyNodes/CutNodes/PasteFromClipboard) 위임.
//   + 같은 helper 가 키보드 단축키 (Ctrl+C/X/V) 진입점도 공유 — DRY.
//   + Paste 메뉴 활성/비활성 = HGraphClipboard.IsValid(systemCopyBuffer) — magic 패턴 검사.
//   - 다중 선택 처리 (Q7): _GetEffectiveTargets 헬퍼.
//   + selection 이 본 노드 포함하면 selection 일괄, 미포함이면 본 노드 1 개만.
//   + Cut/Copy/Duplicate/Delete 다중 일괄, 루트 재설정은 단일만 (다중 시 Disabled).
//   + GraphView 우클릭 시 selection 변경 X — 우클릭 노드가 selection 미포함 가능, fallback 처리.
//   - Mixed 도메인 selection (Cut/Copy 시) → HGraphClipboard.Serialize 가 거부 → 핸들러 Warning.
//   - 4 핸들러 모두 NodeCatalogAuthor 호출 + 실패 시 HLogger.Warning:
//   + 복사 (시점 시 ToString 텍스트, 2026-05-08 갱신으로 HGraphClipboard.Serialize JSON 으로 통합 — 아래 Cut/Paste 엔트리 참조).
//   + 복제: NodeCatalogAuthor.DuplicateNode<BaseNode> (도메인 데이터 + 새 UID + 위치 offset).
//   + 루트 재설정: NodeCatalogAuthor.SetRoot — 1-C 의 임시 [Set as Root] 정식 이양.
//   + 삭제: NodeCatalogAuthor.RemoveNode — cascade 자동 (3 보조 맵 + 엣지 모두 정리).
//
//   [Phase 1-B-3 Task G 추가 - 2026-05-07]
//   - OnSelected / OnUnselected override (P1B-j, k):
//   + GraphElement 의 가상 메서드. GraphView.AddToSelection / RemoveFromSelection 시 자동 호출.
//   + .hgraph-node--selected USS 클래스 토글로 외곽선 색 변경 (border-color #6FA5C5 + width 2px).
//   - P1B-k 환경 검증 결과:
//   + GraphView 의 OnSelected/OnUnselected 자동 호출 → fallback (selectionChanged 콜백) 불필요.
//   + 다중 선택 / RectangleSelector / 키보드 선택 모두 같은 경로로 처리됨.
//   + base 호출 유지 — Unity internal selection 추적이 끊어지면 selection 상태 disconnect.
//   - 후속:
//   + "선택 시 연결점 + 연결 노드 동시 강조" 는 Phase 2 엣지 시각화 의존.
//     본 Task G 는 노드 자체 외곽선만 (milestone "노드 하이라이트 - 시점 이동 아님" 의 첫 단계).
//   + USS specificity 충돌 시 micro-iteration: .hgraph-node.hgraph-node--selected 합성 selector
//     또는 :hover/:checked pseudo 로 selectivity 상향.
//
//   [Phase 1-B-2 Task F 추가 - 2026-05-07]
//   - 인라인 Resize Manipulator 구현. resizeHandle 에 MouseDown/Move/Up 직접 등록.
//     + Manipulator 별도 클래스 분리 X (Phase 1-A 의 toggle/header 패턴과 일관). 1-D/E 에서
//       패턴 반복 보일 때 추출 후보.
//   - 핵심 — capture + StopPropagation 둘 다 필수:
//     + StopPropagation 만 두면 capture 가 풀려 mouse 가 노드 밖으로 나가면 추적 끊김.
//     + capture 만 잡으면 capture-phase 에서 GraphView SelectionDragger 가 먼저 받음.
//     + 두 가지 안 갖추면 1-B-1 시점에 보고된 "resize handle 드래그가 노드 위치 변경 발동"
//       증상 재현. UI Toolkit 의 propagation 모델 자체에서 비롯된 함정.
//   - zoom 보정 (delta / scale):
//     + evt.mousePosition 은 panel 좌표 (zoom 영향 X), style.width/height 는 그래프 좌표.
//     + zoom out 0.5x 에서 panel 100px 드래그 = 그래프 200px size 증가 = 사용자 시야 100px.
//     + 보정 없으면 zoom out 일 때 사용자 드래그 거리보다 적게 늘어남 (시야상 절반).
//   - MIN_RESIZE_WIDTH / MIN_RESIZE_HEIGHT — USS .hgraph-node 의 min-width / min-height 와 정합.
//     + USS 와 C# 양쪽이 동일 최소값 강제. 한쪽만 변경하면 어색한 동작 가능 — 변경 시 동기 필수.
//   - MouseUp 시점에만 NotifyResizeFinished → OpenSizeChanged → Author.SetOpenSize (P1B-i).
//     + 드래그 중 매 프레임 호출 X. SetDirty 폭주 회피.
//     + 노드 인스턴스가 사라져도 (RemoveElement) 람다 자기 캡처 currentCatalog 가 stale 될 수 있으나,
//       capture 가 풀린 시점이라 호출 경로 자체가 발생 안 함.
// =============================================================================
//
// [Phase 5 - 2026-05-11] _OnContextSetAsRoot → canvas.SetSelectedAsRoot(UID) 위임.
//   이유 / Set as Root 로직(Author.SetRoot) 을 HGraphCanvas.SetSelectedAsRoot 에 통합.
//          메뉴바 진입점과 우클릭 메뉴 진입점이 같은 Author.SetRoot 경유 — DRY.
//          명시 UID 오버로드 사용: 우클릭 시 GraphView selection 갱신 X 이므로 this.UID 직접 전달.
//          canvas null fallback 유지 — Panel 밖에서 호출되는 극단 케이스 방어.
//
// [Phase 1-F CloseIfExpanded 추가 - 2026-05-09]
// - Internal - Foldout State (Phase 1-F) 영역 신설:
//   + IsExpanded: expanded 프로퍼티 노출 — HGraphCanvas.CloseAllFoldouts 에서 일괄 확인용.
//   + CloseIfExpanded(): 이미 닫혀 있으면 early return. 열린 경우에만 닫힘 상태로 전환.
//     ApplyEditorState 와 같은 닫힘 경로 (toggleArrow 업데이트 + RefreshExpandedState +
//     _ApplyOpenSize + _ApplyResizeHandleVisibility + FoldoutChanged?.Invoke(false)).
//   + FoldoutChanged.Invoke(false) 발화 — HGraphCanvas 의 catalog 갱신 경로 자동 트리거.
//
// [Phase 1-E SetPosition override + _ApplySnap - 2026-05-08]
// - SetPosition override (P1E-α/β/γ + Q5 A + Q6 A):
//   + SelectionDragger 매 frame 호출 → quantize 삽입. 클립보드/키보드/프로그래매틱 이동 등
//     모든 위치 변경 경로가 단일 override 통과.
//   + NodeSnapSettings.Mode + Event.current.shift 분기 — Off/OnShiftHold/Always.
//   + Mathf.Round 좌상단 기준 (milestone §1-2-3).
//   + GridUnit <= 0 시 quantize skip (P1E-4 DivByZero 가드).
//   + 다중 선택 그룹 일괄 스냅 미적용 — 각 노드 개별 quantize (Q8 A). 노드 간 상대 위치가
//     매 frame 미세 변화 가능 — Phase 1-E Round 2 anchor 노드 기준 delta 적용으로 진화 여지.
// =============================================================================
#endif

using System;
using HWindows.NodeWindow;
using HWindows.NodeWindow.Identity;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace HWindows.Editor.NodeWindow {
    public sealed class HGraphNode : Node {
        #region Const
        const string USS_ASSET_NAME = "HGraphNode";
        const string ARROW_OPEN = "▼";
        const string ARROW_CLOSED = "▶";
        // Resize 최소값 — USS .hgraph-node 의 min-width/min-height 와 정합 유지.
        const float MIN_RESIZE_WIDTH = 180f;
        const float MIN_RESIZE_HEIGHT = 64f;
        #endregion

        #region Fields
        readonly BaseNode dataNode;
        readonly bool isRoot;
        VisualElement headerBar;
        Label titleLabel;
        Label toggleArrow;
        VisualElement bodyArea;
        VisualElement resizeHandle;
        Vector2 openSize;
        bool isResizing;
        Vector2 resizeStartMousePos;
        Vector2 resizeStartSize;
        #endregion

        #region Properties
        public BaseNode DataNode => dataNode;
        public NodeUID UID => dataNode.UID;
        public bool IsRoot => isRoot;
        public VisualElement ResizeHandle => resizeHandle;
        public Vector2 OpenSize => openSize;
        #endregion

        #region Events
        // Foldout 토글 시 발화. HGraphCanvas 가 구독해 Author.SetFoldoutOpen 호출.
        public event Action<bool> FoldoutChanged;
        // Resize 종료 (MouseUp) 시 발화. HGraphCanvas 가 구독해 Author.SetOpenSize 호출.
        public event Action<Vector2> OpenSizeChanged;
        #endregion

        #region Constructor
        public HGraphNode(BaseNode dataNode, bool isRoot = false) {
            this.dataNode = dataNode;
            this.isRoot = isRoot;
            this.openSize = Vector2.zero;

            _LoadStyleSheet();
            AddToClassList("hgraph-node");

            _BuildHeader();
            _BuildTitle();
            _BuildBody();
            _BuildResizeHandle();

            expanded = false;
            RefreshExpandedState();
            RefreshPorts();
        }
        #endregion

        #region Public - Editor State
        // HGraphCanvas Populate 가 catalog 에서 읽은 상태를 노드에 적용.
        public void ApplyEditorState(bool isExpanded, Vector2 openSize) {
            this.openSize = openSize;
            expanded = isExpanded;
            if (toggleArrow != null) toggleArrow.text = _GetToggleSymbol();
            RefreshExpandedState();
            _ApplyOpenSize();
            _ApplyResizeHandleVisibility();
        }

        // Resize Manipulator (Task F) 가 MouseUp 시점에 호출. openSize 갱신 + 이벤트 발화.
        internal void NotifyResizeFinished(Vector2 newSize) {
            openSize = newSize;
            OpenSizeChanged?.Invoke(newSize);
        }
        #endregion

        #region Public - Selection (Phase 1-B-3)
        // GraphElement 가상 메서드. GraphView.AddToSelection / RemoveFromSelection 시 자동 호출 (P1B-k).
        // base 호출 유지 — Unity 의 internal selection 추적이 끊어지면 selection 상태 disconnect.
        public override void OnSelected() {
            base.OnSelected();
            AddToClassList("hgraph-node--selected");
        }

        public override void OnUnselected() {
            base.OnUnselected();
            RemoveFromClassList("hgraph-node--selected");
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

            // 토글 진입점 (1) — 헤더 좌측 ▶/▼ 아이콘 클릭 (P1B-e A).
            toggleArrow = new Label(_GetToggleSymbol());
            toggleArrow.AddToClassList("hgraph-node-toggle-arrow");
            toggleArrow.RegisterCallback<MouseDownEvent>(_OnToggleArrowMouseDown);
            headerBar.Add(toggleArrow);

            string headerText = isRoot
                ? $"{dataNode.GetType().Name}  [ROOT]"
                : dataNode.GetType().Name;
            Label headerLabel = new Label(headerText);
            headerBar.Add(headerLabel);

            // 토글 진입점 (2) — 헤더 더블클릭 (P1B-e B). toggleArrow 위 클릭은 (1) 이 먼저 잡음.
            headerBar.RegisterCallback<MouseDownEvent>(_OnHeaderMouseDown);

            mainContainer.Insert(0, headerBar);
        }

        private void _BuildTitle() {
            titleLabel = new Label(dataNode.Title);
            titleLabel.AddToClassList("hgraph-node-title");
            mainContainer.Add(titleLabel);
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

        private void _BuildResizeHandle() {
            resizeHandle = new VisualElement();
            resizeHandle.AddToClassList("hgraph-node-resize-handle");
            // 인라인 Resize Manipulator (Phase 1-B-2, P1B-g/h/i).
            // MouseDown 시 capture + StopPropagation 으로 GraphView SelectionDragger 차단 필수.
            resizeHandle.RegisterCallback<MouseDownEvent>(_OnResizeHandleMouseDown);
            resizeHandle.RegisterCallback<MouseMoveEvent>(_OnResizeHandleMouseMove);
            resizeHandle.RegisterCallback<MouseUpEvent>(_OnResizeHandleMouseUp);
            Add(resizeHandle);
            _ApplyResizeHandleVisibility();
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
            _ToggleExpanded();
            evt.StopPropagation();
        }

        private void _ToggleExpanded() {
            expanded = !expanded;
            if (toggleArrow != null) toggleArrow.text = _GetToggleSymbol();
            RefreshExpandedState();
            _ApplyOpenSize();
            _ApplyResizeHandleVisibility();
            FoldoutChanged?.Invoke(expanded);
        }

        private void _ApplyOpenSize() {
            if (expanded && openSize.x > 0f && openSize.y > 0f) {
                style.width = openSize.x;
                style.height = openSize.y;
            } else {
                // 닫힘 시 또는 openSize 미보유 시 USS min-width/min-height 로 자연 복귀 (P1B-f).
                style.width = StyleKeyword.Null;
                style.height = StyleKeyword.Null;
            }
        }

        private void _ApplyResizeHandleVisibility() {
            if (resizeHandle == null) return;
            if (expanded) {
                resizeHandle.pickingMode = PickingMode.Position;
                resizeHandle.style.display = DisplayStyle.Flex;
            } else {
                resizeHandle.pickingMode = PickingMode.Ignore;
                resizeHandle.style.display = DisplayStyle.None;
            }
        }
        #endregion

        #region Private - Resize (Phase 1-B-2)
        private void _OnResizeHandleMouseDown(MouseDownEvent evt) {
            if (evt.button != 0) return;
            if (!expanded) return;

            isResizing = true;
            resizeStartMousePos = evt.mousePosition;
            resizeStartSize = new Vector2(
                resolvedStyle.width > 0f ? resolvedStyle.width : MIN_RESIZE_WIDTH,
                resolvedStyle.height > 0f ? resolvedStyle.height : MIN_RESIZE_HEIGHT);

            // capture + StopPropagation 둘 다 필수.
            // capture 만 잡으면 capture-phase 에서 GraphView SelectionDragger 가 먼저 처리 가능.
            // StopPropagation 만 두면 capture 가 풀려 외부 mouse 이동 추적 끊김.
            resizeHandle.CaptureMouse();
            evt.StopPropagation();
        }

        private void _OnResizeHandleMouseMove(MouseMoveEvent evt) {
            if (!isResizing) return;

            // panel 좌표 delta 를 그래프 좌표로 변환 (zoom 보정).
            // panel 1px 이동 = 그래프 (1 / scale) px 이동. scale=1 일 때 그대로.
            float scale = _GetGraphViewScale();
            Vector2 panelDelta = (Vector2)evt.mousePosition - resizeStartMousePos;
            Vector2 graphDelta = panelDelta / scale;

            float newWidth = Mathf.Max(MIN_RESIZE_WIDTH, resizeStartSize.x + graphDelta.x);
            float newHeight = Mathf.Max(MIN_RESIZE_HEIGHT, resizeStartSize.y + graphDelta.y);
            style.width = newWidth;
            style.height = newHeight;

            evt.StopPropagation();
        }

        private void _OnResizeHandleMouseUp(MouseUpEvent evt) {
            if (!isResizing) return;
            isResizing = false;

            resizeHandle.ReleaseMouse();

            // 최종 크기로 catalog 갱신 이벤트 발화 (P1B-i).
            Vector2 finalSize = new Vector2(resolvedStyle.width, resolvedStyle.height);
            NotifyResizeFinished(finalSize);

            evt.StopPropagation();
        }

        // GraphView 의 viewTransform.scale 을 부모 traversal 로 조회. zoom 0 인 경우 1 fallback.
        private float _GetGraphViewScale() {
            GraphView gv = GetFirstAncestorOfType<GraphView>();
            if (gv == null) return 1f;
            float scale = gv.viewTransform.scale.x;
            return scale > 0f ? scale : 1f;
        }
        #endregion
    }
}

#if UNITY_EDITOR
// =============================================================================
// Dev Log
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
#endif

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- HWindows NodeWindow 진입점 EditorWindow. 메뉴바 + 툴바 + 카탈로그 바인드.
 *
 * 특징 ::
 * [View ▾] / [Edit ▾] 메뉴바: Go To Root(Ctrl+Home) / Close All(Ctrl+0) / Select All / Set as Root.
 * + ObjectField(catalogField)로 카탈로그 선택 · 파일 확인 · 피커 통합.
 * + canvas 드래그드롭: 카탈로그 미바인드 → 바인드, 이미 바인드 → CatalogNode 자동 생성.
 * + CatalogNode 더블클릭 → canvas.CatalogSwitchRequested → _BindCatalog 카탈로그 전환.
 * + 타이틀 검색: searchField 타이핑 → 첫 결과 이동, Enter → 다음 결과 순환, ESC → 초기화.
 *
 * 주의사항 ::
 * currentCatalog 미직렬화 — 창 재오픈 시 참조 유실은 의도된 동작.
 * catalogField.SetValueWithoutNotify / searchField.SetValueWithoutNotify — valueChanged 재진입 차단.
 * =========================================================
 */
#endif
using HDiagnosis.Logger;
using HWindows.Editor.NodeWindow.Authoring;
using HWindows.Editor.NodeWindow.Settings;
using HWindows.NodeWindow;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace HWindows.Editor.NodeWindow {
    public sealed class HGraphWindow : EditorWindow {
        #region Menu
        [MenuItem("Window/HWindows/Node Window/Graph Editor")]
        public static void Open() {
            HGraphWindow window = GetWindow<HGraphWindow>();
            window.titleContent = new GUIContent("Graph Editor");
            window.minSize = new Vector2(400, 300);
        }
        #endregion

        #region Fields
        NodeCatalogSO currentCatalog;
        HGraphCanvas canvas;
        IMGUIContainer settingsPanel;
        ObjectField catalogField;
        Label viewportCenterLabel;
        TextField searchField;
        Label searchCountLabel;
        #endregion

        #region Unity Lifecycle
        private void CreateGUI() {
            VisualElement root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Column;
            root.Add(_BuildMenuBar());
            root.Add(_BuildToolbar());
            root.Add(_BuildContentRow());
            _WireUpEvents();
            _InitialBind();
        }

        private void _OnCatalogMutated(NodeCatalogSO mutated) {
            if (currentCatalog == null || mutated != currentCatalog) return;
            Repaint();
        }

        private void OnDisable() {
            NodeCatalogAuthor.CatalogMutated -= _OnCatalogMutated;
        }

        private void _UpdateViewportCenterLabel() {
            if (canvas == null || viewportCenterLabel == null) return;
            Vector2 center = canvas.GetViewportCenterWorld();
            viewportCenterLabel.text = $"View: ({center.x:F0}, {center.y:F0})";
        }
        #endregion

        #region Build — GUI 빌드 헬퍼
        private VisualElement _BuildMenuBar() {
            VisualElement menuBar = new VisualElement();
            menuBar.style.flexDirection = FlexDirection.Row;
            menuBar.style.height = 22;
            menuBar.style.backgroundColor = new StyleColor(new Color(0.165f, 0.165f, 0.165f));
            menuBar.style.paddingLeft = 2;
            menuBar.style.alignItems = Align.Center;
            menuBar.Add(_BuildViewMenu());
            menuBar.Add(_BuildEditMenu());
            return menuBar;
        }

        private ToolbarMenu _BuildViewMenu() {
            ToolbarMenu viewMenu = new ToolbarMenu { text = "View" };
            viewMenu.menu.AppendAction(
                "Go To Root                Ctrl+Home",
                _ => _GoToRoot(),
                _ => currentCatalog != null
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled);
            viewMenu.menu.AppendAction(
                "Close All Foldouts        Ctrl+0",
                _ => _CloseAllFoldouts(),
                _ => currentCatalog != null
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled);
            return viewMenu;
        }

        private ToolbarMenu _BuildEditMenu() {
            ToolbarMenu editMenu = new ToolbarMenu { text = "Edit" };
            editMenu.menu.AppendAction(
                "Select All                Ctrl+A",
                _ => _OnMenuSelectAll(),
                _ => currentCatalog != null
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled);
            editMenu.menu.AppendAction(
                "Set as Root",
                _ => _OnMenuSetAsRoot(),
                _GetEditMenuStatus_SetAsRoot);
            return editMenu;
        }

        private VisualElement _BuildToolbar() {
            VisualElement toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.height = 24;
            toolbar.style.backgroundColor = new StyleColor(new Color(0.22f, 0.22f, 0.22f));
            toolbar.style.paddingLeft = 4;
            toolbar.style.paddingRight = 4;
            toolbar.style.alignItems = Align.Center;

            toolbar.Add(_CreateCatalogField());
            toolbar.Add(_CreateSearchField());
            toolbar.Add(_CreateSearchCountLabel());
            toolbar.Add(_CreateViewportCenterLabel());
            toolbar.Add(_CreateSettingsToggle());
            return toolbar;
        }

        private ObjectField _CreateCatalogField() {
            catalogField = new ObjectField {
                objectType = typeof(NodeCatalogSO),
                allowSceneObjects = false,
                label = ""
            };
            catalogField.style.flexGrow = 1;
            catalogField.style.marginLeft = 8;
            catalogField.style.marginRight = 8;
            catalogField.RegisterValueChangedCallback(evt => _BindCatalog(evt.newValue as NodeCatalogSO));
            return catalogField;
        }

        private Label _CreateViewportCenterLabel() {
            viewportCenterLabel = new Label("View: (0, 0)");
            viewportCenterLabel.style.width = 128;
            viewportCenterLabel.style.flexShrink = 0;
            viewportCenterLabel.style.marginLeft = 6;
            viewportCenterLabel.style.marginRight = 8;
            viewportCenterLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            viewportCenterLabel.style.color = new StyleColor(new Color(0.55f, 0.75f, 0.55f));
            return viewportCenterLabel;
        }

        private TextField _CreateSearchField() {
            searchField = new TextField();
            searchField.style.width = 150;
            searchField.style.marginLeft = 8;
            searchField.style.marginRight = 2;
            searchField.tooltip = "노드 타이틀 검색 (Enter: 다음 결과, Esc: 초기화)";
            searchField.RegisterValueChangedCallback(evt => _OnSearchValueChanged(evt.newValue));
            searchField.RegisterCallback<KeyDownEvent>(_OnSearchKeyDown);
            return searchField;
        }

        private Label _CreateSearchCountLabel() {
            searchCountLabel = new Label(string.Empty);
            searchCountLabel.style.minWidth = 36;
            searchCountLabel.style.marginRight = 6;
            searchCountLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            searchCountLabel.style.color = new StyleColor(new Color(0.8f, 0.78f, 0.35f));
            return searchCountLabel;
        }

        private ToolbarToggle _CreateSettingsToggle() {
            ToolbarToggle toggle = new ToolbarToggle { text = "Settings" };
            // settingsPanel 은 _BuildContentRow 에서 초기화됨.
            // 람다는 필드 레퍼런스를 늦게 평가하므로 클릭 시점에 항상 유효.
            toggle.RegisterValueChangedCallback(evt => {
                settingsPanel.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            });
            return toggle;
        }

        private VisualElement _BuildContentRow() {
            canvas = new HGraphCanvas();
            canvas.style.flexGrow = 1;

            settingsPanel = new IMGUIContainer(_OnSettingsPanelGUI);
            settingsPanel.style.display = DisplayStyle.None;
            settingsPanel.style.width = 280;
            settingsPanel.style.flexShrink = 0;
            settingsPanel.style.borderLeftWidth = 1;
            settingsPanel.style.borderLeftColor = new StyleColor(new Color(0.15f, 0.15f, 0.15f));

            VisualElement contentRow = new VisualElement();
            contentRow.style.flexDirection = FlexDirection.Row;
            contentRow.style.flexGrow = 1;
            contentRow.Add(canvas);
            contentRow.Add(settingsPanel);
            return contentRow;
        }

        private void _WireUpEvents() {
            canvas.viewTransformChanged = _ => _UpdateViewportCenterLabel();
            _RegisterDragDropCallbacks();
            canvas.CatalogSwitchRequested += _BindCatalog;
            NodeCatalogAuthor.CatalogMutated += _OnCatalogMutated;
        }

        private void _InitialBind() {
            canvas.Bind(currentCatalog);
            catalogField.SetValueWithoutNotify(currentCatalog);
            _UpdateViewportCenterLabel();
        }
        #endregion

        #region Toolbar Actions
        private void _GoToRoot() {
            if (canvas == null) return;
            if (currentCatalog == null) {
                HLogger.Warning("[HGraphWindow] Go To Root rejected: no catalog bound.");
                return;
            }
            if (!canvas.GoToRoot()) {
                HLogger.Warning("[HGraphWindow] Go To Root: catalog has no root node.");
            }
        }

        private void _CloseAllFoldouts() {
            if (canvas == null) return;
            canvas.CloseAllFoldouts();
        }
        #endregion

        #region MenuBar Actions (Phase 5)
        private void _OnMenuSelectAll() {
            if (canvas == null || currentCatalog == null) return;
            canvas.SelectAllNodes();
        }

        private void _OnMenuSetAsRoot() {
            if (canvas == null || currentCatalog == null) return;
            canvas.SetSelectedAsRoot();
        }

        // Set as Root 동적 status callback — 메뉴 열 때마다 재평가.
        // 단일 노드 선택 + non-root 인 경우에만 Normal, 그 외 Disabled.
        private DropdownMenuAction.Status _GetEditMenuStatus_SetAsRoot(DropdownMenuAction action) {
            if (canvas == null || currentCatalog == null) return DropdownMenuAction.Status.Disabled;
            HGraphNode node = canvas.GetSingleSelectedHGraphNode();
            if (node == null) return DropdownMenuAction.Status.Disabled;
            if (node.UID == currentCatalog.RootUID) return DropdownMenuAction.Status.Disabled;
            return DropdownMenuAction.Status.Normal;
        }
        #endregion

        #region Search (Phase 4)
        private void _OnSearchValueChanged(string query) {
            if (canvas == null) return;
            if (string.IsNullOrEmpty(query)) {
                canvas.ClearSearch();
                searchCountLabel.text = string.Empty;
                return;
            }
            var (count, current) = canvas.SearchNodes(query);
            searchCountLabel.text = count > 0 ? $"{current}/{count}" : "0";
        }

        private void _OnSearchKeyDown(KeyDownEvent evt) {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) {
                if (canvas == null) return;
                var (count, current) = canvas.AdvanceSearch();
                searchCountLabel.text = count > 0 ? $"{current}/{count}" : "0";
                evt.StopPropagation();
                return;
            }
            if (evt.keyCode == KeyCode.Escape) {
                searchField.SetValueWithoutNotify(string.Empty);
                canvas?.ClearSearch();
                searchCountLabel.text = string.Empty;
                evt.StopPropagation();
            }
        }

        private void _ClearSearchUI() {
            if (searchField != null) searchField.SetValueWithoutNotify(string.Empty);
            if (searchCountLabel != null) searchCountLabel.text = string.Empty;
        }
        #endregion

        #region Settings Panel
        private void _OnSettingsPanelGUI() {
            NodeWindowSettingsProvider.DrawSettingsGUI(string.Empty);
        }
        #endregion

        #region DragDrop
        private void _RegisterDragDropCallbacks() {
            // canvas 영역에만 등록 — ObjectField 드래그드롭과 충돌 방지.
            canvas.RegisterCallback<DragUpdatedEvent>(_OnDragUpdated);
            canvas.RegisterCallback<DragPerformEvent>(_OnDragPerform);
        }

        private void _OnDragUpdated(DragUpdatedEvent evt) {
            if (DragAndDrop.objectReferences.Length == 0) return;
            Object obj = DragAndDrop.objectReferences[0];
            if (obj is NodeCatalogSO) {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.StopPropagation();
            }
        }

        private void _OnDragPerform(DragPerformEvent evt) {
            if (DragAndDrop.objectReferences.Length == 0) return;
            Object obj = DragAndDrop.objectReferences[0];
            if (obj is NodeCatalogSO catalog) {
                DragAndDrop.AcceptDrag();
                if (currentCatalog == null) {
                    // 카탈로그 미바인드 — 바인드 (최초 로드 경로).
                    _BindCatalog(catalog);
                } else if (currentCatalog != catalog) {
                    // 카탈로그 이미 바인드 + 다른 SO 드롭 → CatalogNode 자동 생성.
                    Vector2 dropPos = canvas.ToGraphPosition(evt.localMousePosition);
                    NodeCatalogAuthor.CreateCatalogNodeAt(currentCatalog, catalog, dropPos);
                }
                // currentCatalog == catalog: 동일 카탈로그 드롭 — 무반응.
                evt.StopPropagation();
            }
            else {
                HLogger.Warning(
                    $"[HGraphWindow] Drop rejected: not a NodeCatalogSO (got {obj?.GetType().Name ?? "null"})");
            }
        }
        #endregion

        #region Bind
        private void _BindCatalog(NodeCatalogSO catalog) {
            if (catalog == currentCatalog) return;
            currentCatalog = catalog;
            canvas.Bind(catalog);
            catalogField.SetValueWithoutNotify(catalog);
            _ClearSearchUI();
        }
        #endregion
    }
}

#if UNITY_EDITOR
// =============================================================================
// Dev Log
// =============================================================================
// @Jason - PKH 2026.05.11 Phase 5 — 메뉴바 신설 + Go To Root / Close All Toolbar 이관
//
//   변경 / _BuildMenuBar() 신설 (height=22, #2A2A2A).
//          _BuildViewMenu() : Go To Root (Ctrl+Home) + Close All Foldouts (Ctrl+0). catalog null 시 Disabled.
//          _BuildEditMenu() : Select All (Ctrl+A) + Set as Root. 각 동적 status callback 적용.
//          _GetEditMenuStatus_SetAsRoot : 단일 선택 + non-root 조건만 Normal.
//          CreateGUI : root.Add(_BuildMenuBar()) 를 _BuildToolbar 앞에 삽입.
//          _BuildToolbar : _CreateGoToRootButton / _CreateCloseAllButton 두 Add 제거.
//          _CreateGoToRootButton / _CreateCloseAllButton 메서드 삭제 — 메뉴바로 이관.
//   이유 / milestone §5 스펙: Go To Root + Close All 을 메뉴바로 이관. Select All / Set as Root 은
//          단축키/우클릭 만 진입 가능했던 기능의 메뉴바 비-컨텍스트 진입 경로 추가.
//   결과 / Toolbar 7 항목 → 5 항목 (CatalogField/Search/Count/ViewportLabel/Settings 잔존).
//          [View ▾] / [Edit ▾] 2 카테고리 풀다운 메뉴바 상단 추가.
//          ToolbarMenu.menu.AppendAction statusCallback 파라미터로 메뉴 열 때마다 동적 재평가.
//   주의 / _GetEditMenuStatus_SetAsRoot 는 canvas.GetSingleSelectedHGraphNode() 를 호출.
//          메뉴 열 때마다 selection 순회 (O(M)) — N=100 이하 실사용 범위에서 무시 가능.
//
// @Jason - PKH 2026.05.11 툴바 순서 조정 — 검색 앞, 좌표값 뒤 + viewportCenterLabel 고정 너비
//
//   변경 / _BuildToolbar: SearchField + SearchCountLabel 을 ViewportCenterLabel 앞으로 이동.
//          _CreateViewportCenterLabel: width=128, flexShrink=0, marginLeft=6 추가.
//   이유 / 검색 입력 필드가 카탈로그 ObjectField 바로 옆에 위치해야 UX 흐름이 자연스러움.
//          좌표값은 보조 정보이므로 우측 끝(Settings 토글 직전)에 배치.
//          flexShrink=0 미설정 시 flex 컨테이너가 레이블 너비를 임의 수축 가능.
//   결과 / 툴바 순서: GoToRoot / CloseAll / CatalogField / SearchField / SearchCount / ViewportCenter / Settings.
//          ViewportCenterLabel 은 항상 128px 고정 — 좌표 자릿수 변동에도 레이아웃 흔들림 없음.
//
// @Jason - PKH 2026.05.11 CreateGUI 분해 + CS1061 SetPlaceholderText 제거
//
//   변경 / CreateGUI 를 _BuildToolbar / _BuildContentRow / _WireUpEvents / _InitialBind 로 분해.
//          각 툴바 항목은 _CreateGoToRootButton / _CreateCloseAllButton / _CreateCatalogField /
//          _CreateViewportCenterLabel / _CreateSearchField / _CreateSearchCountLabel / _CreateSettingsToggle.
//          searchField.SetPlaceholderText("Search...") 제거 (CS1061: Unity 6000.3.11f1 미지원 API).
//   이유 / CreateGUI 가 단일 메서드에 툴바 항목 + 캔버스 + 이벤트 + 바인드 책임 혼재 → 기능별 분리.
//          _CreateXxx 는 해당 필드(catalogField 등)를 초기화하고 반환 — 설정 책임 캡슐화.
//          _WireUpEvents 에 CatalogMutated 구독 통합 → OnDisable 구독 해제 대상과 짝 추적 용이.
//   결과 / CreateGUI 는 5줄의 조립 흐름만 표현. 항목별 세부 설정은 각 _Create* 로 이동.
//   주의 / _CreateSettingsToggle 람다는 settingsPanel 필드를 늦게 평가.
//          toggle 생성(_BuildToolbar) 전에 settingsPanel(_BuildContentRow) 이 null 이어도
//          클릭 시점에는 유효 — CreateGUI 종료 후에만 사용자 인터랙션 가능하므로 안전.
//
// @Jason - PKH 2026.05.11 Phase 4 — 타이틀 검색 UI (TextField + 카운트 레이블)
//
//   변경 / searchField(TextField 150px) + searchCountLabel(Label) 툴바 추가 (viewportCenterLabel 뒤, settingsToggle 앞).
//          Search 영역 신설: _OnSearchValueChanged / _OnSearchKeyDown / _ClearSearchUI.
//          _BindCatalog 에 _ClearSearchUI() 추가 — 카탈로그 전환 시 검색 UI 초기화.
//   이유 / value changed → canvas.SearchNodes(query) → 첫 결과 이동 + "1/3" 표시.
//          Enter → canvas.AdvanceSearch() → 다음 결과 순환.
//          ESC → SetValueWithoutNotify("") + canvas.ClearSearch() — valueChanged 발화 없이 UI만 초기화.
//          SetValueWithoutNotify 채택 이유: valueChanged 재진입으로 ClearSearch 가 두 번 호출되는 것을 차단.
//   결과 / 카탈로그 전환(_BindCatalog) 시 _ClearSearchUI 호출 → canvas.Bind→_PopulateInternal→ClearSearch 와 쌍으로
//          canvas 상태(count/index) + 툴바 UI(텍스트/카운트) 양쪽 일관 초기화.
//
// @Jason - PKH 2026.05.10 Phase 3 — CatalogNode 드래그드롭 + 카탈로그 스위치 구독
//
//   변경 / _OnDragPerform: catalog 이미 바인드 상태 + 다른 SO 드롭 →
//          NodeCatalogAuthor.CreateCatalogNodeAt(currentCatalog, catalog, dropPos).
//          카탈로그 미바인드 상태 → 기존 _BindCatalog 경로 유지.
//          동일 카탈로그 드롭 → 무반응.
//        canvas.ToGraphPosition(evt.localMousePosition) — pan/zoom 보정된 드롭 위치.
//        canvas.CatalogSwitchRequested 구독 → CatalogNode 더블클릭 시 _BindCatalog 호출.
//        using Object = UnityEngine.Object 추가 — CS0104 (Object 모호성) 해소.
//   이유 / 카탈로그가 이미 바인드된 상태에서 드롭은 "다른 카탈로그로 전환"이 아닌
//          "현재 캔버스에 참조 노드 추가"가 자연스러운 UX.
//          카탈로그 전환은 ObjectField 피커로, 노드 추가는 드래그드롭으로 역할 분리.
//   결과 / canvas.CatalogSwitchRequested += _BindCatalog — 이벤트 구독 단방향.
//          canvas가 HGraphWindow를 모르고, HGraphWindow가 canvas를 구독하는 단방향 의존.
//
// @Jason - PKH 2026.05.10 Phase 2 — Toolbar ObjectField 전환 (Lock 제거)
//
//   변경 / Lock 버튼 + Open Catalog 버튼 + catalogNameLabel 제거. ObjectField(catalogField) 로 대체.
//          OnSelectionChange / OnGUI 제거 (Project 창 클릭 자동 바인드 해제).
//          DragDrop 등록 대상: rootVisualElement → canvas (ObjectField 드래그와 충돌 방지).
//   이유 / 이름만 표시하는 Label 로는 동일명 파일 구분 불가, 클릭 하이라이트 없음.
//          Lock 이 필요했던 이유(자동 바인드 차단)가 ObjectField 전환으로 자연스럽게 해소.
//          ObjectField 의 ping-on-click / 아이콘 표시가 SO 식별 방식을 개선.
//   결과 / canvas 드래그드롭 또는 ObjectField 피커로만 카탈로그 교체 가능 (명시 적용 UX).
//          _BindCatalog 에 catalog == currentCatalog 가드 추가 (이중 바인드 방지).
//   주의 / catalogField.SetValueWithoutNotify — UI↔데이터 동기화. valueChanged 재진입 차단.
//
// @Jason - PKH 2026.05.09 Phase 1-F — Close All 버튼 추가
//
//   변경 / Toolbar 에 [Close All] 버튼 추가. 순서: Lock / Open Catalog / Go To Root / Close All / ...
//   이유 / 다수 노드 펼친 상태에서 한 번에 닫기 위한 직접 진입점.
//   결과 / _CloseAllFoldouts() → canvas.CloseAllFoldouts() → 각 HGraphNode.CloseIfExpanded().
//   주의 / catalog 없을 때 canvas.CloseAllFoldouts 호출해도 무반응 (graphElements 순회 결과 0).
//
// @Jason - PKH 2026-04-24 HGraphWindow 의 역할 - EditorWindow 진입점 + Toolbar + Bind 입구
//
//   [Phase 1-A 확장]
//   - Toolbar: [Lock 아이콘] + [Open Catalog...] 두 버튼.
//     - Lock 아이콘 토글로 selectionLocked 제어. Unity Inspector padlock 관용.
//     - Open Catalog 는 EditorGUIUtility.ShowObjectPicker 경유.
//   - OnSelectionChange: Lock OFF 시 Selection 자동 Bind. 잘못된 타입 early return.
//   - DragDrop: rootVisualElement 에 DragUpdated/DragPerform 콜백 등록.
//     - NodeCatalogSO 만 AcceptDrag, 나머지는 Warning + 거부.
//     - Lock 상태 무관 - 명시 사용자 의도로 간주.
//   - Object picker 수신: OnGUI (IMGUI) 의 ObjectSelectorUpdated 이벤트 경로 경유.
//     + UIElements Toolbar 와 IMGUI ObjectPicker 의 공존은 Unity 관용 (Unity 자체도 동일 패턴).
//
//   [Lock 버튼 텍스트 - ASCII "Lock"/"Locked" 채택]
//   - 초안에서는 유니코드 자물쇠 이모지 (\U0001F513 / \U0001F512) 사용.
//   + 그러나 Unity Editor 기본 폰트가 이모지 글리프 미지원 - 시각적으로 빈 사각형 표시됨.
//   + 최종: "Lock" (OFF, 중립색) / "Locked" (ON, 붉은 배경) 로 상태 2가지를 명확히 구분.
//
//   [Catalog Name Label 추가 - 스모크 피드백 반영]
//   - Toolbar 에 catalogNameLabel 추가: "Catalog: {asset name}" 또는 "(no catalog bound)" 표시.
//   - _BindCatalog(catalog) 헬퍼로 3곳 중복 (OnSelectionChange/OnGUI/DragPerform) 의 bind 로직 단일화.
//
//   [Lock 계약 변경 - Stage 3 피드백 반영 (2026-04-24)]
//   - 초안: "Lock ON 은 Selection 자동 동기화만 차단. 드래그드롭·Open 버튼은 명시 의도로 간주해 Lock 무관"
//   - 변경: "Lock ON 은 모든 Bind 경로 차단. Selection/Drag/Open 전부 동일하게 막힘"
//   + 사용자 의도: "데이터 오버라이드 방지" - 실수로 드래그드롭 해도 기존 catalog 안전 보장.
//
//   [상태 영속화 - 부분 적용]
//   - selectionLocked → [SerializeField] 적용. 세션 내 Lock 상태 안정성 확보.
//   - currentCatalog → [SerializeField] 미적용. Close/Reopen 시 참조 유실은 사용자 의도.
//
//   [Phase 1-C 확장 - 2026-05-07]
//   - Toolbar 에 [Go To Root] [Set as Root] 두 버튼 추가.
//   - "Set as Root" 는 임시 진입점. Phase 1-D 우클릭 메뉴 도입 시 제거 예정.
//
//   [Phase 1-E 버그픽스 - canvas mutation 후 실시간 갱신 - 2026-05-09]
//   - 증상: Cut/Paste/Duplicate/Delete/Create 후 화면 미갱신.
//   - 원인: MarkDirtyRepaint() 는 pass 예약이지 pass 유발이 아님.
//   - 픽스: HGraphWindow 에서 CatalogMutated 구독 + Repaint() 호출.
//
//   [Phase 1-E Toolbar [Settings] 사이드패널 - 2026-05-08]
//   - Toolbar 우측 끝 ToolbarToggle [Settings]. IMGUIContainer settingsPanel 280px 고정 너비.
//   - _OnSettingsPanelGUI → NodeWindowSettingsProvider.DrawSettingsGUI 위임 — DRY.
//
//   [Phase 1-D 정식 이양 후 제거 - 2026-05-08]
//   - Toolbar [Set as Root] 버튼 + _SetSelectedAsRoot 메서드 제거.
//   + 정식 위치 = HGraphNode 우클릭 메뉴 "루트 노드 재설정 (Set as Root)".
// =============================================================================
#endif

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
using System;
using HDiagnosis.Logger;
using HWindows.Editor.NodeWindow.Authoring;
using HWindows.Editor.NodeWindow.Settings;
using HWindows.NodeWindow;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace HWindows.Editor.NodeWindow {
    public class HGraphWindow<TCatalog> : EditorWindow where TCatalog : NodeCatalogSO {
        #region Fields
        protected TCatalog currentCatalog;
        protected HGraphCanvas canvas;
        IMGUIContainer settingsPanel;
        ObjectField catalogField;
        Label viewportCenterLabel;
        TextField searchField;
        Label searchCountLabel;
        #endregion

        #region Unity Lifecycle
        protected virtual void CreateGUI() {
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
            _AppendExtraMenuBarItems(menuBar);
            return menuBar;
        }

        // 서브클래스가 메뉴바 오른쪽에 항목을 추가하는 훅. 기본 구현은 no-op.
        // _BuildMenuBar 내부에서 View/Edit 메뉴 추가 후 호출됨 — 가상 디스패치 보장.
        protected virtual void _AppendExtraMenuBarItems(VisualElement menuBar) { }

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
                objectType = typeof(TCatalog),
                allowSceneObjects = false,
                label = ""
            };
            catalogField.style.flexGrow = 1;
            catalogField.style.marginLeft = 8;
            catalogField.style.marginRight = 8;
            catalogField.RegisterValueChangedCallback(evt => _BindCatalog(evt.newValue as TCatalog));
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
            canvas.CatalogSwitchRequested += catalog => _BindCatalog(catalog as TCatalog);
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
            if (obj is TCatalog) {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.StopPropagation();
            }
        }

        private void _OnDragPerform(DragPerformEvent evt) {
            if (DragAndDrop.objectReferences.Length == 0) return;
            Object obj = DragAndDrop.objectReferences[0];
            if (obj is TCatalog catalog) {
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
                    $"[HGraphWindow] Drop rejected: not a {typeof(TCatalog).Name} (got {obj?.GetType().Name ?? "null"})");
            }
        }
        #endregion

        #region Bind
        protected virtual void _BindCatalog(TCatalog catalog) {
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
// @Jason - PKH 2026.05.15 Phase 7 — HGraphWindow 제너릭 베이스 전환
//
// # 변경
// - 클래스 선언: HGraphWindow → HGraphWindow<TCatalog> where TCatalog : NodeCatalogSO
// - currentCatalog / canvas private → protected (서브클래스 접근 허용)
// - CreateGUI / _BindCatalog private → protected virtual
// - _AppendExtraMenuBarItems(VisualElement) protected virtual 훅 신설 (_BuildMenuBar 내부 호출)
// - objectType = typeof(TCatalog) 으로 ObjectField 타입 제한 자동화 (캐스팅 제거)
// - Window/HWindows/Node Window/Graph Editor [MenuItem] 제거 — 베이스 창 직접 오픈 지원 중단
//
// =============================================================================
// [LOG-20260511-3] Phase 5 — 메뉴바 신설 + Go To Root / Close All Toolbar 이관
// [LOG-20260511-2] 툴바 순서 조정 — 검색 앞, 좌표값 뒤 + viewportCenterLabel 고정 너비
// [LOG-20260511-1] CreateGUI 분해 + CS1061 SetPlaceholderText 제거
// → 전체 이력: docs/history/HWindows/Editor/NodeWindow/Core/HGraphWindow.md
// =============================================================================
#endif

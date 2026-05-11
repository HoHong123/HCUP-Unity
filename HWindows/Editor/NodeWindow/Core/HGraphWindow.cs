#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- HWindows NodeWindow 진입점 EditorWindow. 툴바 + 카탈로그 바인드.
 *
 * 특징 ::
 * ObjectField(catalogField)로 카탈로그 선택 · 파일 확인 · 피커 통합.
 * + canvas 드래그드롭: 카탈로그 미바인드 → 바인드, 이미 바인드 → CatalogNode 자동 생성.
 * + CatalogNode 더블클릭 → canvas.CatalogSwitchRequested → _BindCatalog 카탈로그 전환.
 *
 * 주의사항 ::
 * currentCatalog 미직렬화 — 창 재오픈 시 참조 유실은 의도된 동작.
 * catalogField.SetValueWithoutNotify — UI↔데이터 동기화 전용. valueChanged 재진입 차단.
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
        #endregion

        #region Unity Lifecycle
        private void CreateGUI() {
            VisualElement root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Column;

            VisualElement toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.height = 24;
            toolbar.style.backgroundColor = new StyleColor(new Color(0.22f, 0.22f, 0.22f));
            toolbar.style.paddingLeft = 4;
            toolbar.style.paddingRight = 4;
            toolbar.style.alignItems = Align.Center;

            Button goToRootButton = new Button(_GoToRoot) { text = "Go To Root" };
            goToRootButton.tooltip = "Center viewport on the catalog's root node";
            toolbar.Add(goToRootButton);

            Button closeAllButton = new Button(_CloseAllFoldouts) { text = "Close All" };
            closeAllButton.style.marginLeft = 4;
            closeAllButton.tooltip = "Close all expanded node foldouts";
            toolbar.Add(closeAllButton);

            catalogField = new ObjectField {
                objectType = typeof(NodeCatalogSO),
                allowSceneObjects = false,
                label = ""
            };
            catalogField.style.flexGrow = 1;
            catalogField.style.marginLeft = 8;
            catalogField.style.marginRight = 8;
            catalogField.RegisterValueChangedCallback(evt => _BindCatalog(evt.newValue as NodeCatalogSO));
            toolbar.Add(catalogField);

            viewportCenterLabel = new Label("View: (0, 0)");
            viewportCenterLabel.style.marginRight = 8;
            viewportCenterLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            viewportCenterLabel.style.color = new StyleColor(new Color(0.55f, 0.75f, 0.55f));
            toolbar.Add(viewportCenterLabel);

            ToolbarToggle settingsToggle = new ToolbarToggle { text = "Settings" };
            settingsToggle.RegisterValueChangedCallback(evt => {
                settingsPanel.style.display = evt.newValue
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            });
            toolbar.Add(settingsToggle);

            root.Add(toolbar);

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
            root.Add(contentRow);

            canvas.viewTransformChanged = _ => _UpdateViewportCenterLabel();

            _RegisterDragDropCallbacks();

            canvas.CatalogSwitchRequested += _BindCatalog;

            canvas.Bind(currentCatalog);
            catalogField.SetValueWithoutNotify(currentCatalog);
            _UpdateViewportCenterLabel();

            NodeCatalogAuthor.CatalogMutated += _OnCatalogMutated;
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
        }
        #endregion
    }
}

#if UNITY_EDITOR
// =============================================================================
// Dev Log
// =============================================================================
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

using HDiagnosis.Logger;
using HWindows.NodeWindow;
using UnityEditor;
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
        [SerializeField]
        bool selectionLocked;

        NodeCatalogSO currentCatalog;
        HGraphCanvas canvas;
        Button lockButton;
        Label catalogNameLabel;
        Label viewportCenterLabel;
        int activePickerControlId = -1;
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

            lockButton = new Button(_ToggleLock) { text = "Lock" };
            lockButton.style.width = 56;
            lockButton.tooltip = "Lock selection-based Bind (click to toggle)";
            toolbar.Add(lockButton);

            Button openButton = new Button(_OpenCatalogPicker) { text = "Open Catalog..." };
            openButton.style.marginLeft = 4;
            toolbar.Add(openButton);

            catalogNameLabel = new Label();
            catalogNameLabel.style.marginLeft = 8;
            catalogNameLabel.style.flexGrow = 1;
            catalogNameLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            catalogNameLabel.style.color = new StyleColor(new Color(0.75f, 0.75f, 0.75f));
            toolbar.Add(catalogNameLabel);

            viewportCenterLabel = new Label("View: (0, 0)");
            viewportCenterLabel.style.marginRight = 8;
            viewportCenterLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            viewportCenterLabel.style.color = new StyleColor(new Color(0.55f, 0.75f, 0.55f));
            toolbar.Add(viewportCenterLabel);

            root.Add(toolbar);

            canvas = new HGraphCanvas();
            canvas.style.flexGrow = 1;
            root.Add(canvas);

            // viewport 변경 시점에만 좌표 라벨 갱신 (매 프레임 폴링 회피).
            canvas.viewTransformChanged = _ => _UpdateViewportCenterLabel();

            _RegisterDragDropCallbacks();

            // 직렬화된 selectionLocked / currentCatalog 복원 반영.
            _ApplyLockVisualState();
            canvas.Bind(currentCatalog);
            _UpdateCatalogNameLabel();
            _UpdateViewportCenterLabel();
        }

        private void OnSelectionChange() {
            if (selectionLocked) return;

            NodeCatalogSO catalog = Selection.activeObject as NodeCatalogSO;
            if (catalog == null) return;

            _BindCatalog(catalog);
        }

        private void OnGUI() {
            if (Event.current.commandName == "ObjectSelectorUpdated"
                && EditorGUIUtility.GetObjectPickerControlID() == activePickerControlId) {
                NodeCatalogSO picked = EditorGUIUtility.GetObjectPickerObject() as NodeCatalogSO;
                if (picked != null) {
                    _BindCatalog(picked);
                }
            }
        }

        private void OnDisable() {
            // L1: no cleanup needed for canvas-only window.
            // Phase 1-A: DragDrop 콜백은 rootVisualElement 수명과 묶여 자동 해제.
            // 추후 Phase 1+ 에서 이벤트 구독이 추가되면 여기서 unhook.
        }

        private void _UpdateViewportCenterLabel() {
            // viewTransformChanged 콜백 + 초기 1회 호출. 매 프레임 폴링 없이 갱신.
            if (canvas == null || viewportCenterLabel == null) return;
            Vector2 center = canvas.GetViewportCenterWorld();
            viewportCenterLabel.text = $"View: ({center.x:F0}, {center.y:F0})";
        }
        #endregion

        #region Toolbar Actions
        private void _ToggleLock() {
            selectionLocked = !selectionLocked;
            _ApplyLockVisualState();
        }

        private void _ApplyLockVisualState() {
            if (lockButton == null) return;
            lockButton.text = selectionLocked ? "Locked" : "Lock";
            lockButton.style.backgroundColor = selectionLocked
                ? new StyleColor(new Color(0.55f, 0.35f, 0.35f))
                : StyleKeyword.Null;
            lockButton.tooltip = selectionLocked
                ? "All Bind paths are LOCKED. Click to unlock."
                : "All Bind paths are UNLOCKED. Click to lock.";
        }

        private void _OpenCatalogPicker() {
            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            EditorGUIUtility.ShowObjectPicker<NodeCatalogSO>(
                currentCatalog, false, string.Empty, controlId);
            activePickerControlId = controlId;
        }
        #endregion

        #region DragDrop
        private void _RegisterDragDropCallbacks() {
            rootVisualElement.RegisterCallback<DragUpdatedEvent>(_OnDragUpdated);
            rootVisualElement.RegisterCallback<DragPerformEvent>(_OnDragPerform);
        }

        private void _OnDragUpdated(DragUpdatedEvent evt) {
            if (selectionLocked) {
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                evt.StopPropagation();
                return;
            }
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
                _BindCatalog(catalog);
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
            if (selectionLocked && catalog != currentCatalog) {
                HLogger.Warning("[HGraphWindow] Bind rejected - window is Locked. Unlock to change catalog.");
                return;
            }
            currentCatalog = catalog;
            canvas.Bind(catalog);
            _UpdateCatalogNameLabel();
        }

        private void _UpdateCatalogNameLabel() {
            if (currentCatalog == null) {
                catalogNameLabel.text = "(no catalog bound)";
            }
            else {
                catalogNameLabel.text = $"Catalog: {currentCatalog.name}";
            }
        }
        #endregion
    }
}

#if UNITY_EDITOR
// =============================================================================
// Dev Log
// =============================================================================
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
//   + 사용자 피드백 메모리 (ASCII 직접 입력 가능 문자 우선) 와도 정합.
//   + 최종: "Lock" (OFF, 중립색) / "Locked" (ON, 붉은 배경) 로 상태 2가지를 명확히 구분.
//     - 텍스트 변화 + 배경색 변화로 시각 피드백 이중화.
//     - ASCII 만 사용, 어떤 폰트 환경에서도 동작 보장.
//
//   [Catalog Name Label 추가 - 스모크 피드백 반영]
//   - Toolbar 에 catalogNameLabel 추가: "Catalog: {asset name}" 또는 "(no catalog bound)" 표시.
//   + Stage 2 검증 시 Bind 경로별 (Selection/Drag/Picker) 결과 확인 GUI 가 필요했음.
//   + Phase 1-A 스펙 §4.1 minimal Toolbar 구성을 소급 확장. empty state 힌트와 대칭의 bound state 표시.
//   - _BindCatalog(catalog) 헬퍼로 3곳 중복 (OnSelectionChange/OnGUI/DragPerform) 의 bind 로직 단일화.
//     + currentCatalog 업데이트 + canvas.Bind + Label 갱신을 한 메서드로 집약 (DRY).
//
//   [Lock 계약 변경 - Stage 3 피드백 반영 (2026-04-24)]
//   - 초안 (스펙 §2.3 P1-b): "Lock ON 은 Selection 자동 동기화만 차단. 드래그드롭·Open 버튼은 명시 의도로 간주해 Lock 무관"
//   + Unity Inspector padlock 관용을 그대로 채택한 것이었음.
//   - 변경 후: "Lock ON 은 모든 Bind 경로 차단. Selection/Drag/Open 전부 동일하게 막힘"
//   + 사용자 의도: "데이터 오버라이드 방지" - 실수로 드래그드롭 해도 기존 catalog 안전 보장.
//   + Phase 1-F Undo 가 오기 전까지 중요한 데이터 안전망.
//   - 구현: _BindCatalog 헬퍼에 Lock 중앙 체크 추가 (catalog 변경 시도 시 거부 + Warning).
//     _OnDragUpdated 에서 Lock 상태면 visualMode=Rejected 로 시각 피드백 (drop 불가 커서).
//   + _BindCatalog 가 3경로 집약점이므로 체크 1곳에 추가하면 Selection/Drag/Picker 전부 일괄 차단.
//
//   [상태 영속화 - 부분 적용]
//   - 배경: Stage 3 테스트에서 Lock 상태가 유실되는 현상 관찰.
//     의심 원인: Unity EditorWindow 라이프사이클 (domain reload / 재생성) 중 일반 private 필드 초기화.
//   - 선택:
//     + selectionLocked → [SerializeField] 적용. 세션 내 Lock 상태 안정성 확보.
//     + currentCatalog → [SerializeField] 미적용. Close/Reopen 시 참조 유실은 사용자 의도.
//       (브레인스토밍 이후 피드백: "창 재오픈 시 SO 참조 유실은 이슈가 아니다")
//   - CreateGUI 는 currentCatalog (null 또는 재생성 후 기본값) 을 canvas.Bind 로 그대로 전달.
//     + null 이면 empty state 힌트 표시로 자연스럽게 흘러감.
//   - _ApplyLockVisualState: _ToggleLock 과 CreateGUI 가 공유하는 UI 갱신 로직.
//     + lockButton 이 null 일 수 있는 시점 (serialization 복원 직후) 방어.
// =============================================================================
#endif

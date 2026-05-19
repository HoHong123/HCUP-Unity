#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- HDialogue 전용 노드 그래프 에디터 윈도우.
 *
 * 특징 ::
 * HGraphWindow 를 상속, HCUP/Dialogue 메뉴에서 오픈.
 * + 메뉴바 [Create ▾]: 9종 다이얼로그 노드를 뷰포트 중앙에 생성.
 * + 우클릭 컨텍스트 메뉴: 마우스 위치의 그래프 좌표에 동일 9종 생성.
 * + HGraphCanvas.AdditionalContextMenuActions 에 주입 — 베이스 창 미영향.
 * + Play 모드 경로 하이라이트 — OnLineEnter → 실행 중인 LineNode 녹색 강조.
 *
 * 주의사항 ::
 * AdditionalContextMenuActions 는 인스턴스 레벨. 이 창의 캔버스에만 적용됨.
 * CreateGUI 에서 base.CreateGUI() 호출 후 canvas 가 초기화되므로 순서 엄수.
 * Play 모드 진입 전 NodeWindow 가 열려 있어야 하이라이트 활성화됨.
 * =========================================================
 */
#endif

using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using HWindows.Editor.NodeWindow;
using HWindows.Editor.NodeWindow.Authoring;
using HWindows.NodeWindow;

namespace HDialogue.Editor {
    public sealed class DialogueNodeWindow : HGraphWindow<DialogueCatalogSO> {
        #region Fields
        DialogueDirector trackedDirector;
        #endregion

        #region Menu
        [MenuItem("HCUP/Dialogue/Open Node Window", false, 10)]
        public static void OpenDialogueWindow() {
            DialogueNodeWindow window = GetWindow<DialogueNodeWindow>();
            window.titleContent = new GUIContent("Dialogue Graph Editor");
            window.minSize = new Vector2(400, 300);
        }
        #endregion

        #region Lifecycle
        private void OnEnable() {
            EditorApplication.playModeStateChanged += _OnPlayModeChanged;
        }

        private void OnDisable() {
            EditorApplication.playModeStateChanged -= _OnPlayModeChanged;
            _UntrackDirector();
        }

        protected override void CreateGUI() {
            base.CreateGUI();
            _WireDialogueContextMenu();
        }
        #endregion

        #region Menu Bar Extension
        protected override void _AppendExtraMenuBarItems(VisualElement menuBar) {
            menuBar.Add(_BuildCreateMenu());
            menuBar.Add(_BuildTraceToggle());
        }

        private ToolbarToggle _BuildTraceToggle() {
            ToolbarToggle toggle = new ToolbarToggle { text = "Trace" };
            toggle.tooltip = "선택 노드에서 도달 가능한 모든 노드를 청록으로 표시 (선택 없으면 Root 기준)";
            toggle.RegisterValueChangedCallback(evt => {
                if (canvas == null) return;
                canvas.SetTraceMode(evt.newValue);
            });
            return toggle;
        }

        private ToolbarMenu _BuildCreateMenu() {
            ToolbarMenu createMenu = new ToolbarMenu { text = "Create" };
            _AppendDialogueNodeItems(createMenu.menu, _ => canvas.GetViewportCenterWorld());
            return createMenu;
        }
        #endregion

        #region Context Menu Extension
        private void _WireDialogueContextMenu() {
            canvas.AdditionalContextMenuActions = evt => {
                if (currentCatalog == null) return;
                _AppendDialogueNodeItems(evt.menu,
                    action => canvas.ToGraphPosition(action.eventInfo.localMousePosition));
            };
        }
        #endregion

        #region Play Mode Bridge (HCUP-2.7.0 Phase 1)
        private void _OnPlayModeChanged(PlayModeStateChange state) {
            if (state == PlayModeStateChange.EnteredPlayMode) _TrackDirector();
            else if (state == PlayModeStateChange.ExitingPlayMode) _UntrackDirector();
        }

        private void _TrackDirector() {
            DialogueDirector[] found = FindObjectsByType<DialogueDirector>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (found.Length == 0) return;
            trackedDirector = found[0];
            trackedDirector.OnLineEnter += _OnLineEnter;
        }

        private void _UntrackDirector() {
            if (trackedDirector == null) return;
            trackedDirector.OnLineEnter -= _OnLineEnter;
            trackedDirector = null;
            canvas?.ClearActiveHighlight();
        }

        private void _OnLineEnter(DialogueLineNode node) {
            canvas?.HighlightActiveNode(node.UID);
        }
        #endregion

        #region Shared Node Creation Items
        // 우클릭 컨텍스트 메뉴와 메뉴바 Create 서브메뉴가 공유하는 항목 빌더.
        // getPosition: DropdownMenuAction → 그래프 좌표. 메뉴바는 뷰포트 중앙, 우클릭은 마우스 위치.
        private void _AppendDialogueNodeItems(DropdownMenu menu, Func<DropdownMenuAction, Vector2> getPosition) {
            DropdownMenuAction.Status Status(DropdownMenuAction _) =>
                currentCatalog != null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;

            menu.AppendAction("Entry Node",
                a => _CreateNodeAt<DialogueEntryNode>(getPosition(a)), Status);
            menu.AppendAction("Line Node",
                a => _CreateNodeAt<DialogueLineNode>(getPosition(a)), Status);
            menu.AppendAction("Choice Node (Hub)",
                a => _CreateNodeAt<DialogueChoiceNode>(getPosition(a)), Status);
            menu.AppendAction("Branch Node (Hub)",
                a => _CreateNodeAt<DialogueBranchNode>(getPosition(a)), Status);
            menu.AppendAction("Exit Node",
                a => _CreateNodeAt<DialogueExitNode>(getPosition(a)), Status);
            menu.AppendAction("Event Node",
                a => _CreateNodeAt<DialogueEventNode>(getPosition(a)), Status);
            menu.AppendAction("Variable Node",
                a => _CreateNodeAt<DialogueVariableNode>(getPosition(a)), Status);
            menu.AppendAction("Wait Node",
                a => _CreateNodeAt<DialogueWaitNode>(getPosition(a)), Status);
            menu.AppendAction("Cinematic Node",
                a => _CreateNodeAt<DialogueCinematicNode>(getPosition(a)), Status);
        }

        private void _CreateNodeAt<T>(Vector2 position) where T : BaseNode {
            if (currentCatalog == null) return;
            NodeCatalogAuthor.CreateNode<T>(currentCatalog, position);
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.20 HCUP-2.7.0 Phase 1 — Play 모드 경로 하이라이트
 *
 * # 변경
 * - `DialogueDirector trackedDirector` 필드 추가.
 * - `OnEnable`: `EditorApplication.playModeStateChanged += _OnPlayModeChanged` 구독.
 * - `OnDisable`: 구독 해제 + `_UntrackDirector()`.
 * - `_OnPlayModeChanged`: EnteredPlayMode → `_TrackDirector`, ExitingPlayMode → `_UntrackDirector`.
 * - `_TrackDirector`: `FindObjectsByType<DialogueDirector>` → `OnLineEnter` 구독.
 * - `_UntrackDirector`: `OnLineEnter` 해제 + `canvas.ClearActiveHighlight()`.
 * - `_OnLineEnter`: `canvas.HighlightActiveNode(node.UID)` 위임.
 *
 * # 이유
 * - Play 모드에서 실행 중인 LineNode를 NodeWindow에 녹색 외곽선으로 실시간 표시.
 * - `ExitingPlayMode` 시 해제 — 이 시점에 Director는 아직 살아 있어 안전한 -= 가능.
 *   `ExitedPlayMode` 이후는 오브젝트 파괴 완료 시점 → null 역참조 위험.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.16 DialogueCinematicNode 메뉴 항목 추가
 *
 * # 변경
 * - _AppendDialogueNodeItems: "Cinematic Node" AppendAction 추가 (8종 → 9종)
 * - 헤더 주석 노드 수 갱신
 *
 * =============================================================================
 * @Jason - PKH 2026.05.15 HCUP-2.3.0 Phase 7 — DialogueNodeWindow 베이스 코드 생성
 *
 * # 변경
 * - HGraphWindow 비sealed 화 + canvas/currentCatalog protected 이관 후 서브클래스로 구현
 * - HGraphCanvas.AdditionalContextMenuActions 인스턴스 델리게이트로 우클릭 메뉴 주입
 * - 메뉴바 Create 서브메뉴: 뷰포트 중앙 기준 노드 생성
 * - 우클릭 컨텍스트 메뉴: 마우스 위치 기준 노드 생성
 *
 * # 이유
 * - 다이얼로그 전용 창만 8종 노드 생성 메뉴 보유 — 베이스 HGraphWindow 미변경
 * - 공통 _AppendDialogueNodeItems 헬퍼로 두 진입점(메뉴바·우클릭) 중복 제거
 *
 * # 주의
 * - CreateGUI 에서 base.CreateGUI() 가 canvas 를 초기화하므로 _WireDialogueContextMenu 는 반드시 그 후 호출
 * - _AppendExtraMenuBarItems 는 _BuildMenuBar 내부에서 가상 디스패치 → base.CreateGUI() 실행 중 이미 호출됨
 * =============================================================================
 */
#endif

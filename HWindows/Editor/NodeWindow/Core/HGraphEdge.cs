#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- BaseNodeEdge 1개에 대응하는 GraphView 시각 엣지.
 *
 * 특징 ::
 * BranchUID + LeafUID 로 catalog 엣지와 1:1 매핑.
 * + OnSelected / OnUnselected → 연결 노드 CSS 하이라이트.
 * + 우클릭 메뉴: 연결 해제 / 브랜치·리프 시점 중앙.
 *
 * 주의사항 ::
 * Capabilities.Deletable 비활성 — Delete 키 처리는 HGraphCanvas._OnKeyDown 집중.
 * + 연결 해제: 우클릭 메뉴 / Delete 키(HGraphCanvas._DeleteSelectedEdges) 두 경로만.
 * =========================================================
 */
#endif
using HWindows.Editor.NodeWindow.Authoring;
using HWindows.NodeWindow;
using HWindows.NodeWindow.Identity;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace HWindows.Editor.NodeWindow {
    public sealed class HGraphEdge : Edge {
        #region Properties
        public NodeUID BranchUID { get; }
        public NodeUID LeafUID { get; }

        // HGraphCanvas Populate 시점에 주입. 우클릭 메뉴 핸들러가 catalog 조회용.
        internal NodeCatalogSO Catalog { get; set; }
        #endregion

        #region Constructor
        public HGraphEdge(NodeUID branchUID, NodeUID leafUID) {
            BranchUID = branchUID;
            LeafUID = leafUID;

            // HGraphCanvas._OnKeyDown 이 Delete 일괄 처리 — GraphView 자동 삭제 경로 차단.
            capabilities &= ~Capabilities.Deletable;

            // Edge 베이스 체인이 BuildContextualMenu 를 virtual 로 노출하지 않아
            // override 불가 (CS0115). ContextualMenuPopulateEvent 콜백 등록으로 대체.
            RegisterCallback<ContextualMenuPopulateEvent>(_OnContextualMenuPopulate);
        }
        #endregion

        #region Public - Selection Highlight (Phase 2)
        public override void OnSelected() {
            base.OnSelected();
            _SetConnectedNodeHighlight(true);
        }

        public override void OnUnselected() {
            base.OnUnselected();
            _SetConnectedNodeHighlight(false);
        }
        #endregion

        #region Private - Context Menu (Phase 2)
        private void _OnContextualMenuPopulate(ContextualMenuPopulateEvent evt) {
            if (Catalog == null) return;

            // Canvas.BuildContextualMenu 의 early-return 이 버블 전파 차단 역할을 하지만,
            // 콜백 방식에서는 evt.StopPropagation() 으로 이중 방어.
            evt.StopPropagation();

            evt.menu.AppendAction("연결 해제 (Disconnect)",
                _ => _OnContextDisconnect(),
                DropdownMenuAction.Status.Normal);

            evt.menu.AppendSeparator();

            evt.menu.AppendAction("브랜치 노드를 시점 중앙으로",
                _ => GetFirstAncestorOfType<HGraphCanvas>()?.CenterOnNode(BranchUID),
                DropdownMenuAction.Status.Normal);

            evt.menu.AppendAction("리프 노드를 시점 중앙으로",
                _ => GetFirstAncestorOfType<HGraphCanvas>()?.CenterOnNode(LeafUID),
                DropdownMenuAction.Status.Normal);
        }
        #endregion

        #region Private - Handlers
        private void _SetConnectedNodeHighlight(bool on) {
            GetFirstAncestorOfType<HGraphCanvas>()?.HighlightNodesByEdge(BranchUID, LeafUID, on);
        }

        private void _OnContextDisconnect() {
            if (Catalog == null) return;
            NodeCatalogAuthor.DisconnectEdge(Catalog, BranchUID, LeafUID);
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.10 Phase 2 — HGraphEdge 베이스 코드 생성
 *
 * # 목적
 * - GraphView.Edge 상속으로 BaseNodeEdge 1개에 1:1 대응하는 시각 엣지 구현.
 * - BranchUID / LeafUID 보유로 catalog 엣지와 매핑 유지.
 *
 * # 사용 흐름
 * - HGraphCanvas._PopulateInternal → new HGraphEdge(branch, leaf) → edgeView.output/input 포트 연결.
 * - OnSelected → HighlightNodesByEdge → "hgraph-node--edge-highlight" CSS 토글.
 * - 우클릭 연결 해제 → DisconnectEdge → CatalogMutated → repopulate.
 * - Delete 키 → HGraphCanvas._DeleteSelectedEdges → DisconnectEdge (Deletable 비활성이므로 직접 처리).
 *
 * =============================================================================
 */
#endif

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
        #endregion

        #region Fields
        readonly BaseNode dataNode;
        readonly bool isRoot;
        VisualElement headerBar;
        Label titleLabel;
        #endregion

        #region Properties
        public BaseNode DataNode => dataNode;
        public NodeUID UID => dataNode.UID;
        public bool IsRoot => isRoot;
        #endregion

        #region Constructor
        public HGraphNode(BaseNode dataNode, bool isRoot = false) {
            this.dataNode = dataNode;
            this.isRoot = isRoot;

            _LoadStyleSheet();
            AddToClassList("hgraph-node");

            _BuildHeader();
            _BuildTitle();

            RefreshExpandedState();
            RefreshPorts();
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

            string headerText = isRoot
                ? $"{dataNode.GetType().Name}  [ROOT]"
                : dataNode.GetType().Name;
            Label headerLabel = new Label(headerText);
            headerBar.Add(headerLabel);

            mainContainer.Insert(0, headerBar);
        }

        private void _BuildTitle() {
            titleLabel = new Label(dataNode.Title);
            titleLabel.AddToClassList("hgraph-node-title");
            mainContainer.Add(titleLabel);
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
// =============================================================================
#endif

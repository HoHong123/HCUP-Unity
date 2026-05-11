#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- CatalogNode 전용 시각 노드 (GraphView 확장).
 *
 * 특징 ::
 * HGraphNode 상속. body에 참조 카탈로그 ObjectField + 더블클릭 카탈로그 전환.
 * + 복사/복제/잘라내기/루트재설정 컨텍스트 메뉴 차단 (이동/삭제/붙여넣기만 허용).
 * + 포트: 입구 1개 고정, 출구 1개 고정 (다중 포트는 HubNode 가 전담).
 *
 * 주의사항 ::
 * OnHeaderDoubleClick override — foldout 토글 없이 카탈로그 전환만 수행.
 * + 다중 출구 Port 기능 제거됨 (Phase 3+ HubNode 분리).
 * =========================================================
 */
#endif
using System.Collections.Generic;
using HDiagnosis.Logger;
using HWindows.Editor.NodeWindow.Authoring;
using HWindows.NodeWindow;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace HWindows.Editor.NodeWindow {
    public sealed class HGraphCatalogNode : HGraphNode {
        #region Constructor
        public HGraphCatalogNode(CatalogNode dataNode, bool isRoot = false)
            : base(dataNode, isRoot) {
            AddToClassList("hgraph-catalog-node");
            _InitCatalogBody();
        }
        #endregion

        #region Protected Override — 더블클릭 → 카탈로그 전환
        // 헤더 더블클릭 시 foldout 토글 대신 참조 카탈로그로 뷰 전환.
        // referencedCatalog 미설정 시 무반응.
        protected override void OnHeaderDoubleClick(UnityEngine.UIElements.MouseDownEvent evt) {
            if (DataNode is not CatalogNode cn || cn.ReferencedCatalog == null) return;
            HGraphCanvas canvas = GetFirstAncestorOfType<HGraphCanvas>();
            canvas?.RequestCatalogSwitch(cn.ReferencedCatalog);
            evt.StopPropagation();
        }
        #endregion

        #region Public Override — 컨텍스트 메뉴 (복사/복제/잘라내기 제거)
        // base.BuildContextualMenu 미호출 — 허용 항목만 직접 구성.
        // CatalogNode 허용: 붙여넣기 / 삭제. 이동/연결은 GraphView 기본 처리.
        // 루트 노드 재설정 제외 — CatalogNode는 루트가 될 수 없음.
        public override void BuildContextualMenu(UnityEngine.UIElements.ContextualMenuPopulateEvent evt) {
            NodeCatalogSO catalog = Catalog;
            if (catalog == null) return;

            DropdownMenuAction.Status pasteStatus = HGraphClipboard.IsValid(GUIUtility.systemCopyBuffer)
                ? DropdownMenuAction.Status.Normal
                : DropdownMenuAction.Status.Disabled;
            evt.menu.AppendAction("붙여넣기 (Paste)",
                _ => { GetFirstAncestorOfType<HGraphCanvas>()?.PasteFromClipboard(); },
                pasteStatus);

            evt.menu.AppendSeparator();

            evt.menu.AppendAction("삭제 (Delete)",
                _ => { GetFirstAncestorOfType<HGraphCanvas>()?.DeleteNodes(new List<HGraphNode> { this }); },
                DropdownMenuAction.Status.Normal);
        }
        #endregion

        #region Private — Body 초기화
        // base._BuildBody가 추가한 UID placeholder를 제거하고 ObjectField 로 재구성.
        private void _InitCatalogBody() {
            bodyArea.Clear();

            ObjectField field = new ObjectField("참조 카탈로그") {
                objectType = typeof(NodeCatalogSO),
                allowSceneObjects = false
            };
            field.style.marginBottom = 4;
            if (DataNode is CatalogNode cn)
                field.SetValueWithoutNotify(cn.ReferencedCatalog);
            field.RegisterValueChangedCallback(evt => _OnCatalogFieldChanged(evt.newValue as NodeCatalogSO));
            bodyArea.Add(field);

            Label hint = new Label("더블클릭 → 카탈로그 전환");
            hint.style.color = new StyleColor(new Color(0.55f, 0.85f, 0.8f, 0.6f));
            hint.style.fontSize = 10;
            hint.style.unityTextAlign = TextAnchor.MiddleCenter;
            hint.style.marginTop = 4;
            bodyArea.Add(hint);
        }

        private void _OnCatalogFieldChanged(NodeCatalogSO newCatalog) {
            if (DataNode is not CatalogNode cn) return;
            Undo.RecordObject(cn, "Change Referenced Catalog");
            cn.SetReferencedCatalog(newCatalog);
            EditorUtility.SetDirty(cn);
            AssetDatabase.SaveAssets();
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.11 CatalogNode 루트 설정 제약
 *
 * # 변경
 * - BuildContextualMenu 에서 "루트 노드 재설정 (Set as Root)" 항목 제거.
 * - 헤더 주석 허용 항목 갱신: 이동/삭제/붙여넣기만 허용 명시.
 *
 * # 이유
 * - CatalogNode 는 외부 카탈로그 참조 역할만 담당. 루트는 일반 노드가 맡아야 함.
 * - NodeCatalogAuthor.SetRoot 에도 CatalogNode 타입 가드 추가 (backend 이중 방어).
 *
 * =============================================================================
 * @Jason - PKH 2026.05.10 Phase 3+ — 다중 출구 Port 제거 (HubNode 분리)
 *
 * # 변경
 * - 동적 포트 시스템 전면 제거: _outputPorts / _outputPortColumn / EnsureOutputPorts /
 *   AddSpareOutputPort / GetOutputPortIndex / UpdateOutputPortLabel / _BuildPorts override.
 * - GetOutputPort override 제거 → base 단일 outputPort 반환으로 복귀.
 * - 포트 구성: 입구 1개 + 출구 1개 (base _BuildPorts 기본 동작).
 *
 * # 이유
 * - 사양 재정의: CatalogNode 는 "외부 카탈로그 연결 표시" 단순 역할.
 *   다중 라우팅 → HubNode 가 전담. 역할 단일화 + 오류 원인 제거.
 * - 이전 구현 오류: CatalogNode 의 동적 포트 수가 "A에서 C로 들어오는 연결 수"가 아닌
 *   "자신의 outgoing 연결 수"로 계산되어 요구사항과 불일치.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.10 Phase 3+ — 동적 출력 포트 + 양방향 생성 지원
 *
 * # 변경 (이전 — 제거됨)
 * - _BuildPorts override: 입력 1개 + _outputPortColumn(세로 컬럼) 레이아웃.
 * - EnsureOutputPorts(count) / AddSpareOutputPort / GetOutputPortIndex / UpdateOutputPortLabel.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.10 Phase 3 — HGraphCatalogNode.cs 베이스 코드 생성
 *
 * # 목적
 * - CatalogNode 데이터를 시각화하는 Editor 전용 GraphView 노드.
 * - 더블클릭 네비게이션 + 복사/복제 차단 + 참조 카탈로그 ObjectField.
 *
 * # 사용 흐름
 * - HGraphCanvas._PopulateInternal: data is CatalogNode 분기에서 생성.
 * - OnHeaderDoubleClick → canvas.RequestCatalogSwitch → HGraphWindow._BindCatalog.
 * - body ObjectField 변경 → Undo.RecordObject(cn) + SetReferencedCatalog + SetDirty.
 * =============================================================================
 */
#endif

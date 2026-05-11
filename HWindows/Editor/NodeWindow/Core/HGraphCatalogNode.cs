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
 * [LOG-20260511-1] CatalogNode 루트 설정 제약 (Set as Root 항목 제거)
 * [LOG-20260510-2] Phase 3+ — 다중 출구 Port 제거 (HubNode 분리)
 * [LOG-20260510-1b] Phase 3+ — 동적 출력 포트 + 양방향 생성 지원 (이전 제거됨)
 * → 전체 이력: docs/history/HWindows/Editor/NodeWindow/Core/HGraphCatalogNode.md
 * =============================================================================
 */
#endif

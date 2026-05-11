#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 타 NodeCatalogSO 를 참조하는 전용 노드. 카탈로그 간 연결 유도.
 *
 * 특징 ::
 * BaseNode 상속. referencedCatalog (#if UNITY_EDITOR) 필드만 추가.
 * + HGraphCatalogNode(Editor)가 ObjectField + 더블클릭 네비게이션 처리.
 * + [HTitle("참조 카탈로그")] + [HReadOnly] — GraphView ObjectField 가 정식 편집 채널.
 *
 * 주의사항 ::
 * referencedCatalog는 Editor-only — 빌드에 SO 참조 메모리/크기 영향 없음.
 * + 복제/복사 UI 차단은 HGraphCatalogNode.BuildContextualMenu에서 수행.
 * =========================================================
 */
#endif
using HInspector;
using UnityEngine;

namespace HWindows.NodeWindow {
    public sealed class CatalogNode : BaseNode {
#if UNITY_EDITOR
        #region Fields
        [HTitle("참조 카탈로그")]
        [HReadOnly]
        [SerializeField]
        NodeCatalogSO referencedCatalog;
        #endregion

        #region Properties
        public NodeCatalogSO ReferencedCatalog => referencedCatalog;
        #endregion

        #region Internal
        // Author 전용. CreateCatalogNodeAt에서 생성 직후 1회 호출.
        internal void SetReferencedCatalog(NodeCatalogSO catalog) {
            referencedCatalog = catalog;
        }
        #endregion
#endif

        #region Public - Clipboard
        // CatalogNode 고유 magic. SimpleNode 와 혼합 selection 시 Serialize 거부.
        // 단독 selection 도 HGraphCatalogNode 가 복사/복제 메뉴 차단.
        public override string ClipboardMagic => "HGRAPH_CATALOG_NODE_V1";
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.10 — Inspector HTitle + HReadOnly 추가
 *
 * # 변경
 * - using HInspector 추가.
 * - referencedCatalog: [HTitle("참조 카탈로그")] + [HReadOnly].
 *
 * # 이유
 * - referencedCatalog 변경은 HGraphCatalogNode ObjectField → Undo.RecordObject → SetDirty 경로가 정식.
 *   Inspector 직접 수정 시 CatalogMutated 미발화 → canvas 불일치 발생 가능.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.10 Phase 3 — CatalogNode.cs 베이스 코드 생성
 *
 * # 목적
 * - 카탈로그 간 연결을 위한 전용 노드 타입.
 * - referencedCatalog(#if UNITY_EDITOR)로 대상 SO 보관. 빌드에서 필드 자체가 제거됨.
 *
 * # 사용 흐름
 * - NodeCatalogAuthor.CreateCatalogNodeAt 으로 생성 + 참조 설정.
 * - HGraphCatalogNode 가 ObjectField 표시 + 더블클릭 → HGraphWindow.BindCatalog 경유 전환.
 * =============================================================================
 */
#endif

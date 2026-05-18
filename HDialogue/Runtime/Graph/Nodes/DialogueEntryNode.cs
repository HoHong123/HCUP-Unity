#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- 대화 그래프 진입점 노드.
 *
 * 특징 / 지원기능 ::
 * + 추가 도메인 필드 없음 — 카탈로그당 정확히 1개 존재
 * + DialogueCatalogSO.RootNode로 지정됨
 * + DialogueDirector.PlayCatalog 진입 시 즉시 통과 (1프레임 yield)
 *
 * 주의사항 ::
 * DialogueCatalogValidator(Phase 3)가 EntryNode=1개, RootNode 일치를 강제 검증.
 * =========================================================
 */
#endif

using HWindows.NodeWindow;

namespace HDialogue {
    public sealed class DialogueEntryNode : BaseNode {
        public override string ClipboardMagic => "HGRAPH_DIALOGUE_ENTRY_NODE_V1";
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.15 DialogueEntryNode 베이스 코드 생성
 *
 * # 목적
 * - 대화 그래프 시작점 — DialogueDirector가 RootNode로부터 순회 시작
 * - HCUP-2.3.0 Phase 1
 *
 * # 사용 흐름
 * - DialogueCatalogAuthor.CreateEntryNode → catalog.InternalSetRoot(uid)
 * - DialogueDirector._ProcessEntryNode → 1프레임 yield 후 ResolveNextNode
 *
 * =============================================================================
 */
#endif

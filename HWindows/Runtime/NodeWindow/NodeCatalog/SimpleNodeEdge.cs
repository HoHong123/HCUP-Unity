#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * -- BaseNodeEdge 최소 concrete 구현.
 *
 * 특징 ::
 * 도메인 필드 없음 — 그래프 구조 정보(branchUID + leafUID)만 보유.
 * + ConnectEdge<TEdge> 의 `where TEdge : BaseNodeEdge, new()` 제약 충족용 wrapper.
 * =========================================================
 */
#endif
using System;

namespace HWindows.NodeWindow {
    // 검증용 최소 concrete edge. BaseNodeEdge 는 abstract — new() 제약 충족을 위한 concrete wrapper.
    [Serializable]
    public sealed class SimpleNodeEdge : BaseNodeEdge { }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.10 Phase 2 — SimpleNodeEdge 베이스 코드 생성
 *
 * # 목적
 * - BaseNodeEdge abstract 의 최소 concrete 구현.
 * - NodeCatalogAuthor.ConnectEdge<TEdge> where TEdge : BaseNodeEdge, new() 제약 충족.
 *
 * # 사용 흐름
 * - HGraphCanvas._OnGraphViewChanged → ConnectEdge<SimpleNodeEdge>(catalog, branchUID, leafUID).
 * - SerializeReference 직렬화 — catalog.edges YAML 에 FQN "HWindows.NodeWindow.SimpleNodeEdge" 저장.
 *
 * =============================================================================
 */
#endif

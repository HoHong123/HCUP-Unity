---
script_path: Assets/01_Scripts/HCUP-Unity/HWindows/Editor/NodeWindow/Core/HGraphEdge.cs
script_name: HGraphEdge
latest_log_id: LOG-20260510-1
total_entries: 1
created: 2026-05-12
updated: 2026-05-12
---

# HGraphEdge Dev Log History

`Assets/01_Scripts/HCUP-Unity/HWindows/Editor/NodeWindow/Core/HGraphEdge.cs` 의 Dev Log 엔트리 풀 본문이 본 파일에 시간 역순으로 보관됩니다 (최신이 위). .cs 파일 안에는 최신 3 엔트리의 1-줄 요약 view 가 유지되며, **본 history MD 가 ground truth** — 모든 엔트리의 풀 본문은 손실 없이 본 파일에 보관됩니다.

본 파일은 unity-devlog-history-mirror 스킬에 의해 사용자 명시 요청으로 생성되었습니다 (2026-05-12). 엔트리 1개로 최초 생성 — .cs 파일 Dev Log 내용과 동일.

=============================================================================
@Jason - PKH 2026.05.10 Phase 2 — HGraphEdge 베이스 코드 생성 [LOG-20260510-1]

# 목적
- GraphView.Edge 상속으로 BaseNodeEdge 1개에 1:1 대응하는 시각 엣지 구현.
- BranchUID / LeafUID 보유로 catalog 엣지와 매핑 유지.

# 사용 흐름
- HGraphCanvas._PopulateInternal → new HGraphEdge(branch, leaf) → edgeView.output/input 포트 연결.
- OnSelected → HighlightNodesByEdge → "hgraph-node--edge-highlight" CSS 토글.
- 우클릭 연결 해제 → DisconnectEdge → CatalogMutated → repopulate.
- Delete 키 → HGraphCanvas._DeleteSelectedEdges → DisconnectEdge (Deletable 비활성이므로 직접 처리).

=============================================================================

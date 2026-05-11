---
script_path: Assets/01_Scripts/HCUP-Unity/HWindows/Editor/NodeWindow/Core/HGraphClipboard.cs
script_name: HGraphClipboard
latest_log_id: LOG-20260509-1
total_entries: 2
created: 2026-05-12
updated: 2026-05-12
---

# HGraphClipboard Dev Log History

`Assets/01_Scripts/HCUP-Unity/HWindows/Editor/NodeWindow/Core/HGraphClipboard.cs` 의 Dev Log 엔트리 풀 본문이 본 파일에 시간 역순으로 보관됩니다 (최신이 위). .cs 파일 안에는 최신 3 엔트리의 1-줄 요약 view 가 유지되며, **본 history MD 가 ground truth** — 모든 엔트리의 풀 본문은 손실 없이 본 파일에 보관됩니다.

본 파일은 unity-devlog-history-mirror 스킬에 의해 사용자 명시 요청으로 생성되었습니다 (2026-05-12). legacy 형식 엔트리 포함.

=============================================================================
@Jason - PKH 2026.05.09 Phase 1-F — Serialize 에디터 상태 읽기 이관 (catalog → BaseNode) [LOG-20260509-1]

# 변경
- Serialize: catalog.EditorNodeLayouts / FoldoutOpen / OpenSizes dict 읽기
  → n.EditorPosition / n.EditorFoldoutOpen / n.EditorOpenSize 직접 읽기
- catalog 파라미터: 보조 맵 읽기 제거 후 미사용 상태이나 미래 확장용으로 시그니처 유지

# 이유
- NodeCatalogSO Phase 1-F 에서 editor HDictionary 3개 제거에 따른 소비처 갱신.

=============================================================================
@Jason - PKH 2026-05-07 HGraphClipboard 의 역할 - Cut/Paste 직렬화 + 검증 단일 게이트 [LOG-20260507-1]
=============================================================================

[역할]
- 노드(들) ↔ 클립보드 JSON 변환 + magic/version 검증 단일 진입점.
- GUIUtility.systemCopyBuffer 입출력은 호출자(NodeCatalogAuthor 또는 HGraphNode/Canvas) 책임.

[JSON 형식 - 도메인별 magic (HGRAPH_<DOMAIN>_NODE_V1)]
{
  "magic": "HGRAPH_<DOMAIN>_NODE_V1",
  "version": 1,
  "entries": [
    { "typeName": "...AssemblyQualifiedName...",
      "nodeJson": "{...JsonUtility.ToJson result...}",
      "layout": {"x":..,"y":..},
      "foldoutOpen": bool,
      "openSize": {"x":..,"y":..} }
  ]
}

[어댑터 경계 (P1-3 / P1B-3)]
- GraphView (UnityEditor.Experimental.GraphView) 미참조. HGraphCanvas + HGraphNode 2 파일 한정 유지.
- 본 파일은 BaseNode + NodeCatalogSO + JsonUtility 만 의존. Editor asmdef 안에서 단순 유틸.

[JsonUtility 직렬화 보장]
- BaseNode 의 [SerializeField] uid / title 자동 직렬화 (Phase 0 P0-c).
- 도메인 서브의 추가 [SerializeField] 필드도 자동 포함.
- SerializeReference 필드는 JsonUtility 미지원 — BaseNode 에 그런 필드 없음.
- typeName = AssemblyQualifiedName (Q5-A) → HCUP 패키지 분리 환경에서도 Type.GetType 정확 동작.

[도메인별 magic 정책 - 사용자 결정 (2026-05-08)]
- "노드는 추후 다양한 형태로 확장될 것이기에 개별 매직 스트링이 필요" - 사용자 spec.
- SimpleNode → "HGRAPH_SIMPLE_NODE_V1", DialogueNode → "HGRAPH_DIALOGUE_NODE_V1" 등.
- Wrapper magic = BaseNode.ClipboardMagic 의 값 (도메인 서브 override).
- Mixed 도메인 selection 거부 — Serialize 가 ClipboardMagic 일관성 검사 후 null 반환.

=============================================================================

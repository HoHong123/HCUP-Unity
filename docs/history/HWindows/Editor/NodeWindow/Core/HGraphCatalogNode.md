---
script_path: Assets/01_Scripts/HCUP-Unity/HWindows/Editor/NodeWindow/Core/HGraphCatalogNode.cs
script_name: HGraphCatalogNode
latest_log_id: LOG-20260511-1
total_entries: 4
created: 2026-05-12
updated: 2026-05-12
---

# HGraphCatalogNode Dev Log History

`Assets/01_Scripts/HCUP-Unity/HWindows/Editor/NodeWindow/Core/HGraphCatalogNode.cs` 의 Dev Log 엔트리 풀 본문이 본 파일에 시간 역순으로 보관됩니다 (최신이 위). .cs 파일 안에는 최신 3 엔트리의 1-줄 요약 view 가 유지되며, **본 history MD 가 ground truth** — 모든 엔트리의 풀 본문은 손실 없이 본 파일에 보관됩니다.

본 파일은 unity-devlog-history-mirror 스킬에 의해 사용자 명시 요청으로 생성되었습니다 (2026-05-12). "이전 — 제거됨" 엔트리도 포함 — 시도+폐기 이력 보존.

=============================================================================
@Jason - PKH 2026.05.11 CatalogNode 루트 설정 제약 [LOG-20260511-1]

# 변경
- BuildContextualMenu 에서 "루트 노드 재설정 (Set as Root)" 항목 제거.
- 헤더 주석 허용 항목 갱신: 이동/삭제/붙여넣기만 허용 명시.

# 이유
- CatalogNode 는 외부 카탈로그 참조 역할만 담당. 루트는 일반 노드가 맡아야 함.
- NodeCatalogAuthor.SetRoot 에도 CatalogNode 타입 가드 추가 (backend 이중 방어).

=============================================================================
@Jason - PKH 2026.05.10 Phase 3+ — 다중 출구 Port 제거 (HubNode 분리) [LOG-20260510-2]

# 변경
- 동적 포트 시스템 전면 제거: _outputPorts / _outputPortColumn / EnsureOutputPorts /
  AddSpareOutputPort / GetOutputPortIndex / UpdateOutputPortLabel / _BuildPorts override.
- GetOutputPort override 제거 → base 단일 outputPort 반환으로 복귀.
- 포트 구성: 입구 1개 + 출구 1개 (base _BuildPorts 기본 동작).

# 이유
- 사양 재정의: CatalogNode 는 "외부 카탈로그 연결 표시" 단순 역할.
  다중 라우팅 → HubNode 가 전담. 역할 단일화 + 오류 원인 제거.
- 이전 구현 오류: CatalogNode 의 동적 포트 수가 "A에서 C로 들어오는 연결 수"가 아닌
  "자신의 outgoing 연결 수"로 계산되어 요구사항과 불일치.

=============================================================================
@Jason - PKH 2026.05.10 Phase 3+ — 동적 출력 포트 + 양방향 생성 지원 [LOG-20260510-1b]

# 변경 (이전 — 제거됨)
- _BuildPorts override: 입력 1개 + _outputPortColumn(세로 컬럼) 레이아웃.
- EnsureOutputPorts(count) / AddSpareOutputPort / GetOutputPortIndex / UpdateOutputPortLabel.

=============================================================================
@Jason - PKH 2026.05.10 Phase 3 — HGraphCatalogNode.cs 베이스 코드 생성 [LOG-20260510-1]

# 목적
- CatalogNode 데이터를 시각화하는 Editor 전용 GraphView 노드.
- 더블클릭 네비게이션 + 복사/복제 차단 + 참조 카탈로그 ObjectField.

# 사용 흐름
- HGraphCanvas._PopulateInternal: data is CatalogNode 분기에서 생성.
- OnHeaderDoubleClick → canvas.RequestCatalogSwitch → HGraphWindow._BindCatalog.
- body ObjectField 변경 → Undo.RecordObject(cn) + SetReferencedCatalog + SetDirty.

=============================================================================

---
script_path: Assets/01_Scripts/HCUP-Unity/HWindows/Editor/NodeWindow/Core/HGraphHubNode.cs
script_name: HGraphHubNode
latest_log_id: LOG-20260511-3
total_entries: 4
created: 2026-05-12
updated: 2026-05-12
---

# HGraphHubNode Dev Log History

`Assets/01_Scripts/HCUP-Unity/HWindows/Editor/NodeWindow/Core/HGraphHubNode.cs` 의 Dev Log 엔트리 풀 본문이 본 파일에 시간 역순으로 보관됩니다 (최신이 위). .cs 파일 안에는 최신 3 엔트리의 1-줄 요약 view 가 유지되며, **본 history MD 가 ground truth** — 모든 엔트리의 풀 본문은 손실 없이 본 파일에 보관됩니다.

본 파일은 unity-devlog-history-mirror 스킬에 의해 사용자 명시 요청으로 생성되었습니다 (2026-05-12).

=============================================================================
@Jason - PKH 2026.05.11 — RefreshPortLabels override + portName 갱신 일원화 [LOG-20260511-3]

# 변경
- using System.Linq 추가 (port.connections.Count()).
- RefreshPortLabels() override: 입구 "Input (N)" + 출구 각 "Key (N)" 형식.
- _OnKeysChanged(): 직접 portName 루프 제거 → RefreshPortLabels() 위임.

# 이유
- 포트 라벨 갱신 진입점 일원화 — Inspector 키 수정 / 캔버스 repopulate 가 동일 포맷 사용.
- 연결 수 + 키값을 한 형식으로 묶어 사용자가 라우팅 상태 즉시 확인.

=============================================================================
@Jason - PKH 2026.05.11 — Inspector 키값 수정 즉시 동기화 [LOG-20260511-2]

# 변경
- 생성자: dataNode.KeysChanged += _OnKeysChanged 구독.
  RegisterCallback<DetachFromPanelEvent> 로 패널 분리 시 구독 해제 — 메모리 누수 방지.
- _OnKeysChanged(): 포트 수 동기화(추가/제거) + portName 갱신 + _RebuildEntryList.

# 이유
- entries [HReadOnly] 제거로 Inspector 직접 편집 가능. HubNode.OnValidate → KeysChanged 발화.
  전체 CatalogMutated repopulate 없이 뷰만 부분 갱신하는 경량 경로.

=============================================================================
@Jason - PKH 2026.05.11 — 입구 포트 portName "Input" 부여 [LOG-20260511-1]

# 변경
- _BuildPorts() override: inputPort.portName = "Input" ("" → "Input").
- 출구 포트는 _AddOutputPort 가 이미 hub.Entries[i].Key 를 portName 으로 부여.
  USS 라벨 노출과 결합되어 도트 좌측에 키가 자동 표시됨.

# 이유
- 전역 USS 규칙 `.port > #type { display: none }` 제거에 따라 portName 이 화면에 노출.
  이전 라운드의 별도 Label 래핑 접근은 불필요 — Unity 표준 portName 으로 일원화.

=============================================================================
@Jason - PKH 2026.05.10 Phase 3+ — HGraphHubNode 베이스 코드 생성 [LOG-20260510-1]

# 목적
- HubNode 데이터의 시각 노드. 입구 1개 + 출구 N개 (키 목록 기반).
- CatalogNode 에서 다중 포트 기능 분리 — 역할 단일화.

# 사용 흐름
- HGraphCanvas._PopulateInternal: data is HubNode → HGraphHubNode 생성.
- EnsureOutputPorts(hub.PortCount) → 키 수만큼 포트 사전 생성.
- GetOutputPortByKey(key): HubNodeEdge.BranchPortKey 로 정확한 포트 조회.
- body Add/Remove 버튼 → Author.AddHubEntry / RemoveHubEntry → CatalogMutated → repopulate.

=============================================================================

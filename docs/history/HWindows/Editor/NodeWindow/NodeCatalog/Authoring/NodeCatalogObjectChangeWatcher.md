---
script_path: Assets/01_Scripts/HCUP-Unity/HWindows/Editor/NodeWindow/NodeCatalog/Authoring/NodeCatalogObjectChangeWatcher.cs
script_name: NodeCatalogObjectChangeWatcher
latest_log_id: LOG-20260511-1
total_entries: 2
created: 2026-05-12
updated: 2026-05-12
---

# NodeCatalogObjectChangeWatcher Dev Log History

`Assets/01_Scripts/HCUP-Unity/HWindows/Editor/NodeWindow/NodeCatalog/Authoring/NodeCatalogObjectChangeWatcher.cs` 의 Dev Log 엔트리 풀 본문이 본 파일에 시간 역순으로 보관됩니다 (최신이 위). .cs 파일 안에는 최신 3 엔트리의 1-줄 요약 view 가 유지되며, **본 history MD 가 ground truth** — 모든 엔트리의 풀 본문은 손실 없이 본 파일에 보관됩니다.

본 파일은 unity-devlog-history-mirror 스킬에 의해 사용자 명시 요청으로 생성되었습니다 (2026-05-12). legacy 형식 엔트리 포함.

=============================================================================
@Jason - PKH 2026.05.11 — InstanceIDToObject → EntityIdToObject (Obsolete 수정) [LOG-20260511-1]

# 변경
- EditorUtility.InstanceIDToObject(data.instanceId)
  → EditorUtility.EntityIdToObject(data.instanceId).

# 이유
- Unity 6000.3.11f1 에서 InstanceIDToObject(int) 가 Obsolete 처리됨.
  동작 동일 — instance ID 로 UnityEngine.Object 역조회.

=============================================================================
@Jason - PKH 2026-04-25 NodeCatalogObjectChangeWatcher - Inspector 직접 수정 감지 [LOG-20260425-1]
=============================================================================

[존재 이유]
- NodeCatalogSO 본체에 OnValidate / 정적 이벤트를 두지 않으면서, Inspector 에서 catalog 의
  RootUID 등을 직접 수정해도 시각 레이어 (HGraphCanvas) 가 자동 새로고침되도록 보장.
- Author 의 mutation 메서드 호출은 이미 NodeCatalogAuthor.CatalogMutated 발송. 이 watcher 는
  Author 우회 경로 (Inspector SerializedProperty 직접 수정) 만 보강.

[Unity API]
- ObjectChangeEvents.changesPublished (Unity 2022+) 가 SerializedProperty Apply 시점에 발송.
- ChangeAssetObjectProperties 이벤트는 .asset 파일의 프로퍼티 변경 시 트리거.
- 더블 발송 우려: Author 가 SetDirty + SaveAssets 호출 시에도 이벤트 발생할 수 있음.
  _Populate idempotent 라 결과는 동일. 비효율 미미. 깜빡임 관찰 시 디바운스 도입.

[InitializeOnLoad]
- Editor 어셈블리 로드 시 한 번 실행. 정적 이벤트 구독 영구 유지.
- Domain reload 후에도 자동 재구독.

[필터]
- obj is NodeCatalogSO 만 통과. 다른 asset 변경은 무시 -> 성능 부담 0.

=============================================================================

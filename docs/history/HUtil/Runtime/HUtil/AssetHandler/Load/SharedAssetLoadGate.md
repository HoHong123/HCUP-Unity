---
script_path: Assets/01_Scripts/HCUP-Unity/HUtil/Runtime/HUtil/AssetHandler/Load/SharedAssetLoadGate.cs
script_name: SharedAssetLoadGate
latest_log_id: LOG-20260501-2
total_entries: 4
created: 2026-05-04
updated: 2026-05-04
---

# SharedAssetLoadGate Dev Log History

`Assets/01_Scripts/HCUP-Unity/HUtil/Runtime/HUtil/AssetHandler/Load/SharedAssetLoadGate.cs` 의 Dev Log 엔트리 풀 본문이 본 파일에 시간 역순으로 보관됩니다 (최신이 위). .cs 파일 안에는 최신 3 엔트리의 1-줄 요약 view 가 유지되며, **본 history MD 가 ground truth** — 모든 엔트리의 풀 본문은 손실 없이 본 파일에 보관됩니다.

본 파일은 unity-devlog-history-mirror 스킬에 의해 사용자 명시 요청으로 생성되었습니다 (2026-05-04). 작성자 표기가 없는 legacy 엔트리(`2026-04-26 (수정)`, `2026-04-25 (최초 설계)`) 도 포함됩니다 — 본 skill 의 invariant 상 모든 dated 엔트리는 history MD 에 보관되어야 하기 때문.

=============================================================================
@Jason - PKH 2026.05.01 RunAsync 의 캐시 task 를 UniTask → Task 변환으로 정정 (Preserve 정정) [LOG-20260501-2]

# 변경
- loadingTable 의 value type 을 Dictionary<TKey, UniTask<TAsset>> 에서 Dictionary<TKey, System.Threading.Tasks.Task<TAsset>> 로 전환.
- factory.Invoke() 결과에 .AsTask() 적용 후 loadingTable 에 저장.
- 이전 .Preserve() 적용은 정정 (=제거).

# 이유
- .Preserve() 가 만드는 MemoizeSource 는 결과 evaluate 후의 multi-await (= 결과 share) 만 안전. 결과 evaluate 진행 중 N caller 가 동시 suspend 하는 시나리오에선 underlying UniTaskCompletionSourceCore 의 single-continuation 제약을 forwarding 만 하여 두 번째 caller 의 OnCompleted 등록 시 throw.
- LoadGate 의 dedupe 는 정확히 후자 (factory in-flight 중 N caller 동시 await fan-out) 시나리오라 Preserve 부적합.
- System.Threading.Tasks.Task<T> 는 TaskAwaiter 가 OnCompleted callback 을 multi-continuation 리스트로 누적하므로 두 번째 caller 의 등록도 안전. UniTask.AsTask() 변환 1 회 alloc 비용은 cache miss 발화 한정이라 hot path 가 아님.
- 본 정정으로 SfxAgent → AudioManager.PrewarmSfxView 경로의 InvalidOperationException 차단.

=========================================================
@Jason - PKH 2026.05.01 RunAsync 의 캐시 task 를 Preserve 처리 [LOG-20260501-1]

# 변경
- factory.Invoke() 결과에 .Preserve() 적용 후 loadingTable 에 저장.

# 이유
- UniTask 는 struct + IUniTaskSource pool 기반이라 1 회 await 후 source 가 풀로 반환되어 동일 핸들의 두 번째 await 가 "Already continuation registered" 로 throw.
- LoadGate 의 의도가 N caller share 인 만큼 보존 wrapper (Preserve) 로 변환해 multi-awaitable 보장.
- SfxAgent → AudioManager.PrewarmSfxView 경로에서 InvalidOperationException 발생 사례로 결함 노출.

=========================================================
2026-04-26 (수정) :: 헤더 형틀 통합 + Dev Log 형식 도입 [LOG-20260426-1]
=========================================================
변경 ::
기존 헤더 (상단 도입+주의사항 + 하단 주요기능/사용법/이벤트/기타) 를 한 곳에 통합하여
§11 형틀 통일. 하단 Dev Log 영역 추가. 헤더와 Dev Log 모두 #if UNITY_EDITOR 가드.

이유 ::
글로벌 CLAUDE.md §11 룰 일괄 적용.

=========================================================
2026-04-25 (최초 설계) :: SharedAssetLoadGate 초기 구현 [LOG-20260425-1]
=========================================================
동일 key 동시 로드 dedupe 의 가장 단순한 구현 — Dictionary 한 개 + try/finally 한 개.
같은 key 가 동시에 N 번 요청되면 첫 호출만 factory 실행, 나머지 N-1 호출은 진행 중
UniTask 를 await. finally 에서 loadingTable.Remove 로 task 완료 후 즉시 정리 — 다음
호출은 다시 factory 실행 가능. 17 줄짜리 본문이 핵심 가치 (성능 + 방어적 cleanup 동시 달성).
=========================================================

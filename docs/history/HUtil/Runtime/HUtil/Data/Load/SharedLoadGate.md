---
script_path: Assets/01_Scripts/HCUP-Unity/HUtil/Runtime/HUtil/Data/Load/SharedLoadGate.cs
script_name: SharedLoadGate
latest_log_id: LOG-20260501-2
total_entries: 2
created: 2026-05-04
updated: 2026-05-04
---

# SharedLoadGate Dev Log History

`Assets/01_Scripts/HCUP-Unity/HUtil/Runtime/HUtil/Data/Load/SharedLoadGate.cs` 의 Dev Log 엔트리 풀 본문이 본 파일에 시간 역순으로 보관됩니다 (최신이 위). 엔트리가 3 개 이하라 .cs 파일은 변경되지 않았으며, **본 history MD 가 ground truth** — 향후 엔트리가 추가되어 4 개 이상이 되면 .cs 가 1-줄 요약 view 로 압축되고 풀 본문은 본 파일에 누적됩니다.

본 파일은 unity-devlog-history-mirror 스킬에 의해 사용자 명시 요청으로 생성되었습니다 (2026-05-04).

=============================================================================
@Jason - PKH 2026.05.01 RunAsync 의 캐시 task 를 UniTask → Task 변환으로 정정 (Preserve 정정) [LOG-20260501-2]

# 변경
- loading 의 value type 을 Dictionary<TKey, UniTask<TData>> 에서 Dictionary<TKey, System.Threading.Tasks.Task<TData>> 로 전환.
- factory.Invoke() 결과에 .AsTask() 적용 후 loading 에 저장.
- 이전 .Preserve() 적용은 정정 (=제거).

# 이유
- .Preserve() 가 만드는 MemoizeSource 는 결과 evaluate 진행 중 N caller 동시 suspend 시 underlying UniTaskCompletionSourceCore 의 single-continuation 제약을 그대로 forwarding 만 하여 두 번째 caller 가 throw.
- 자매 SharedAssetLoadGate 의 Preserve 정정과 동일 분석 결과를 일괄 적용.
- System.Threading.Tasks.Task<T> 의 multi-continuation 누적 동작으로 dedupe 게이트의 fan-out 의도와 자연 정합.

=============================================================================
@Jason - PKH 2026.05.01 RunAsync 의 캐시 task 를 Preserve 처리 [LOG-20260501-1]

# 변경
- factory.Invoke() 결과에 .Preserve() 적용 후 loading 에 저장.

# 이유
- UniTask 는 struct + IUniTaskSource pool 기반이라 1 회 await 후 source 가 풀로 반환되어 동일 핸들의 두 번째 await 가 "Already continuation registered" 로 throw.
- LoadGate 의 의도가 N caller share 인 만큼 보존 wrapper (Preserve) 로 변환해 multi-awaitable 보장.
- 자매 클래스 SharedAssetLoadGate 와 동일 결함이라 일괄 정정.

=============================================================================

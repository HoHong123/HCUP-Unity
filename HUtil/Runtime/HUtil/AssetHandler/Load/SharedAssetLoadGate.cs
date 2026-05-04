#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 동일 key 동시 로드를 공유하는 기본 게이트 구현. 17 줄짜리 dedupe + finally cleanup.
 *
 * 주요 기능 ::
 * loadingTable 로 진행 중 task 추적. 같은 key 요청은 한 UniTask 로 합쳐 source 호출 1 회.
 *
 * 사용법 ::
 * AssetProvider 가 _GetAsync 에서 source 로드를 본 게이트로 감쌈. 동일 key 연속 요청이
 * 발생하는 환경 (UI 다중 패널 같은 sprite 동시 요청 등) 에서 source 호출 비용 절감.
 *
 * 주의 ::
 * factory 는 예외 발생 시에도 정리 흐름 고려 (finally 에서 loadingTable.Remove). 게이트는
 * 결과 캐시가 아니라 진행 중 작업 공유만 담당 — 캐시 정책은 상위 provider 가 가져감.
 * =========================================================
 */
#endif

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.Assertions;

namespace HUtil.AssetHandler.Load {
    public sealed class SharedAssetLoadGate<TKey, TAsset> : IAssetLoadGate<TKey, TAsset> {
        #region Private - Fields
        readonly Dictionary<TKey, System.Threading.Tasks.Task<TAsset>> loadingTable = new();
        #endregion

        #region Public - Run
        public async UniTask<TAsset> RunAsync(TKey key, Func<UniTask<TAsset>> factory) {
            Assert.IsNotNull(factory, "[SharedAssetLoadGate] factory is null.");

            if (loadingTable.TryGetValue(key, out var runningTask)) {
                return await runningTask;
            }

            var newTask = factory.Invoke().AsTask();
            loadingTable[key] = newTask;

            try {
                return await newTask;
            }
            finally {
                loadingTable.Remove(key);
            }
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * Dev Log
 * =========================================================
 * @Jason - PKH 2026.05.01 RunAsync 의 캐시 task 를 UniTask → Task 변환으로 정정 (Preserve 정정)
 *
 * # 변경
 * - loadingTable 의 value type 을 Dictionary<TKey, UniTask<TAsset>> 에서 Dictionary<TKey, System.Threading.Tasks.Task<TAsset>> 로 전환.
 * - factory.Invoke() 결과에 .AsTask() 적용 후 loadingTable 에 저장.
 * - 이전 .Preserve() 적용은 정정 (=제거).
 *
 * # 이유
 * - .Preserve() 가 만드는 MemoizeSource 는 결과 evaluate 후의 multi-await (= 결과 share) 만 안전. 결과 evaluate 진행 중 N caller 가 동시 suspend 하는 시나리오에선 underlying UniTaskCompletionSourceCore 의 single-continuation 제약을 forwarding 만 하여 두 번째 caller 의 OnCompleted 등록 시 throw.
 * - LoadGate 의 dedupe 는 정확히 후자 (factory in-flight 중 N caller 동시 await fan-out) 시나리오라 Preserve 부적합.
 * - System.Threading.Tasks.Task<T> 는 TaskAwaiter 가 OnCompleted callback 을 multi-continuation 리스트로 누적하므로 두 번째 caller 의 등록도 안전. UniTask.AsTask() 변환 1 회 alloc 비용은 cache miss 발화 한정이라 hot path 가 아님.
 * - 본 정정으로 SfxAgent → AudioManager.PrewarmSfxView 경로의 InvalidOperationException 차단.
 *
 * =========================================================
 * @Jason - PKH 2026.05.01 RunAsync 의 캐시 task 를 Preserve 처리
 *
 * # 변경
 * - factory.Invoke() 결과에 .Preserve() 적용 후 loadingTable 에 저장.
 *
 * # 이유
 * - UniTask 는 struct + IUniTaskSource pool 기반이라 1 회 await 후 source 가 풀로 반환되어 동일 핸들의 두 번째 await 가 "Already continuation registered" 로 throw.
 * - LoadGate 의 의도가 N caller share 인 만큼 보존 wrapper (Preserve) 로 변환해 multi-awaitable 보장.
 * - SfxAgent → AudioManager.PrewarmSfxView 경로에서 InvalidOperationException 발생 사례로 결함 노출.
 *
 * =========================================================
 * 2026-04-26 (수정) :: 헤더 형틀 통합 + Dev Log 형식 도입
 * =========================================================
 * 변경 ::
 * 기존 헤더 (상단 도입+주의사항 + 하단 주요기능/사용법/이벤트/기타) 를 한 곳에 통합하여
 * §11 형틀 통일. 하단 Dev Log 영역 추가. 헤더와 Dev Log 모두 #if UNITY_EDITOR 가드.
 *
 * 이유 ::
 * 글로벌 CLAUDE.md §11 룰 일괄 적용.
 *
 * =========================================================
 * 2026-04-25 (최초 설계) :: SharedAssetLoadGate 초기 구현
 * =========================================================
 * 동일 key 동시 로드 dedupe 의 가장 단순한 구현 — Dictionary 한 개 + try/finally 한 개.
 * 같은 key 가 동시에 N 번 요청되면 첫 호출만 factory 실행, 나머지 N-1 호출은 진행 중
 * UniTask 를 await. finally 에서 loadingTable.Remove 로 task 완료 후 즉시 정리 — 다음
 * 호출은 다시 factory 실행 가능. 17 줄짜리 본문이 핵심 가치 (성능 + 방어적 cleanup 동시 달성).
 * =========================================================
 */
#endif

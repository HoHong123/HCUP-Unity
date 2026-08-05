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
using HDiagnosis.Logger;

namespace HResource.Load {
    public sealed class SharedAssetLoadGate<TKey, TAsset> : IAssetLoadGate<TKey, TAsset> {
        #region Private - Fields
        readonly Dictionary<TKey, System.Threading.Tasks.Task<TAsset>> loadingTable = new();
        #endregion

        #region Public - Run
        public async UniTask<TAsset> RunAsync(TKey key, Func<UniTask<TAsset>> factory) {
            if (factory == null) {
                HLogger.Throw(new ArgumentNullException(nameof(factory), "[SharedAssetLoadGate] factory is null."));
            }

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
 * @Jason - PKH 2026.05.01 RunAsync 의 캐시 task 를 UniTask → Task 변환으로 정정 (Preserve 정정) [LOG-20260501-2]
 * - dedupe 게이트의 fan-out 의도와 multi-continuation 정합 위해 Task 로 전환.
 * =========================================================
 * @Jason - PKH 2026.05.01 RunAsync 의 캐시 task 를 Preserve 처리 [LOG-20260501-1]
 * - UniTask single-continuation 제약 회피 위해 Preserve 적용 (이후 LOG-20260501-2 로 정정됨).
 * =========================================================
 * 2026-04-26 (수정) :: 헤더 형틀 통합 + Dev Log 형식 도입 [LOG-20260426-1]
 * - 글로벌 §11 형틀 통일 + #if UNITY_EDITOR 가드 적용.
 * =========================================================
 * > 이전 엔트리는 docs/history/HUtil/Runtime/HUtil/AssetHandler/Load/SharedAssetLoadGate.md 참조 (총 4 엔트리)
 * =========================================================
 */
#endif

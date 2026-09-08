#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 동일 key 동시 로드를 공유하는 기본 게이트 구현. 17 줄짜리 dedupe + finally cleanup.
 *
 * 주요 기능 ::
 * loadingTable 로 진행 중 task 추적. 같은 key 요청은 한 UniTask 로 합쳐 source 호출 1 회.
 *
 * 사용법 ::
 * AssetProvider 가 _GetAsync 에서 fetch mode 전 구간을 본 게이트로 감쌈. 우회 경로 0건.
 *
 * 주의 :: 이 게이트는 성능 최적화가 아니라 정합성 장치다. 제거하면 영구 잔존이 발생한다.
 * AddressableAssetLoader 는 handleTable 조회가 await 앞, 등록이 await 뒤다. 게이트가 없으면
 * 동시 요청 2건이 모두 LoadAssetAsync 를 불러 Addressables refcount 가 2 로 오르고,
 * handleTable 은 뒤엣것으로 덮이며, Release 1회로는 0 에 도달하지 못한다.
 * HResource 가 refcount 를 1 로 고정하기로 한 선택을 성립시키는 것이 이 게이트다.
 *
 * Resources 축은 대상이 아니다 - LoadAsync 가 동기라 병합할 진행 중 구간이 없다.
 * factory 는 예외 발생 시에도 정리 흐름 고려 (finally 에서 loadingTable.Remove). 게이트는
 * 결과 캐시가 아니라 진행 중 작업 공유만 담당 - 캐시 정책은 상위 provider 가 가져감.
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
 * 2026-09-08 (수정) :: 존재 이유를 성능에서 정합성으로 정정
 *
 * 변경 ::
 * 헤더의 "source 호출 비용 절감" 서술을 refcount 고정 유지 근거로 교체
 *
 * 이유 ::
 * Addressables 는 중복 로드를 막지만 참조 수를 올리며 막는다. HResource 는 그 수를 1 로
 * 고정하므로 진입 자체를 직렬화해야 한다. 성능으로 읽히면 제거 대상으로 오해된다
 *
 * 결과 ::
 * 게이트 제거가 왜 영구 잔존을 만드는지 헤더만 읽어도 드러난다
 *
 * 주의 ::
 * Resources 축은 대상이 아니다. LoadAsync 가 동기라 병합할 구간이 없다
 *
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

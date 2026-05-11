#if UNITY_EDITOR
/* =========================================================
 * 동일 Key에 대한 중복 데이터 로드를 방지하는 비동기 로드 게이트 클래스입니다.
 *
 * 주의사항 ::
 * 동일 Key 요청이 동시에 발생하면 최초 Task를 공유합니다.
 * =========================================================
 */
#endif

using Cysharp.Threading.Tasks;

namespace HUtil.Data.Load {
    public sealed class SharedLoadGate<TKey, TData> {
        #region Fields
        readonly System.Collections.Generic.Dictionary<TKey, System.Threading.Tasks.Task<TData>> loading = new();
        #endregion

        #region Public - Run
        public async UniTask<TData> RunAsync(TKey key, System.Func<UniTask<TData>> factory) {
#if UNITY_ASSERTIONS
            UnityEngine.Assertions.Assert.IsNotNull(factory);
#endif
            if (loading.TryGetValue(key, out var runningTask))
                return await runningTask;

            var newTask = factory.Invoke().AsTask();
            loading[key] = newTask;

            try {
                return await newTask;
            }
            // finally로 remove를 보장하여 예외/취소에도 게이트가 영구 잠기지 않음
            finally {
                loading.Remove(key);
            }
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 *
 * 주요 기능 ::
 * 1. RunAsync
 *    + 동일 Key 요청 병합
 *
 * 사용법 ::
 * 1. RunAsync(key,factory) 호출
 *
 * 기타 ::
 * 1. 중복 다운로드 방지 목적의 Gate 시스템입니다.
 * =========================================================
 */
#endif

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.01 RunAsync 의 캐시 task 를 UniTask → Task 변환으로 정정 (Preserve 정정)
 *
 * # 변경
 * - loading 의 value type 을 Dictionary<TKey, UniTask<TData>> 에서 Dictionary<TKey, System.Threading.Tasks.Task<TData>> 로 전환.
 * - factory.Invoke() 결과에 .AsTask() 적용 후 loading 에 저장.
 * - 이전 .Preserve() 적용은 정정 (=제거).
 *
 * # 이유
 * - .Preserve() 가 만드는 MemoizeSource 는 결과 evaluate 진행 중 N caller 동시 suspend 시 underlying UniTaskCompletionSourceCore 의 single-continuation 제약을 그대로 forwarding 만 하여 두 번째 caller 가 throw.
 * - 자매 SharedAssetLoadGate 의 Preserve 정정과 동일 분석 결과를 일괄 적용.
 * - System.Threading.Tasks.Task<T> 의 multi-continuation 누적 동작으로 dedupe 게이트의 fan-out 의도와 자연 정합.
 *
 * =============================================================================
 * @Jason - PKH 2026.05.01 RunAsync 의 캐시 task 를 Preserve 처리
 *
 * # 변경
 * - factory.Invoke() 결과에 .Preserve() 적용 후 loading 에 저장.
 *
 * # 이유
 * - UniTask 는 struct + IUniTaskSource pool 기반이라 1 회 await 후 source 가 풀로 반환되어 동일 핸들의 두 번째 await 가 "Already continuation registered" 로 throw.
 * - LoadGate 의 의도가 N caller share 인 만큼 보존 wrapper (Preserve) 로 변환해 multi-awaitable 보장.
 * - 자매 클래스 SharedAssetLoadGate 와 동일 결함이라 일괄 정정.
 *
 * =============================================================================
 */
#endif

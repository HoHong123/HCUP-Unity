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
        readonly System.Collections.Generic.Dictionary<TKey, UniTask<TData>> loading = new();
        #endregion

        #region Public - Run
        public async UniTask<TData> RunAsync(TKey key, System.Func<UniTask<TData>> factory) {
#if UNITY_ASSERTIONS
            UnityEngine.Assertions.Assert.IsNotNull(factory);
#endif
            if (loading.TryGetValue(key, out var runningTask)) return await runningTask;

            var newTask = factory.Invoke();
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
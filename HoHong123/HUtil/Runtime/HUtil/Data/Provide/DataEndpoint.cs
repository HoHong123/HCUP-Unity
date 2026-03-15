#if UNITY_EDITOR
/* =========================================================
 * 데이터 로드, 캐시, 중복 요청 방지 및 영속 저장을 통합 관리하는 데이터 엔드포인트 클래스입니다.
 *
 * 데이터 처리 흐름 ::
 * Cache → Load → Cache Save → Optional Persistent Save
 *
 * 주의사항 ::
 * 1. SharedLoadGate를 사용하여 동일 Key 중복 로드를 방지합니다.
 * =========================================================
 */
#endif

using Cysharp.Threading.Tasks;
using HUtil.Data.Save;
using HUtil.Data.Load;
using HUtil.Data.Cache;

namespace HUtil.Data.Provider {
    public sealed class DataEndpoint<TKey, TData> {
        #region Fields
        readonly DataLoader<TKey, TData> handler; // 원본 데이터 로더 선택기
        readonly SharedLoadGate<TKey, TData> loadGate; // 중복 로드 방지 게이트
        readonly IDataCache<TKey, TData> cacheStore; // 캐시 저장소
        readonly IDataSave<TKey, TData> dataStore; // (옵션) 영속 저장소
        #endregion

        #region Public - Construction
        public DataEndpoint(
            DataLoader<TKey, TData> handler,
            IDataCache<TKey, TData> cacheStore,
            SharedLoadGate<TKey, TData> loadGate = null,
            IDataSave<TKey, TData> dataStore = null) {
#if UNITY_ASSERTIONS
            UnityEngine.Assertions.Assert.IsNotNull(handler);
            UnityEngine.Assertions.Assert.IsNotNull(cacheStore);
#endif
            this.handler = handler;
            this.cacheStore = cacheStore;
            this.loadGate = loadGate ?? new SharedLoadGate<TKey, TData>();
            this.dataStore = dataStore;
        }
        #endregion

        #region Public - Get 
        /// 1. 캐시 우선(useCache=true)
        /// 2. 없으면 원본 로드
        /// 3. 캐시 저장
        /// 4. (옵션) 영속 저장
        /// forceRefresh=true면 캐시를 무시하고 강제로 원본 로드 진행
        public UniTask<TData> GetAsync(
            TKey key,
            DataLoadType loadType,
            bool useCache,
            bool forceRefresh = false) {
            if (forceRefresh) return _FetchAndCacheAsync(key, loadType);
            if (useCache) return _GetCacheOrFetchAsync(key, loadType);
            return _FetchAndCacheAsync(key, loadType);
        }
        #endregion

        #region Public - Remove
        public void RemoveCache(TKey key) => cacheStore.ForceRemove(key);
        #endregion

        #region Public - Clear 
        public void ClearCache() => cacheStore.Clear();
        #endregion

        #region Private - Fetch
        private async UniTask<TData> _GetCacheOrFetchAsync(TKey key, DataLoadType loadType) {
            // 캐시 확인
            if (cacheStore.TryGet(key, out var cached)) return cached;
            // 캐시에 없으면 로드
            return await _FetchAndCacheAsync(key, loadType);
        }

        private async UniTask<TData> _FetchAndCacheAsync(TKey key, DataLoadType loadType) {
            return await loadGate.RunAsync(key, async () => {
                // 기존 캐시에 있으면 의존성을 높이고 반환
                if (cacheStore.TryLoad(key, out var cached)) return cached;

                // 데이터 게이트 사용
                var loader = handler.Resolve(loadType);
#if UNITY_ASSERTIONS
                UnityEngine.Assertions.Assert.IsNotNull(loader);
#endif
                // 데이터 다운로드
                var data = await loader.LoadAsync(key);

                // 캐시 저장
                cacheStore.Save(key, data);
                
                // 추가 저장 로직이 필요하면 실행
                if (dataStore != null) await dataStore.SaveAsync(key, data);

                return data;
            });
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 *
 * 주요 기능 ::
 * 1. GetAsync
 *    + 캐시 또는 원본 로드로 데이터 반환
 * 2. RemoveCache
 *    + 특정 캐시 제거
 * 3. ClearCache
 *    + 캐시 전체 제거
 *
 * 사용법 ::
 * 1. DataLoader, IDataCache, SharedLoadGate를 전달하여 생성합니다.
 * 2. GetAsync 호출로 데이터 로드를 수행합니다.
 *
 * 기타 ::
 * 1. IDataSave 구현체가 존재할 경우 자동 영속 저장이 수행됩니다.
 * =========================================================
 */
#endif
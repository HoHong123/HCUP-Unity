#if UNITY_EDITOR
/* =========================================================
 * Unity Asset 데이터를 로드하고 캐싱하는 Provider 클래스입니다.
 * Asset 로드, 캐시 관리, 의존성 관리 및 데이터 엔드포인트 흐름을 통합 관리하는 역할을 수행합니다.
 *
 * 주의사항 ::
 * 1. AssetProvider는 DataEndpoint를 통해 실제 로드 흐름을 처리합니다.
 * 2. ReleaseId 호출 시 캐시 의존성이 감소합니다.
 * =========================================================
 */
#endif

#if UNITY_EDITOR && ODIN_INSPECTOR
using System.Collections.Generic;
#endif
using Cysharp.Threading.Tasks;
using UnityEngine.Assertions;
using HUtil.Data.Cache;
using HUtil.Data.Provider;

namespace HUtil.Data.Load {
    /// <summary>
    /// Resources data provider
    /// </summary>
    /// <typeparam name="TAsset">'Resources/' data type</typeparam>
    public sealed class AssetProvider<TAsset> : IDataProvider<string, TAsset>
        where TAsset : UnityEngine.Object {
        #region Fields
        readonly DataEndpoint<string, TAsset> endpoint;
        readonly BaseDataCache<string, TAsset> cache;
        readonly IReleasableDataLoad<string, TAsset> releasableLoader;
        readonly DataLoadType loadType;
        #endregion

        #region Properties
        public DataLoadType Type => loadType;
#if UNITY_EDITOR && ODIN_INSPECTOR
        public BaseDataCache<string,TAsset> Cache => cache;
        public IReadOnlyDictionary<string, BaseDataCache<string, TAsset>.Item> Preview => cache.Preview;
#endif
        #endregion

        #region Public - Init
        public AssetProvider(
            DataLoadType loadType,
            IDataLoad<string, TAsset> loader,
            BaseDataCache<string, TAsset> cache) {

            Assert.IsNotNull(loader);
            Assert.IsNotNull(cache);

            this.loadType = loadType;
            this.cache = cache;
            releasableLoader = loader as IReleasableDataLoad<string, TAsset>;

            var handler = new DataLoader<string, TAsset>(loader);
            endpoint = new DataEndpoint<string, TAsset>(
                handler: handler,
                cacheStore: cache,
                loadGate: new SharedLoadGate<string, TAsset>(),
                dataStore: null
            );

            cache.OnDataRemoved += _OnDataRemoved;
        }
        #endregion

        #region Public - Prewarm
        public async UniTask PrewarmIdAsync(string id) => await GetOrLoadAsync(id);
        #endregion

        #region Public - Get
        public bool TryGet(string id, out TAsset data) => cache.TryGet(id, out data);

        public async UniTask<TAsset> GetOrLoadAsync(string id) =>
            await endpoint.GetAsync(
                key: id,
                loadType: loadType,
                useCache: true,
                forceRefresh: false);

        public async UniTask<TAsset> GetOrLoadAsync(string id, object owner) =>
            await endpoint.GetAsync(
                key: id,
                loadType: loadType,
                useCache: true,
                forceRefresh: false,
                owner: owner);
        #endregion

        #region Public - Release
        public void ReleaseId(string id) => cache.Release(id);
        public void ReleaseId(string id, object owner) => cache.Release(id, owner);
        public int ReleaseOwner(object owner) => cache.ReleaseOwner(owner);
        #endregion

        #region Public - Prune
        public void Prune() {
            cache.Prune();
        }
        #endregion

        #region Public - Clear
        public void Clear() {
            cache.Clear();
        }
        #endregion

        #region Private - Cache Event
        private void _OnDataRemoved(string key, TAsset data) {
            releasableLoader.Release(key);
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 *
 * 주요 기능 ::
 * 1. PrewarmIdAsync
 *    + 특정 Asset을 사전 로드
 * 2. GetOrLoadAsync
 *    + 캐시 확인 후 없으면 로드
 * 3. TryGet
 *    + 캐시에서만 데이터 조회
 * 4. ReleaseId
 *    + Asset 의존성 감소
 * 5. Prune / Clear
 *    + 캐시 정리 및 초기화
 *
 * 사용법 ::
 * 1. AssetProvider 생성 시 Loader와 Cache를 전달합니다.
 * 2. GetOrLoadAsync로 Asset을 로드합니다.
 *
 * 기타 ::
 * 1. DataEndpoint를 통해 Loader / Cache / Gate를 통합 관리합니다.
 * 2. Releasable Loader는 Cache 제거 이벤트와 연동됩니다.
 * =========================================================
 */
#endif

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using HDiagnosis.Logger;
using HAudio.Catalog;
using HAudio.Core;
using HResource.Data;
using HResource.Provider;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Audio 도메인에서 AssetProvider<string, AudioClip>를 감싸는 저장소 스크립트입니다.
 *
 * 주의사항 ::
 * 1. catalogRegistry 없이 생성하면 token 해석을 제대로 수행할 수 없습니다.
 * 2. Resources 모드에서는 catalog의 path 정보가 필요합니다.
 * =========================================================
 */
#endif

namespace HAudio.Repository {
    public sealed class AudioClipRepository : IAudioClipRepository {
        #region Fields
        readonly AudioCatalogRegistry catalogRegistry;
        readonly IAssetSource<string, AudioClip> assetProvider;
        // 이 저장소가 잡는 모든 자산의 소유자. 생성자에서 한 번만 받는다.
        // 소유자가 파괴되면 provider 의 프로브가 이 저장소 몫을 자동 회수한다.
        readonly Component owner;
        // uid → 최종 loadKey 해석 캐시.
        readonly Dictionary<int, string> loadKeyByUid = new();
        readonly bool ownsAssetProvider;
        bool disposed;
        #endregion

        #region Properties
        public AssetLoadMode LoadMode { get; }

        public bool IsDisposed => disposed;
        public bool OwnsAssetProvider => ownsAssetProvider;
        #endregion

        #region Public - Constructors
        public AudioClipRepository(
            AssetLoadMode loadMode,
            AudioCatalogRegistry catalogRegistry,
            Component owner,
            IAssetSource<string, AudioClip> assetProvider = null) {

            if (catalogRegistry == null) {
                HLogger.Throw(new System.ArgumentNullException(nameof(catalogRegistry), "[AudioClipRepository] catalogRegistry is null."));
            }
            if (owner == null) {
                HLogger.Throw(new System.ArgumentNullException(
                    nameof(owner),
                    "[AudioClipRepository] owner is null or destroyed. Every occupancy must be attributable to a live Component."));
            }
            if (loadMode != AssetLoadMode.Resources && loadMode != AssetLoadMode.Addressable) {
                HLogger.Throw(new System.ArgumentException($"[AudioClipRepository] Unsupported load mode. loadMode={loadMode}", nameof(loadMode)));
            }

            LoadMode = loadMode;
            this.catalogRegistry = catalogRegistry;
            this.owner = owner;
            ownsAssetProvider = assetProvider == null;
            this.assetProvider = assetProvider ?? _CreateDefaultProvider(loadMode);
        }
        #endregion

        #region Public - Get
        /// <summary> uid 로 조회한다. 재생 경로의 기본 진입점 - 문자열 정규화·할당이 없다. </summary>
        public bool TryGet(int uid, out AudioClip clip) {
            clip = null;
            if (!_TryBuildLoadKey(uid, out string loadKey)) return false;
            return assetProvider.TryGet(loadKey, out clip) && clip;
        }

        public bool TryGet(string token, out AudioClip clip) {
            clip = null;
            if (!_TryBuildLoadKey(token, out string loadKey)) return false;
            return assetProvider.TryGet(loadKey, out clip) && clip;
        }

        public UniTask<AudioClip> GetOrLoadAsync(
            int uid,
            AssetFetchMode fetchMode = AssetFetchMode.CacheFirst) {

            if (!_TryBuildLoadKey(uid, out string loadKey)) {
                return UniTask.FromResult<AudioClip>(null);
            }

            return assetProvider.GetAsync(owner, loadKey, LoadMode, fetchMode);
        }

        public UniTask<AudioClip> GetOrLoadAsync(
            string token,
            AssetFetchMode fetchMode = AssetFetchMode.CacheFirst) {

            if (!_TryBuildLoadKey(token, out string loadKey)) {
                return UniTask.FromResult<AudioClip>(null);
            }

            return assetProvider.GetAsync(owner, loadKey, LoadMode, fetchMode);
        }
        #endregion

        #region Public - Prewarm
        public async UniTask PrewarmTokenAsync(
            int uid,
            AssetFetchMode fetchMode = AssetFetchMode.CacheFirst) {

            await GetOrLoadAsync(uid, fetchMode);
        }

        public async UniTask PrewarmTokenAsync(
            string token,
            AssetFetchMode fetchMode = AssetFetchMode.CacheFirst) {

            await GetOrLoadAsync(token, fetchMode);
        }

        public async UniTask PrewarmCatalogAsync(
            AudioCatalogSO catalog,
            AssetFetchMode fetchMode = AssetFetchMode.CacheFirst) {

            if (!catalog) return;   // null/파괴 가드. Assert 는 릴리즈에서 제거되므로 런타임 가드만 유지

            catalogRegistry.RegisterCatalog(catalog);

            List<UniTask> tasks = new List<UniTask>(catalog.Entries.Count);
            foreach (var entry in catalog.Entries) {
                if (entry == null) continue;
                tasks.Add(GetOrLoadAsync(entry.Token, fetchMode));
            }

            await UniTask.WhenAll(tasks);
        }
        #endregion

        #region Public - Release
        public bool Release(int uid) {
            if (!_TryBuildLoadKey(uid, out string loadKey)) return false;
            return assetProvider.Release(owner, loadKey);
        }

        public bool Release(string token) {
            if (!_TryBuildLoadKey(token, out string loadKey)) return false;
            return assetProvider.Release(owner, loadKey);
        }

        public void ReleaseCatalog(AudioCatalogSO catalog) {
            if (!catalog) return;

            List<AudioCatalogSO.Entry> removedEntries = new List<AudioCatalogSO.Entry>();
            int refCount = catalogRegistry.ReleaseCatalog(catalog, removedEntries);
            if (refCount > 0) return;

            foreach (var entry in removedEntries) {
                // 같은 uid 가 경로가 다른 카탈로그로 재등록하면 남아있던 loadKey 가 옛 경로를 가리켜 조용히 잘못된 자산을 찾음.
                if (entry != null && entry.Uid != 0) loadKeyByUid.Remove(entry.Uid);

                string loadKey = _ResolveLoadKey(entry);
                if (string.IsNullOrWhiteSpace(loadKey)) continue;

                assetProvider.Release(owner, loadKey);
            }
        }

        /// <summary> 이 저장소 소유자의 점유를 전부 반납한다. 다른 소유자의 점유는 건드리지 않는다. </summary>
        public void ReleaseAll() {
            loadKeyByUid.Clear();
            assetProvider.ReleaseOwner(owner);
        }

        /// <summary>
        /// 저장소 폐기. 기본 provider 를 이 저장소가 만들었을 때만 provider 까지 함께 폐기한다.
        /// 주입받은 provider 는 수명 소유자가 따로 있으므로 아무것도 하지 않는다.
        /// </summary>
        public void Dispose() {
            if (disposed) return;
            disposed = true;

            loadKeyByUid.Clear();

            // 이제 이 저장소는 자기 몫을 소유자로 식별할 수 있다.
            // 주입받은 provider 라도 자기 점유만 정확히 내려놓을 수 있으므로 그냥 두지 않는다 (케이스 리포트 07 COR-1 해소).
            assetProvider.ReleaseOwner(owner);
            if (!ownsAssetProvider) return;

            assetProvider.Dispose();
        }
        #endregion

        #region Private - Resolve
        private bool _TryBuildLoadKey(int uid, out string loadKey) {
            // 히트하면 여기서 끝난다 - int 해시 1회, 할당 0.
            if (loadKeyByUid.TryGetValue(uid, out loadKey)) {
                return !string.IsNullOrWhiteSpace(loadKey);
            }

            if (!catalogRegistry.TryGetEntry(uid, out AudioCatalogSO.Entry entry)) {
                loadKey = string.Empty;
                return false;
            }

            loadKey = _ResolveLoadKey(entry);
            if (string.IsNullOrWhiteSpace(loadKey)) return false;

            loadKeyByUid[uid] = loadKey;
            return true;
        }

        private bool _TryBuildLoadKey(string token, out string loadKey) {
            loadKey = string.Empty;

            string normalizedToken = _NormalizeToken(token);
            if (string.IsNullOrWhiteSpace(normalizedToken)) return false;
            if (catalogRegistry.TryGetEntry(normalizedToken, out AudioCatalogSO.Entry entry)) {
                loadKey = _ResolveLoadKey(entry);
                return !string.IsNullOrWhiteSpace(loadKey);
            }

            if (LoadMode != AssetLoadMode.Addressable) return false;
            loadKey = normalizedToken;
            return true;
        }

        private string _ResolveLoadKey(AudioCatalogSO.Entry entry) {
            if (entry == null) return string.Empty;

            return LoadMode switch {
                AssetLoadMode.Resources => AudioCatalogSO.BuildResourcesLoadKey(entry.Path, entry.Token),
                AssetLoadMode.Addressable => _ResolveAddressableLoadKey(entry),
                _ => string.Empty
            };
        }

        private string _ResolveAddressableLoadKey(AudioCatalogSO.Entry entry) {
            if (entry == null) return string.Empty;
            return _NormalizeToken(entry.Token);
        }

        private IAssetSource<string, AudioClip> _CreateDefaultProvider(AssetLoadMode loadMode) {
            return loadMode switch {
                AssetLoadMode.Resources => AssetProviderFactory.CreateResources<AudioClip>(string.Empty),
                AssetLoadMode.Addressable => AssetProviderFactory.CreateAddressable<AudioClip>(),
                _ => null
            };
        }

        private string _NormalizeToken(string token) {
            if (string.IsNullOrWhiteSpace(token)) return string.Empty;
            return token.Trim();
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 *  Dev Log
 * =========================================================
 * @Jason - PKH
 * 주요 기능 ::
 * 1. token과 catalog를 실제 load key로 해석합니다.
 * 2. catalog 단위 preload와 release를 제공합니다.
 * 3. owner 기준 release를 provider에 위임합니다.
 *
 * 사용법 ::
 * 1. loadMode와 catalogRegistry를 넘겨 생성합니다.
 * 2. 사용 전 PrewarmTokenAsync 또는 PrewarmCatalogAsync를 호출합니다.
 * 3. 해제는 Release, ReleaseCatalog, ReleaseOwner로 수행합니다.
 *
 * 이벤트 ::
 * 1. catalog preload 시 registry 등록이 함께 일어납니다.
 * 2. release 시 provider와 cache에 정리 요청을 전달합니다.
 *
 * 기타 ::
 * 1. Addressable 모드에서는 token 직접 해석 fallback을 허용합니다.
 * 2. 실제 source 호출은 AssetProvider가 담당합니다.
 * =========================================================
 * @Jason - PKH 2026.08.07 IsDisposed/OwnsAssetProvider 진단 프로퍼티 추가 (케이스 리포트 07 TST-2)
 *
 * # 변경
 * - `disposed` / `ownsAssetProvider` 필드를 읽기 전용으로 노출하는
 *   `public bool IsDisposed` / `public bool OwnsAssetProvider` 프로퍼티 신설
 *
 * # 이유
 * - COR-1(주입받은 공유 provider 에 `ReleaseAll`)의 분기(`ownsAssetProvider`)를 테스트가
 *   생성자 인자로만 추론해야 했다. 둘 다 private 이라 관측 수단이 없었다
 * - 상태를 바꾸지 않는 읽기 전용이라 `Dispose()` 의 기존 분기 로직에는 영향이 없다
 * =========================================================
 */
#endif

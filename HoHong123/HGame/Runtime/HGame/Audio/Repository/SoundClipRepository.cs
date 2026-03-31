using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using HGame.Audio.Catalog;
using HGame.Sound.Core;
using HUtil.AssetHandler.Data;
using HUtil.AssetHandler.Provider;
using HUtil.AssetHandler.Subscription;

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

namespace HGame.Audio.Repository {
    public sealed partial class SoundClipRepository : ISoundClipRepository {
        #region Fields
        readonly SoundCatalogRegistry catalogRegistry;
        readonly IAssetProvider<string, AudioClip> assetProvider;
        #endregion

        #region Properties
        public AssetLoadMode LoadMode { get; }
        #endregion

        #region Public - Constructors
        public SoundClipRepository(
            AssetLoadMode loadMode,
            SoundCatalogRegistry catalogRegistry,
            IAssetProvider<string, AudioClip> assetProvider = null) {

            Assert.IsNotNull(catalogRegistry, "[SoundClipRepository] catalogRegistry is null.");
            Assert.IsTrue(
                loadMode == AssetLoadMode.Resources || loadMode == AssetLoadMode.Addressable,
                $"[SoundClipRepository] Unsupported load mode. loadMode={loadMode}");

            LoadMode = loadMode;
            this.catalogRegistry = catalogRegistry;
            this.assetProvider = assetProvider ?? _CreateDefaultProvider(loadMode);
        }
        #endregion

        #region Public - Get
        public bool TryGet(string token, out AudioClip clip) {
            clip = null;
            if (!_TryBuildLoadKey(token, out string loadKey)) return false;
            return assetProvider.TryGet(loadKey, out clip) && clip;
        }

        public UniTask<AudioClip> GetOrLoadAsync(
            string token,
            AssetOwnerId ownerId = default,
            AssetFetchMode fetchMode = AssetFetchMode.CacheFirst) {

            if (!_TryBuildLoadKey(token, out string loadKey)) {
                return UniTask.FromResult<AudioClip>(null);
            }

            return assetProvider.GetAsync(loadKey, LoadMode, fetchMode, ownerId);
        }
        #endregion

        #region Public - Prewarm
        public async UniTask PrewarmTokenAsync(
            string token,
            AssetOwnerId ownerId = default,
            AssetFetchMode fetchMode = AssetFetchMode.CacheFirst) {

            await GetOrLoadAsync(token, ownerId, fetchMode);
        }

        public async UniTask PrewarmCatalogAsync(
            SoundCatalogSO catalog,
            AssetOwnerId ownerId = default,
            AssetFetchMode fetchMode = AssetFetchMode.CacheFirst) {

            Assert.IsNotNull(catalog, "[SoundClipRepository] catalog is null.");
            if (!catalog) return;

            catalogRegistry.RegisterCatalog(catalog);

            List<UniTask> tasks = new List<UniTask>(catalog.Entries.Count);
            foreach (var entry in catalog.Entries) {
                if (entry == null) continue;
                tasks.Add(GetOrLoadAsync(entry.Token, ownerId, fetchMode));
            }

            await UniTask.WhenAll(tasks);
        }
        #endregion

        #region Public - Release
        public bool Release(string token) {
            if (!_TryBuildLoadKey(token, out string loadKey)) return false;
            return assetProvider.Release(loadKey);
        }

        public bool Release(string token, AssetOwnerId ownerId) {
            if (!_TryBuildLoadKey(token, out string loadKey)) return false;
            return assetProvider.Release(loadKey, ownerId);
        }

        public void ReleaseCatalog(SoundCatalogSO catalog) {
            ReleaseCatalog(catalog, default);
        }

        public void ReleaseCatalog(SoundCatalogSO catalog, AssetOwnerId ownerId) {
            Assert.IsNotNull(catalog, "[SoundClipRepository] catalog is null.");
            if (!catalog) return;

            List<SoundCatalogSO.Entry> removedEntries = new List<SoundCatalogSO.Entry>();
            int refCount = catalogRegistry.ReleaseCatalog(catalog, removedEntries);
            if (refCount > 0) return;

            foreach (var entry in removedEntries) {
                string loadKey = _ResolveLoadKey(entry);
                if (string.IsNullOrWhiteSpace(loadKey)) continue;

                if (ownerId.IsValid) {
                    assetProvider.Release(loadKey, ownerId);
                    continue;
                }

                assetProvider.Release(loadKey);
            }
        }

        public int ReleaseOwner(AssetOwnerId ownerId) {
            return assetProvider.ReleaseOwner(ownerId);
        }

        public void ReleaseAll() {
            assetProvider.ReleaseAll();
        }
        #endregion

        #region Private - Resolve
        private bool _TryBuildLoadKey(string token, out string loadKey) {
            loadKey = string.Empty;

            string normalizedToken = _NormalizeToken(token);
            if (string.IsNullOrWhiteSpace(normalizedToken)) return false;
            if (catalogRegistry.TryGetEntry(normalizedToken, out SoundCatalogSO.Entry entry)) {
                loadKey = _ResolveLoadKey(entry);
                return !string.IsNullOrWhiteSpace(loadKey);
            }

            if (LoadMode != AssetLoadMode.Addressable) return false;
            loadKey = normalizedToken;
            return true;
        }

        private string _ResolveLoadKey(SoundCatalogSO.Entry entry) {
            if (entry == null) return string.Empty;

            return LoadMode switch {
                AssetLoadMode.Resources => SoundCatalogSO.BuildResourcesLoadKey(entry.Path, entry.Token),
                AssetLoadMode.Addressable => _ResolveAddressableLoadKey(entry),
                _ => string.Empty
            };
        }

        private string _ResolveAddressableLoadKey(SoundCatalogSO.Entry entry) {
            if (entry == null) return string.Empty;
            return _NormalizeToken(entry.Token);
        }

        private IAssetProvider<string, AudioClip> _CreateDefaultProvider(AssetLoadMode loadMode) {
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
 */
#endif

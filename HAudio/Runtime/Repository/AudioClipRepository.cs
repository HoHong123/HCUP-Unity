using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using HDiagnosis.Logger;
using HAudio.Catalog;
using HAudio.Core;
using HResource.Data;
using HResource.Provider;
using HResource.Subscription;

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
        readonly IAssetProvider<string, AudioClip> assetProvider;
        // uid → 최종 loadKey 해석 캐시. loadKey 생성은 문자열 할당을 동반하므로(Resources 모드의
        // BuildResourcesLoadKey) uid 당 1회만 수행하고 이후 재생은 조회만 한다.
        // AudioClip 자체는 캐시하지 않는다 — 자산 수명은 AssetProvider 한 곳이 소유해야 하고,
        // 여기에 두 번째 캐시를 두면 해제 경로가 둘로 갈라진다.
        readonly Dictionary<int, string> loadKeyByUid = new();
        // provider 를 주입받았다면 소유자는 호출자다 — 이 저장소가 폐기하면 안 된다.
        readonly bool ownsAssetProvider;
        bool disposed;
        #endregion

        #region Properties
        public AssetLoadMode LoadMode { get; }

        // disposed/ownsAssetProvider 가 private 이라 "폐기됐는가" · "provider 를 소유하는가"
        // 를 테스트가 단정할 수 없었다(케이스 리포트 07 TST-2). 진단 전용 읽기 프로퍼티만
        // 노출한다 — 상태를 바꾸지 않는다.
        public bool IsDisposed => disposed;
        public bool OwnsAssetProvider => ownsAssetProvider;
        #endregion

        #region Public - Constructors
        public AudioClipRepository(
            AssetLoadMode loadMode,
            AudioCatalogRegistry catalogRegistry,
            IAssetProvider<string, AudioClip> assetProvider = null) {

            if (catalogRegistry == null) {
                HLogger.Throw(new System.ArgumentNullException(nameof(catalogRegistry), "[AudioClipRepository] catalogRegistry is null."));
            }
            if (loadMode != AssetLoadMode.Resources && loadMode != AssetLoadMode.Addressable) {
                HLogger.Throw(new System.ArgumentException($"[AudioClipRepository] Unsupported load mode. loadMode={loadMode}", nameof(loadMode)));
            }

            LoadMode = loadMode;
            this.catalogRegistry = catalogRegistry;
            ownsAssetProvider = assetProvider == null;
            this.assetProvider = assetProvider ?? _CreateDefaultProvider(loadMode);
        }
        #endregion

        #region Public - Get
        /// <summary> uid 로 조회한다. 재생 경로의 기본 진입점 — 문자열 정규화·할당이 없다. </summary>
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
            AssetOwnerId ownerId = default,
            AssetFetchMode fetchMode = AssetFetchMode.CacheFirst) {

            if (!_TryBuildLoadKey(uid, out string loadKey)) {
                return UniTask.FromResult<AudioClip>(null);
            }

            return assetProvider.GetAsync(loadKey, LoadMode, fetchMode, ownerId);
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
            int uid,
            AssetOwnerId ownerId = default,
            AssetFetchMode fetchMode = AssetFetchMode.CacheFirst) {

            await GetOrLoadAsync(uid, ownerId, fetchMode);
        }

        public async UniTask PrewarmTokenAsync(
            string token,
            AssetOwnerId ownerId = default,
            AssetFetchMode fetchMode = AssetFetchMode.CacheFirst) {

            await GetOrLoadAsync(token, ownerId, fetchMode);
        }

        public async UniTask PrewarmCatalogAsync(
            AudioCatalogSO catalog,
            AssetOwnerId ownerId = default,
            AssetFetchMode fetchMode = AssetFetchMode.CacheFirst) {

            if (!catalog) return;   // null/파괴 가드 — Assert 는 릴리즈에서 제거되므로 런타임 가드만 유지

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
        public bool Release(int uid) {
            if (!_TryBuildLoadKey(uid, out string loadKey)) return false;
            return assetProvider.Release(loadKey);
        }

        public bool Release(int uid, AssetOwnerId ownerId) {
            if (!_TryBuildLoadKey(uid, out string loadKey)) return false;
            return assetProvider.Release(loadKey, ownerId);
        }

        public bool Release(string token) {
            if (!_TryBuildLoadKey(token, out string loadKey)) return false;
            return assetProvider.Release(loadKey);
        }

        public bool Release(string token, AssetOwnerId ownerId) {
            if (!_TryBuildLoadKey(token, out string loadKey)) return false;
            return assetProvider.Release(loadKey, ownerId);
        }

        public void ReleaseCatalog(AudioCatalogSO catalog) {
            ReleaseCatalog(catalog, default);
        }

        public void ReleaseCatalog(AudioCatalogSO catalog, AssetOwnerId ownerId) {
            if (!catalog) return;

            List<AudioCatalogSO.Entry> removedEntries = new List<AudioCatalogSO.Entry>();
            int refCount = catalogRegistry.ReleaseCatalog(catalog, removedEntries);
            if (refCount > 0) return;

            foreach (var entry in removedEntries) {
                // 해석 캐시를 함께 버린다. 같은 uid 가 경로가 다른 카탈로그로 재등록되면
                // 남아있던 loadKey 가 옛 경로를 가리켜 조용히 엉뚱한 자산을 찾게 된다.
                if (entry != null && entry.Uid != 0) loadKeyByUid.Remove(entry.Uid);

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
            loadKeyByUid.Clear();
            assetProvider.ReleaseAll();
        }

        /// <summary>
        /// 저장소 폐기. 기본 provider 를 이 저장소가 만들었을 때만 provider 까지 함께 폐기한다.
        /// 주입받은 provider 는 수명 소유자가 따로 있으므로 아무것도 하지 않는다.
        /// </summary>
        public void Dispose() {
            if (disposed) return;
            disposed = true;

            loadKeyByUid.Clear();

            // 주입받은 provider 에 ReleaseAll 을 걸면 owner 를 가리지 않고 캐시를 통째로 비우므로
            // 주입한 쪽의 다른 점유까지 파괴된다 (케이스 리포트 07 COR-1). 이 저장소는 자기 몫만
            // 식별할 수단(자체 ownerId)이 없으므로, 비소유 경로에서는 손대지 않는 것이 유일하게
            // 안전한 선택이다 — 해제 책임은 provider 를 주입한 쪽에 있다.
            if (!ownsAssetProvider) return;

            assetProvider.Dispose();
        }
        #endregion

        #region Private - Resolve
        private bool _TryBuildLoadKey(int uid, out string loadKey) {
            // 히트하면 여기서 끝난다 — int 해시 1회, 할당 0.
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

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
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
 * =============================================================================
 */
#endif

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using HUtil.AssetHandler.Cache;
using HUtil.AssetHandler.Load;
using HUtil.AssetHandler.Store;
using HUtil.AssetHandler.Validation;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 기본 AssetProvider 조합을 만드는 팩토리 스크립트입니다.
 *
 * 주의사항 ::
 * 1. 편의 생성 계층일 뿐 실제 로딩 정책을 직접 해석하지 않습니다.
 * 2. 특수 조합이 필요하면 직접 Create 오버로드를 사용해야 합니다.
 * =========================================================
 */
#endif

namespace HUtil.AssetHandler.Provider {
    public static class AssetProviderFactory {
        #region Public - Create
        public static AssetProvider<string, TAsset> CreateResources<TAsset>(
            string resourcesRootPath,
            IAssetStore<string, TAsset> assetStore = null)
            where TAsset : Object {

            var assetLoader = new ResourcesAssetLoader<TAsset>(resourcesRootPath);
            return Create(new[] { assetLoader }, assetStore);
        }

        public static AssetProvider<string, TAsset> CreateAddressable<TAsset>(
            IAssetStore<string, TAsset> assetStore = null)
            where TAsset : Object {

            var assetLoader = new AddressableAssetLoader<TAsset>();
            return Create(new[] { assetLoader }, assetStore);
        }

        public static AssetProvider<string, TAsset> Create<TAsset>(
            IEnumerable<IAssetLoader<string, TAsset>> assetLoaders,
            IAssetStore<string, TAsset> assetStore = null)
            where TAsset : Object {

            Assert.IsNotNull(assetLoaders, "[AssetProviderFactory] assetLoaders is null.");

            return new AssetProvider<string, TAsset>(
                assetLoaders: assetLoaders,
                assetCache: new MemoryAssetCache<string, TAsset>(),
                assetValidator: new DefaultAssetValidator<string, TAsset>(),
                assetLoadGate: new SharedAssetLoadGate<string, TAsset>(),
                assetStore: assetStore);
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 주요 기능 ::
 * 1. Resources 기본 조합을 생성합니다.
 * 2. Addressable 기본 조합을 생성합니다.
 * 3. cache, validator, gate, loader를 기본 묶음으로 제공합니다.
 *
 * 사용법 ::
 * 1. 도메인 코드가 빠르게 provider를 조립할 때 사용합니다.
 * 2. 기본 store를 추가하고 싶으면 인자로 전달합니다.
 *
 * 이벤트 ::
 * 1. 직접 이벤트는 없습니다.
 * 2. 생성된 provider가 이후 런타임 이벤트 흐름을 담당합니다.
 *
 * 기타 ::
 * 1. 실행 객체가 아니라 조립 헬퍼입니다.
 * 2. 프로젝트 기본 구성을 한곳에 모으기 위한 도구입니다.
 * =========================================================
 */
#endif

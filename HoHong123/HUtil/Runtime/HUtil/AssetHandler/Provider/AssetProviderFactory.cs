using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using HUtil.AssetHandler.Cache;
using HUtil.AssetHandler.Load;
using HUtil.AssetHandler.Store;
using HUtil.AssetHandler.Validation;

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

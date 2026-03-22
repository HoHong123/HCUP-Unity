using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using HUtil.Data.Load;
using HUtil.Data.Sequence;

namespace HUtil.Data.Adapter {
    public abstract class AddressableAdaptedLoadSequence<TAsset, TResult> :
        BaseLoadSequence<TResult>,
        IAssetAdapter<TAsset, TResult>
        where TAsset : Object
        where TResult : class {
        #region Fields
        readonly AddressableLoadSequence<TAsset> assetLoader;
        #endregion

        #region Protected - Constructors
        protected AddressableAdaptedLoadSequence()
            : base(DataLoadType.Addressable) {
            assetLoader = new AddressableLoadSequence<TAsset>();
            Assert.IsNotNull(assetLoader);
        }
        #endregion

        #region Protected - Normalize Key
        protected override string _NormalizeKey(string tokenOrPath) => tokenOrPath;
        #endregion

        #region Protected - Load By Key
        protected override async UniTask<TResult> _LoadByKeyAsync(string key) {
#if UNITY_EDITOR
            Logger.HLogger.Log($"Load Adapted Addressable :: {key}");
#endif
            var asset = await assetLoader.LoadAsync(key);
            return asset != null ? Convert(asset) : null;
        }
        #endregion

        #region Public - Release
        public void Release(string key) {
            assetLoader.Release(key);
        }

        public void ReleaseAll() {
            assetLoader.ReleaseAll();
        }
        #endregion

        #region Public Abstract - Convert
        public abstract TResult Convert(TAsset asset);
        #endregion
    }
}
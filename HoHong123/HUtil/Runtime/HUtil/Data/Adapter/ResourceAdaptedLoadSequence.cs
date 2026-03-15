#if UNITY_EDITOR
/* =========================================================
 * Resources 기반 Asset을 로드하고 Runtime 데이터로 변환하는 LoadSequence 베이스 클래스입니다.
 *
 * 구조 ::
 * Resource Load → Asset Adapter → Runtime Data
 *
 * 주의사항 ::
 * 1. Convert 메서드는 반드시 구현되어야 합니다.
 * 2. Asset이 null일 경우 결과 데이터는 null 반환됩니다.
 * =========================================================
 */
#endif

using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using HUtil.Data.Load;
using HUtil.Data.Sequence;

namespace HUtil.Data.Adapter {
    public abstract class ResourceAdaptedLoadSequence<TAsset, TResult> :
        BaseLoadSequence<TResult>,
        IAssetAdapter<TAsset, TResult>
        where TAsset : Object
        where TResult : class {
        #region Fields
        readonly ResourcesLoadSequence<TAsset> assetLoader;
        #endregion

        #region Protected - Constructors
        protected ResourceAdaptedLoadSequence(string resourcesRootPath)
            : base(DataLoadType.Resources) {

            Assert.IsFalse(string.IsNullOrWhiteSpace(resourcesRootPath));
            assetLoader = new ResourcesLoadSequence<TAsset>(resourcesRootPath);
        }
        #endregion

        #region Protected - Normalize Key
        protected override string _NormalizeKey(string tokenOrPath) => tokenOrPath;
        #endregion

        #region Protected - Load By Key
        protected override async UniTask<TResult> _LoadByKeyAsync(string key) {
#if UNITY_EDITOR
            Logger.HLogger.Log($"Load Adapted Resource :: {key}");
#endif
            var asset = await assetLoader.LoadAsync(key);
            return asset != null ? Convert(asset) : null;
        }
        #endregion

        #region Public Abstract - Onvert
        public abstract TResult Convert(TAsset asset);
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 *
 * 주요 기능 ::
 * 1. Resource Asset 로드
 * 2. Asset → Runtime Data 변환
 *
 * 사용법 ::
 * 1. ResourceAdaptedLoadSequence<TAsset,TResult>를 상속합니다.
 * 2. Convert(TAsset asset) 메서드를 구현합니다.
 *
 * 기타 ::
 * 1. ResourcesLoadSequence를 내부적으로 사용합니다.
 * =========================================================
 */
#endif
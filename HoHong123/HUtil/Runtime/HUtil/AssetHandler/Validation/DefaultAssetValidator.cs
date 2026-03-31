using UnityEngine;

namespace HUtil.AssetHandler.Validation {
    public sealed class DefaultAssetValidator<TKey, TAsset> : IAssetValidator<TKey, TAsset> {
        #region Public - Validate
        public bool CanLoad(TKey key) {
            if (key is string stringKey) {
                return !string.IsNullOrWhiteSpace(stringKey);
            }

            if (ReferenceEquals(key, null)) {
                return false;
            }

            return true;
        }

        public bool IsValid(TKey key, TAsset asset) {
            if (!CanLoad(key)) return false;

            if (asset is Object unityObject) {
                return unityObject != null;
            }

            if (ReferenceEquals(asset, null)) {
                return false;
            }

            return true;
        }
        #endregion
    }
}

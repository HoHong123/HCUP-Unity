using System.Collections.Generic;

namespace HUtil.AssetHandler.Cache {
    public sealed class MemoryAssetCache<TKey, TAsset> : IAssetCache<TKey, TAsset> {
        #region Fields
        readonly Dictionary<TKey, TAsset> table = new();
        #endregion

        #region Public - Get
        public bool TryGet(TKey key, out TAsset asset) {
            if (!table.TryGetValue(key, out asset)) return false;

            if (ReferenceEquals(asset, null)) {
                asset = default;
                return false;
            }

            return true;
        }
        #endregion

        #region Public - Save
        public bool Save(TKey key, TAsset asset) {
            if (ReferenceEquals(asset, null)) return false;

            table[key] = asset;
            return true;
        }
        #endregion

        #region Public - Release
        public bool Release(TKey key) {
            return table.Remove(key);
        }

        public void ReleaseAll() {
            table.Clear();
        }

        public void Clear() {
            table.Clear();
        }
        #endregion
    }
}

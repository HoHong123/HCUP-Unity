using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using Object = UnityEngine.Object;

namespace HUtil.AssetHandler.Load {
    public sealed class AddressableLabelLoader<TAsset> : IAddressableLabelLoader<TAsset>
        where TAsset : Object {
        #region Nested Types
        internal enum AddressableLabelLoadMode : byte {
            All = 0,
            First = 1,
            Single = 2,
            Index = 3,
        }

        readonly struct LabelHandleKey : IEquatable<LabelHandleKey> {
            public string Label { get; }
            public AddressableLabelLoadMode LoadMode { get; }
            public int Index { get; }

            public LabelHandleKey(string label, AddressableLabelLoadMode loadMode, int index = -1) {
                Label = label;
                LoadMode = loadMode;
                Index = index;
            }

            public bool Equals(LabelHandleKey other) {
                return string.Equals(Label, other.Label, StringComparison.Ordinal)
                    && LoadMode == other.LoadMode
                    && Index == other.Index;
            }

            public override bool Equals(object obj) {
                return obj is LabelHandleKey other && Equals(other);
            }

            public override int GetHashCode() {
                return HashCode.Combine(Label, (int)LoadMode, Index);
            }
        }
        #endregion

        #region Fields
        readonly Dictionary<LabelHandleKey, AsyncOperationHandle<TAsset>> singleHandleTable = new();
        readonly Dictionary<LabelHandleKey, AsyncOperationHandle<IList<TAsset>>> multiHandleTable = new();
        #endregion

        #region Public - Load
        public async UniTask<IList<TAsset>> LoadAllAsync(string label) {
            label = _NormalizeLabel(label);
            if (string.IsNullOrWhiteSpace(label)) return null;

            var handleKey = _CreateHandleKey(label, AddressableLabelLoadMode.All);
            if (multiHandleTable.TryGetValue(handleKey, out var cachedHandle)) {
                if (cachedHandle.IsValid()) return cachedHandle.Result;
                multiHandleTable.Remove(handleKey);
            }

            var handle = Addressables.LoadAssetsAsync<TAsset>(label, null);
            await handle.ToUniTask();

            if (handle.Status != AsyncOperationStatus.Succeeded) {
                if (handle.IsValid()) Addressables.Release(handle);
                return null;
            }

            multiHandleTable[handleKey] = handle;
            return handle.Result;
        }

        public UniTask<TAsset> LoadFirstAsync(string label) {
            return _LoadSingleAsync(
                label,
                normalizedLabel => _CreateHandleKey(normalizedLabel, AddressableLabelLoadMode.First),
                _ResolveFirstLocation);
        }

        public UniTask<TAsset> LoadSingleAsync(string label) {
            return _LoadSingleAsync(
                label,
                normalizedLabel => _CreateHandleKey(normalizedLabel, AddressableLabelLoadMode.Single),
                _ResolveSingleLocation);
        }

        public UniTask<TAsset> LoadByIndexAsync(string label, int index) {
            return _LoadSingleAsync(
                label,
                normalizedLabel => _CreateHandleKey(normalizedLabel, AddressableLabelLoadMode.Index, index),
                locations => _ResolveIndexLocation(locations, index));
        }
        #endregion

        #region Public - Release
        public bool ReleaseAllByLabel(string label) {
            return _ReleaseMultiHandle(_CreateHandleKey(_NormalizeLabel(label), AddressableLabelLoadMode.All));
        }

        public bool ReleaseFirstByLabel(string label) {
            return _ReleaseSingleHandle(_CreateHandleKey(_NormalizeLabel(label), AddressableLabelLoadMode.First));
        }

        public bool ReleaseSingleByLabel(string label) {
            return _ReleaseSingleHandle(_CreateHandleKey(_NormalizeLabel(label), AddressableLabelLoadMode.Single));
        }

        public bool ReleaseByLabelIndex(string label, int index) {
            return _ReleaseSingleHandle(_CreateHandleKey(_NormalizeLabel(label), AddressableLabelLoadMode.Index, index));
        }

        public void ReleaseAll() {
            foreach (var handle in singleHandleTable.Values) {
                if (handle.IsValid()) Addressables.Release(handle);
            }

            foreach (var handle in multiHandleTable.Values) {
                if (handle.IsValid()) Addressables.Release(handle);
            }

            singleHandleTable.Clear();
            multiHandleTable.Clear();
        }
        #endregion

        #region Private - Load
        private async UniTask<TAsset> _LoadSingleAsync(
            string label,
            Func<string, LabelHandleKey> createHandleKey,
            Func<IList<IResourceLocation>, IResourceLocation> resolveLocation) {

            label = _NormalizeLabel(label);
            if (string.IsNullOrWhiteSpace(label)) return null;

            var handleKey = createHandleKey(label);
            if (singleHandleTable.TryGetValue(handleKey, out var cachedHandle)) {
                if (cachedHandle.IsValid()) return cachedHandle.Result;
                singleHandleTable.Remove(handleKey);
            }

            var locationHandle = Addressables.LoadResourceLocationsAsync(label, typeof(TAsset));
            await locationHandle.ToUniTask();

            try {
                if (locationHandle.Status != AsyncOperationStatus.Succeeded) return null;

                var location = resolveLocation(locationHandle.Result);
                if (location == null) return null;

                var assetHandle = Addressables.LoadAssetAsync<TAsset>(location);
                await assetHandle.ToUniTask();

                if (assetHandle.Status != AsyncOperationStatus.Succeeded) {
                    if (assetHandle.IsValid()) Addressables.Release(assetHandle);
                    return null;
                }

                singleHandleTable[handleKey] = assetHandle;
                return assetHandle.Result;
            }
            finally {
                if (locationHandle.IsValid()) {
                    Addressables.Release(locationHandle);
                }
            }
        }
        #endregion

        #region Private - Resolve
        private IResourceLocation _ResolveFirstLocation(IList<IResourceLocation> locations) {
            if (locations == null || locations.Count < 1) return null;
            return locations[0];
        }

        private IResourceLocation _ResolveSingleLocation(IList<IResourceLocation> locations) {
            if (locations == null || locations.Count != 1) return null;
            return locations[0];
        }

        private IResourceLocation _ResolveIndexLocation(IList<IResourceLocation> locations, int index) {
            if (locations == null) return null;
            if ((uint)index >= (uint)locations.Count) return null;
            return locations[index];
        }
        #endregion

        #region Private - Release
        private bool _ReleaseSingleHandle(LabelHandleKey handleKey) {
            if (string.IsNullOrWhiteSpace(handleKey.Label)) return false;
            if (!singleHandleTable.TryGetValue(handleKey, out var handle)) return false;
            if (handle.IsValid()) Addressables.Release(handle);
            singleHandleTable.Remove(handleKey);
            return true;
        }

        private bool _ReleaseMultiHandle(LabelHandleKey handleKey) {
            if (string.IsNullOrWhiteSpace(handleKey.Label)) return false;
            if (!multiHandleTable.TryGetValue(handleKey, out var handle)) return false;
            if (handle.IsValid()) Addressables.Release(handle);
            multiHandleTable.Remove(handleKey);
            return true;
        }
        #endregion

        #region Private - Key
        private LabelHandleKey _CreateHandleKey(string label, AddressableLabelLoadMode loadMode, int index = -1) {
            if (string.IsNullOrWhiteSpace(label)) return default;
            return new LabelHandleKey(label, loadMode, index);
        }

        private string _NormalizeLabel(string label) {
            if (string.IsNullOrWhiteSpace(label)) return string.Empty;
            return label.Trim();
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * Addressables 기반 데이터 로드를 위한 LoadSequence 클래스입니다.
 * Address / Label token 기반 데이터 로드를 지원합니다.
 *
 * Token 규칙 ::
 * 1. "Some/Address"           = Address 로드
 * 2. "address:Some/Address"   = Address 로드
 * 3. "label:SomeLabel"        = Label 첫 번째 Asset 로드
 * 4. "label!:SomeLabel"       = Label 결과가 정확히 1개일 때만 로드
 * 5. "label[2]:SomeLabel"     = Label 결과 중 index 2 Asset 로드
 * =========================================================
 */
#endif

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using HUtil.Data.Load;

namespace HUtil.Data.Sequence {
    public class AddressableLoadSequence<TData> :
        BaseLoadSequence<TData>,
        IReleasableDataLoad<string, TData>
        where TData : UnityEngine.Object {
        #region Private - Types
        readonly struct AddressableKey {
            public readonly string RawKey;
            public readonly string LookupKey;
            public readonly AddressableLoadType Load;
            public readonly int Index;

            public AddressableKey(
                string rawKey,
                string lookupKey,
                AddressableLoadType load,
                int index = -1) {
                RawKey = rawKey;
                LookupKey = lookupKey;
                Load = load;
                Index = index;
            }
        }
        #endregion

        #region Fields
        readonly Dictionary<string, AsyncOperationHandle<TData>> handles = new();
        #endregion

        #region Public - Constructors
        public AddressableLoadSequence() : base(DataLoadType.Addressable) { }
        #endregion

        #region Public - Release
        public void Release(string key) {
            key = _NormalizeKey(key);
            if (string.IsNullOrWhiteSpace(key)) return;
            if (!handles.TryGetValue(key, out var handle)) return;

            if (handle.IsValid()) Addressables.Release(handle);
            handles.Remove(key);
        }

        public void ReleaseAll() {
            foreach (var pair in handles) {
                if (pair.Value.IsValid()) Addressables.Release(pair.Value);
            }
            handles.Clear();
        }
        #endregion

        #region Protected - Load
        protected override async UniTask<TData> _LoadByKeyAsync(string key) {
            if (handles.TryGetValue(key, out var cachedHandle)) {
                if (cachedHandle.IsValid()) return cachedHandle.Result;
                handles.Remove(key);
            }

            var addressableKey = _ParseKey(key);
            var asset = await _LoadByModeAsync(addressableKey);
            return asset;
        }
        #endregion

        #region Protected - Normalize Key
        protected override string _NormalizeKey(string tokenOrPath) {
            if (string.IsNullOrWhiteSpace(tokenOrPath)) return string.Empty;
            return tokenOrPath.Trim();
        }
        #endregion

        #region Private - Load By Mode
        private async UniTask<TData> _LoadByModeAsync(AddressableKey key) {
            return key.Load switch {
                AddressableLoadType.Address => await _LoadAddressAsync(key),
                AddressableLoadType.LabelFirst => await _LoadLabelAsync(key),
                AddressableLoadType.LabelSingle => await _LoadLabelAsync(key),
                AddressableLoadType.LabelIndex => await _LoadLabelAsync(key),
                _ => null
            };
        }

        private async UniTask<TData> _LoadAddressAsync(AddressableKey key) {
            var handle = Addressables.LoadAssetAsync<TData>(key.LookupKey);
            await handle.ToUniTask();

            if (handle.Status != AsyncOperationStatus.Succeeded) {
                if (handle.IsValid()) Addressables.Release(handle);
                return null;
            }

            handles[key.RawKey] = handle;
            return handle.Result;
        }

        private async UniTask<TData> _LoadLabelAsync(AddressableKey key) {
            var locationHandle = Addressables.LoadResourceLocationsAsync(key.LookupKey, typeof(TData));
            await locationHandle.ToUniTask();

            try {
                var location = _ResolveLocation(key, locationHandle.Result);
                if (location == null) return null;

                var assetHandle = Addressables.LoadAssetAsync<TData>(location);
                await assetHandle.ToUniTask();

                if (assetHandle.Status != AsyncOperationStatus.Succeeded) {
                    if (assetHandle.IsValid()) Addressables.Release(assetHandle);
                    return null;
                }

                handles[key.RawKey] = assetHandle;
                return assetHandle.Result;
            }
            finally {
                if (locationHandle.IsValid()) Addressables.Release(locationHandle);
            }
        }
        #endregion

        #region Private - Key Parsing
        private AddressableKey _ParseKey(string rawKey) {
            if (rawKey.StartsWith("address:", StringComparison.OrdinalIgnoreCase))
                return new AddressableKey(rawKey, rawKey[8..].Trim(), AddressableLoadType.Address);
            if (rawKey.StartsWith("label!:", StringComparison.OrdinalIgnoreCase))
                return new AddressableKey(rawKey, rawKey[7..].Trim(), AddressableLoadType.LabelSingle);
            if (_TryParseLabelIndex(rawKey, out var indexedKey))
                return indexedKey;
            if (rawKey.StartsWith("label:", StringComparison.OrdinalIgnoreCase))
                return new AddressableKey(rawKey, rawKey[6..].Trim(), AddressableLoadType.LabelFirst);
            return new AddressableKey(rawKey, rawKey, AddressableLoadType.Address);
        }

        private bool _TryParseLabelIndex(string rawKey, out AddressableKey key) {
            key = default;
            if (!rawKey.StartsWith("label[", StringComparison.OrdinalIgnoreCase))
                return false;

            int bracketCloseIndex = rawKey.IndexOf(']');
            if (bracketCloseIndex < 6)
                return false;

            if (bracketCloseIndex + 1 >= rawKey.Length || rawKey[bracketCloseIndex + 1] != ':')
                return false;

            string indexText = rawKey.Substring(6, bracketCloseIndex - 6);
            if (!int.TryParse(indexText, out var index))
                return false;

            if (index < 0) return false;

            string label = rawKey[(bracketCloseIndex + 2)..].Trim();
            key = new AddressableKey(rawKey, label, AddressableLoadType.LabelIndex, index);
            return !string.IsNullOrWhiteSpace(label);
        }
        #endregion

        #region Private - Location Resolve
        private IResourceLocation _ResolveLocation(AddressableKey key, IList<IResourceLocation> locations) {
            if (locations == null || locations.Count < 1)
                return null;

            switch (key.Load) {
            case AddressableLoadType.LabelFirst:
                return locations[0];
            case AddressableLoadType.LabelSingle:
                return locations.Count == 1 ? locations[0] : null;
            case AddressableLoadType.LabelIndex:
                return (uint)key.Index < (uint)locations.Count ? locations[key.Index] : null;
            default:
                return null;
            }
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 *
 * 주요 기능 ::
 * 1. Address token 기반 로드
 * 2. Label token 기반 로드
 * 3. 명시적 Release 지원
 *
 * 사용법 ::
 * 1. Address 또는 Label token 문자열을 전달합니다.
 * 2. Cache 제거 시 Release가 함께 호출되도록 Provider와 연결합니다.
 *
 * 기타 ::
 * 1. Label은 단일 / 첫 번째 / index 지정 로드를 지원합니다.
 * =========================================================
 */
#endif
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using HUtil.AssetHandler.Data;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Addressable 단일 asset 로더 구현 스크립트입니다.
 *
 * 주의사항 ::
 * 1. 성공한 handle은 반드시 release 경로와 짝을 맞춰야 합니다.
 * 2. key 정규화 규칙이 addressable 주소 규칙과 맞아야 합니다.
 * =========================================================
 */
#endif

namespace HUtil.AssetHandler.Load {
    public sealed class AddressableAssetLoader<TAsset> : IAssetReleasableLoader<string, TAsset>
        where TAsset : Object {
        #region Fields
        readonly Dictionary<string, AsyncOperationHandle<TAsset>> handleTable = new();
        #endregion

        #region Properties
        public AssetLoadMode LoadMode => AssetLoadMode.Addressable;
        #endregion

        #region Public - Load
        public async UniTask<TAsset> LoadAsync(string key) {
            var normalizedKey = _NormalizeKey(key);
            if (string.IsNullOrWhiteSpace(normalizedKey)) return null;

            if (handleTable.TryGetValue(normalizedKey, out var cachedHandle)) {
                if (cachedHandle.IsValid()) return cachedHandle.Result;
                handleTable.Remove(normalizedKey);
            }

            var handle = Addressables.LoadAssetAsync<TAsset>(normalizedKey);
            await handle.ToUniTask();

            if (handle.Status != AsyncOperationStatus.Succeeded) {
                if (handle.IsValid()) Addressables.Release(handle);
                return null;
            }

            handleTable[normalizedKey] = handle;
            return handle.Result;
        }
        #endregion

        #region Public - Release
        public bool Release(string key) {
            var normalizedKey = _NormalizeKey(key);
            if (string.IsNullOrWhiteSpace(normalizedKey)) {
                return false;
            }

            if (!handleTable.TryGetValue(normalizedKey, out var handle)) {
                return false;
            }

            if (handle.IsValid()) {
                Addressables.Release(handle);
            }

            handleTable.Remove(normalizedKey);
            return true;
        }

        public void ReleaseAll() {
            foreach (var handle in handleTable.Values) {
                if (handle.IsValid()) Addressables.Release(handle);
            }

            handleTable.Clear();
        }
        #endregion

        #region Private - Normalize
        private string _NormalizeKey(string key) {
            if (string.IsNullOrWhiteSpace(key)) return string.Empty;
            return key.Trim();
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 주요 기능 ::
 * 1. 단일 key 기반 Addressable 로드를 수행합니다.
 * 2. 성공한 handle을 보관합니다.
 * 3. source release를 직접 처리합니다.
 *
 * 사용법 ::
 * 1. AssetProvider의 Addressable loader로 등록해 사용합니다.
 * 2. Addressable source release가 필요할 때 IAssetReleasableLoader 경로를 함께 사용합니다.
 *
 * 이벤트 ::
 * 1. 직접 이벤트는 없습니다.
 * 2. provider release 흐름과 연결되어 handle release가 일어납니다.
 *
 * 기타 ::
 * 1. Addressable label 로드는 별도 loader로 분리되어 있습니다.
 * 2. source 책임만 맡고 cache 정책은 provider가 담당합니다.
 * =========================================================
 */
#endif

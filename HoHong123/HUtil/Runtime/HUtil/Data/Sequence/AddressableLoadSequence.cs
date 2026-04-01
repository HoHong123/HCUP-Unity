#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 *
 * 주요 기능 ::
 * 1. Address 기반 단일 Asset 비동기 로드
 * 2. Label 기반 전체 Asset 비동기 로드
 * 3. Label 기반 단일 Asset 비동기 로드
 *     3-1. 첫 번째 결과 로드
 *     3-2. 단일 결과만 허용 로드
 *     3-3. 특정 index 결과 로드
 * 4. 로드 방식별 개별 Release 지원
 * 5. 전체 handle 일괄 ReleaseAll 지원
 *
 * 내부 관리 ::
 * 1. Address / LabelAll / LabelFirst / LabelSingle / LabelIndex는 서로 다른 내부 handle key를 사용합니다.
 * 2. 같은 key라도 로드 방식이 다르면 별도 handle로 관리됩니다.
 * 3. Label 전체 로드는 LoadAssetsAsync handle로 관리됩니다.
 * 4. Label 선택 로드에 사용된 locationHandle은 로드 직후 내부에서 즉시 해제됩니다.
 * 5. 실제 Asset handle은 Release 계열 함수 또는 ReleaseAll로 해제됩니다.
 *
 * 주의사항 ::
 * 1. Address key와 Label name은 자동 판별되지 않습니다.
 * 2. 어떤 방식으로 로드했는지 호출부가 명확히 선택해야 합니다.
 * 3. Label 전체 로드와 Label 단건 선택 로드는 서로 다른 개념입니다.
 * 4. Release도 반드시 같은 방식의 함수를 사용해야 합니다.
 * =========================================================
 */
#endif

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using HUtil.Data.Load;

namespace HUtil.Data.Sequence {
    public class AddressableLoadSequence<TData> :
        BaseLoadSequence<TData>,
        IReleasableDataLoad<string, TData>
        where TData : UnityEngine.Object {
        #region Fields
        readonly Dictionary<string, AsyncOperationHandle<TData>> singleHandles = new();
        readonly Dictionary<string, AsyncOperationHandle<IList<TData>>> multiHandles = new();
        #endregion

        #region Public - Constructors
        public AddressableLoadSequence() : base(DataLoadType.Addressable) { }
        #endregion

        #region Public - Address Load
        public override UniTask<TData> LoadAsync(string tokenOrPath) {
            if (string.IsNullOrWhiteSpace(tokenOrPath))
                return UniTask.FromResult<TData>(null);

            string key = _NormalizeKey(tokenOrPath);
            if (string.IsNullOrWhiteSpace(key))
                return UniTask.FromResult<TData>(null);

            return _LoadAddressByKeyAsync(key);
        }
        #endregion

        #region Public - Label Multi Load
        public UniTask<IList<TData>> LoadAllByLabelAsync(string label) {
            return _LoadAllByLabelAsync(label);
        }
        #endregion

        #region Public - Label Single Load
        public UniTask<TData> LoadFirstByLabelAsync(string label) {
            return _LoadLabelByKeyAsync(label, AddressableLoadType.LabelFirst);
        }

        public UniTask<TData> LoadSingleByLabelAsync(string label) {
            return _LoadLabelByKeyAsync(label, AddressableLoadType.LabelSingle);
        }

        public UniTask<TData> LoadByLabelIndexAsync(string label, int index) {
            return _LoadLabelByKeyAsync(label, AddressableLoadType.LabelIndex, index);
        }
        #endregion

        #region Public - Address Release
        public void Release(string key) {
            key = _NormalizeKey(key);
            if (string.IsNullOrWhiteSpace(key))
                return;
            string handleKey = _BuildAddressHandleKey(key);
            _ReleaseSingleInternal(handleKey);
        }
        #endregion

        #region Public - Label Multi Release
        public void ReleaseAllByLabel(string label) {
            label = _NormalizeKey(label);
            if (string.IsNullOrWhiteSpace(label))
                return;
            string handleKey = _BuildLabelAllHandleKey(label);
            _ReleaseMultiInternal(handleKey);
        }
        #endregion

        #region Public - Label Single Release
        public void ReleaseLabel(string label) {
            label = _NormalizeKey(label);
            if (string.IsNullOrWhiteSpace(label))
                return;
            string handleKey = _BuildLabelFirstHandleKey(label);
            _ReleaseSingleInternal(handleKey);
        }

        public void ReleaseSingleLabel(string label) {
            label = _NormalizeKey(label);
            if (string.IsNullOrWhiteSpace(label))
                return;
            string handleKey = _BuildLabelSingleHandleKey(label);
            _ReleaseSingleInternal(handleKey);
        }

        public void ReleaseLabel(string label, int index) {
            label = _NormalizeKey(label);
            if (string.IsNullOrWhiteSpace(label))
                return;
            string handleKey = _BuildLabelIndexHandleKey(label, index);
            _ReleaseSingleInternal(handleKey);
        }

        public void ReleaseAll() {
            foreach (var pair in singleHandles) {
                if (pair.Value.IsValid())
                    Addressables.Release(pair.Value);
            }

            foreach (var pair in multiHandles) {
                if (pair.Value.IsValid())
                    Addressables.Release(pair.Value);
            }

            singleHandles.Clear();
            multiHandles.Clear();
        }
        #endregion

        #region Protected - Normalize Key
        private string _BuildLabelAllHandleKey(string label) => $"{AddressableLoadType.LabelAll}:{label}";
        private string _BuildLabelFirstHandleKey(string label) => $"{AddressableLoadType.LabelFirst}:{label}";
        private string _BuildLabelSingleHandleKey(string label) => $"{AddressableLoadType.LabelSingle}:{label}";
        private string _BuildLabelIndexHandleKey(string label, int index) => $"{AddressableLoadType.LabelIndex}:{label}:{index}";
        private string _BuildAddressHandleKey(string key) => $"{AddressableLoadType.Address}:{key}";

        protected override string _NormalizeKey(string tokenOrPath) {
            if (string.IsNullOrWhiteSpace(tokenOrPath))
                return string.Empty;
            return tokenOrPath.Trim();
        }

        protected override UniTask<TData> _LoadByKeyAsync(string key) {
            return _LoadAddressByKeyAsync(key);
        }
        #endregion

        #region Private - Address Load
        private async UniTask<TData> _LoadAddressByKeyAsync(string key) {
            string handleKey = _BuildAddressHandleKey(key);

            if (singleHandles.TryGetValue(handleKey, out var cachedHandle)) {
                if (cachedHandle.IsValid())
                    return cachedHandle.Result;
                singleHandles.Remove(handleKey);
            }

            var handle = Addressables.LoadAssetAsync<TData>(key);
            await handle.ToUniTask();

            if (handle.Status != AsyncOperationStatus.Succeeded) {
                if (handle.IsValid())
                    Addressables.Release(handle);
                return null;
            }

            singleHandles[handleKey] = handle;
            return handle.Result;
        }
        #endregion
        #region Private - Label Multi Load
        private async UniTask<IList<TData>> _LoadAllByLabelAsync(string label) {
            label = _NormalizeKey(label);
            if (string.IsNullOrWhiteSpace(label))
                return null;

            string handleKey = _BuildLabelAllHandleKey(label);

            if (multiHandles.TryGetValue(handleKey, out var cachedHandle)) {
                if (cachedHandle.IsValid())
                    return cachedHandle.Result;

                multiHandles.Remove(handleKey);
            }

            var handle = Addressables.LoadAssetsAsync<TData>(label, null);
            await handle.ToUniTask();

            if (handle.Status != AsyncOperationStatus.Succeeded) {
                if (handle.IsValid())
                    Addressables.Release(handle);

                return null;
            }

            multiHandles[handleKey] = handle;
            return handle.Result;
        }
        #endregion

        #region Private - Label Single Load
        private async UniTask<TData> _LoadLabelByKeyAsync(string label, AddressableLoadType mode, int index = -1) {
            label = _NormalizeKey(label);
            if (string.IsNullOrWhiteSpace(label))
                return null;

            string handleKey = mode switch {
                AddressableLoadType.LabelFirst => _BuildLabelFirstHandleKey(label),
                AddressableLoadType.LabelSingle => _BuildLabelSingleHandleKey(label),
                AddressableLoadType.LabelIndex => _BuildLabelIndexHandleKey(label, index),
                _ => _BuildLabelFirstHandleKey(label)
            };

            if (singleHandles.TryGetValue(handleKey, out var cachedHandle)) {
                if (cachedHandle.IsValid())
                    return cachedHandle.Result;
                singleHandles.Remove(handleKey);
            }

            var locationHandle = Addressables.LoadResourceLocationsAsync(label, typeof(TData));
            await locationHandle.ToUniTask();

            try {
                var location = _ResolveLocation(locationHandle.Result, mode, index);
                if (location == null)
                    return null;

                var assetHandle = Addressables.LoadAssetAsync<TData>(location);
                await assetHandle.ToUniTask();

                if (assetHandle.Status != AsyncOperationStatus.Succeeded) {
                    if (assetHandle.IsValid())
                        Addressables.Release(assetHandle);
                    return null;
                }

                singleHandles[handleKey] = assetHandle;
                return assetHandle.Result;
            }
            finally {
                if (locationHandle.IsValid())
                    Addressables.Release(locationHandle);
            }
        }

        private IResourceLocation _ResolveLocation(IList<IResourceLocation> locations, AddressableLoadType mode, int index) {
            if (locations == null || locations.Count < 1)
                return null;

            return mode switch {
                AddressableLoadType.LabelFirst => locations[0],
                AddressableLoadType.LabelSingle => locations.Count == 1 ? locations[0] : null,
                AddressableLoadType.LabelIndex => (uint)index < (uint)locations.Count ? locations[index] : null,
                _ => null
            };
        }
        #endregion

        #region Private - Release
        private void _ReleaseSingleInternal(string handleKey) {
            if (string.IsNullOrWhiteSpace(handleKey))
                return;
            if (!singleHandles.TryGetValue(handleKey, out var handle))
                return;
            if (handle.IsValid())
                Addressables.Release(handle);
            singleHandles.Remove(handleKey);
        }

        private void _ReleaseMultiInternal(string handleKey) {
            if (string.IsNullOrWhiteSpace(handleKey))
                return;
            if (!multiHandles.TryGetValue(handleKey, out var handle))
                return;
            if (handle.IsValid())
                Addressables.Release(handle);
            multiHandles.Remove(handleKey);
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 *
 * 주요 기능 ::
 * 1. Address 기반 단일 Asset 비동기 로드
 * 2. Label 기반 전체 Asset 비동기 로드
 * 3. Label 기반 단일 Asset 비동기 로드
 * 3-1. 첫 번째 결과 로드
 * 3-2. 단일 결과만 허용 로드
 * 3-3. 특정 index 결과 로드
 * 4. 로드 방식별 개별 Release 지원
 * 5. 전체 handle 일괄 ReleaseAll 지원
 *
 * 사용법 ::
 * 1. Address 로드
 *    + LoadAsync("600003_Click")
 *    + Release("600003_Click")
 *
 * 2. Label 전체 로드
 *    + LoadAllByLabelAsync("audio")
 *    + ReleaseAllByLabel("audio")
 *
 * 3. Label 첫 번째 로드
 *    + LoadFirstByLabelAsync("UI_Common")
 *    + ReleaseLabel("UI_Common")
 *
 * 4. Label 단일 로드
 *    + LoadSingleByLabelAsync("OnlyOne")
 *    + ReleaseSingleLabel("OnlyOne")
 *
 * 5. Label index 로드
 *    + LoadByLabelIndexAsync("UI_Common", 2)
 *    + ReleaseLabel("UI_Common", 2)
 *
 * 내부 관리 ::
 * 1. Address / LabelAll / LabelFirst / LabelSingle / LabelIndex는 서로 다른 내부 handle key를 사용합니다.
 * 2. 같은 key라도 로드 방식이 다르면 별도 handle로 관리됩니다.
 * 3. Label 전체 로드는 LoadAssetsAsync handle로 관리됩니다.
 * 4. Label 선택 로드에 사용된 locationHandle은 로드 직후 내부에서 즉시 해제됩니다.
 * 5. 실제 Asset handle은 Release 계열 함수 또는 ReleaseAll로 해제됩니다.
 *
 * 주의사항 ::
 * 1. Address key와 Label name은 자동 판별되지 않습니다.
 * 2. 어떤 방식으로 로드했는지 호출부가 명확히 선택해야 합니다.
 * 3. Label 전체 로드와 Label 단건 선택 로드는 서로 다른 개념입니다.
 * 4. Release도 반드시 같은 방식의 함수를 사용해야 합니다.
 * =========================================================
 */
#endif

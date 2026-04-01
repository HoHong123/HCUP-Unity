#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Addressables 범용 로드 시스템 테스트용 UGUI 컴포넌트입니다.
 * =========================================================
 */
#endif

using System;
using System.Text;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
using UnityEngine.UI;
using TMPro;
using HUtil.Inspector;
using HUtil.Logger;
using HUtil.AssetHandler.Data;
using HUtil.AssetHandler.Load;
using HUtil.AssetHandler.Provider;
using HUtil.AssetHandler.Subscription;

namespace HUtil.Data.Debug {
    public sealed class AddressableLoadSequenceTester : MonoBehaviour {
        #region Private - Types
        [Serializable]
        sealed class LoadRecord {
            public int Id;
            public LoadMode Mode;
            public string RequestKey;
            public string DisplayKey;
            public UnityEngine.Object Asset;
            public List<UnityEngine.Object> Assets = new();
            public DateTime LoadedAt;
            public bool IsReleased;
            public bool IsMulti => Assets != null && Assets.Count > 0;
            public int AssetCount => IsMulti ? Assets.Count : Asset ? 1 : 0;
        }

        enum LoadMode {
            None = 0,
            Address = 1,
            LabelAll = 2,
            LabelFirst = 3,
            LabelSingle = 4,
            LabelIndex = 5
        }
        #endregion

        #region Private - Serialized Fields
        [HTitle("Panel")]
        [SerializeField]
        [FormerlySerializedAs("showGui")]
        bool showPanel = true;

        [HTitle("Keys")]
        [SerializeField]
        string addressKey = "60000_Click";
        [SerializeField]
        string labelName = "audio";
        [SerializeField]
        int labelIndex;
        [SerializeField]
        LoadMode interactionMode = LoadMode.Address;

        [HTitle("Log")]
        [SerializeField]
        bool autoReleaseSameRequestBeforeLoad;
        [SerializeField]
        bool logSuccess = true;
        [SerializeField]
        bool logFailure = true;

        [HTitle("UI")]
        [SerializeField]
        Canvas targetCanvas;
        [SerializeField]
        [FormerlySerializedAs("windowRoot")]
        RectTransform panelRoot;
        [SerializeField]
        TMP_Text statusText;
        [SerializeField]
        TMP_InputField addressInputField;
        [SerializeField]
        TMP_InputField labelInputField;
        [SerializeField]
        TMP_InputField labelIndexInputField;
        [SerializeField]
        Button autoReleaseButton;
        [SerializeField]
        TMP_Text autoReleaseButtonText;
        [SerializeField]
        Button logSuccessButton;
        [SerializeField]
        TMP_Text logSuccessButtonText;
        [SerializeField]
        Button logFailureButton;
        [SerializeField]
        TMP_Text logFailureButtonText;
        [SerializeField]
        TMP_Text recordText;

        [HTitle("Records")]
        [SerializeField]
        List<LoadRecord> records = new();
        #endregion

        #region Private - Fields
        IAssetProvider<string, UnityEngine.Object> assetProvider;
        IAddressableLabelLoader<UnityEngine.Object> addressableLabelLoader;
        AssetOwnerId ownerId;
        bool isLoading;
        int nextRecordId = 1;
        #endregion

        #region Public - Properties
        public bool IsVisible {
            get => showPanel;
            set {
                showPanel = value;
                if (panelRoot != null) panelRoot.gameObject.SetActive(showPanel);
            }
        }
        #endregion

        #region Private - Constructors / Init
        private void Awake() {
            assetProvider = AssetProviderFactory.CreateAddressable<UnityEngine.Object>();
            addressableLabelLoader = new AddressableLabelLoader<UnityEngine.Object>();
            ownerId = AssetOwnerIdGenerator.NewId(this);

            Assert.IsNotNull(assetProvider, "[AddressableLoadSequenceTester] AssetProvider is null.");
            Assert.IsNotNull(addressableLabelLoader, "[AddressableLoadSequenceTester] AddressableLabelLoader is null.");
            Assert.IsTrue(ownerId.IsValid, "[AddressableLoadSequenceTester] OwnerId is invalid.");

            _EnsureUi();
            _BindUi();
            _PushStateToUi();
            _RefreshUi();
        }
        #endregion

        #region Private - Unity Life Cycle
        private void OnDestroy() {
            _ReleaseAllInternal();
            if (ownerId.IsValid) {
                AssetOwnerIdGenerator.NotifyReleased(ownerId);
                ownerId = AssetOwnerId.None;
            }
        }
        #endregion

        #region Public - Controls
        public void SetVisible(bool visible) {
            IsVisible = visible;
        }
        #endregion

        #region Private - UI
        private void _EnsureUi() {
            Assert.IsNotNull(targetCanvas, "[AddressableLoadSequenceTester] TargetCanvas is null.");
            Assert.IsNotNull(panelRoot, "[AddressableLoadSequenceTester] PanelRoot is null.");
            Assert.IsNotNull(statusText, "[AddressableLoadSequenceTester] StatusText is null.");
            Assert.IsNotNull(addressInputField, "[AddressableLoadSequenceTester] AddressInputField is null.");
            Assert.IsNotNull(labelInputField, "[AddressableLoadSequenceTester] LabelInputField is null.");
            Assert.IsNotNull(labelIndexInputField, "[AddressableLoadSequenceTester] LabelIndexInputField is null.");
            Assert.IsNotNull(autoReleaseButton, "[AddressableLoadSequenceTester] AutoReleaseButton is null.");
            Assert.IsNotNull(logSuccessButton, "[AddressableLoadSequenceTester] LogSuccessButton is null.");
            Assert.IsNotNull(logFailureButton, "[AddressableLoadSequenceTester] LogFailureButton is null.");
            Assert.IsNotNull(autoReleaseButtonText, "[AddressableLoadSequenceTester] AutoReleaseButtonText is null.");
            Assert.IsNotNull(logSuccessButtonText, "[AddressableLoadSequenceTester] LogSuccessButtonText is null.");
            Assert.IsNotNull(logFailureButtonText, "[AddressableLoadSequenceTester] LogFailureButtonText is null.");
            Assert.IsNotNull(recordText, "[AddressableLoadSequenceTester] RecordText is null.");
        }

        private void _BindUi() {
            autoReleaseButton.onClick.RemoveAllListeners();
            logSuccessButton.onClick.RemoveAllListeners();
            logFailureButton.onClick.RemoveAllListeners();

            autoReleaseButton.onClick.AddListener(_HandleCycleInteractionMode);
            logSuccessButton.onClick.AddListener(_HandleExecuteCurrentLoad);
            logFailureButton.onClick.AddListener(_HandleExecuteCurrentRelease);
        }
        #endregion

        #region Private - UI Event
        private void _HandleCycleInteractionMode() {
            interactionMode = _GetNextInteractionMode(interactionMode);
            _RefreshUi();
        }

        private void _HandleExecuteCurrentLoad() {
            _PullStateFromUi();
            switch (interactionMode) {
            case LoadMode.Address:
                _LoadAddressAsync().Forget();
                return;
            case LoadMode.LabelAll:
                _LoadLabelAllAsync().Forget();
                return;
            case LoadMode.LabelFirst:
                _LoadLabelFirstAsync().Forget();
                return;
            case LoadMode.LabelSingle:
                _LoadLabelSingleAsync().Forget();
                return;
            case LoadMode.LabelIndex:
                _LoadLabelIndexAsync().Forget();
                return;
            default:
                _LogWarning("[AddressableLoadSequenceTester] Current mode does not support load.");
                return;
            }
        }

        private void _HandleExecuteCurrentRelease() {
            _PullStateFromUi();
            switch (interactionMode) {
            case LoadMode.Address:
                _ReleaseAddressInternal(addressKey);
                break;
            case LoadMode.LabelAll:
                _ReleaseLabelAllInternal(labelName);
                break;
            case LoadMode.LabelFirst:
                _ReleaseLabelFirstInternal(labelName);
                break;
            case LoadMode.LabelSingle:
                _ReleaseLabelSingleInternal(labelName);
                break;
            case LoadMode.LabelIndex:
                _ReleaseLabelIndexInternal(labelName, labelIndex);
                break;
            default:
                _ReleaseAllInternal();
                break;
            }

            _RefreshUi();
        }

        private void _HandleLoadAddress() {
            _PullStateFromUi();
            _LoadAddressAsync().Forget();
        }

        private void _HandleLoadLabelAll() {
            _PullStateFromUi();
            _LoadLabelAllAsync().Forget();
        }

        private void _HandleLoadLabelFirst() {
            _PullStateFromUi();
            _LoadLabelFirstAsync().Forget();
        }

        private void _HandleLoadLabelSingle() {
            _PullStateFromUi();
            _LoadLabelSingleAsync().Forget();
        }

        private void _HandleLoadLabelIndex() {
            _PullStateFromUi();
            _LoadLabelIndexAsync().Forget();
        }

        private void _HandleReleaseAddress() {
            _PullStateFromUi();
            _ReleaseAddressInternal(addressKey);
            _RefreshUi();
        }

        private void _HandleReleaseLabelAll() {
            _PullStateFromUi();
            _ReleaseLabelAllInternal(labelName);
            _RefreshUi();
        }

        private void _HandleReleaseLabelFirst() {
            _PullStateFromUi();
            _ReleaseLabelFirstInternal(labelName);
            _RefreshUi();
        }

        private void _HandleReleaseLabelSingle() {
            _PullStateFromUi();
            _ReleaseLabelSingleInternal(labelName);
            _RefreshUi();
        }

        private void _HandleReleaseLabelIndex() {
            _PullStateFromUi();
            _ReleaseLabelIndexInternal(labelName, labelIndex);
            _RefreshUi();
        }

        private void _HandleReleaseAll() {
            _ReleaseAllInternal();
            _RefreshUi();
        }
        #endregion

        #region Private - Load
        private async UniTaskVoid _LoadAddressAsync() {
            if (!_CanStartLoad()) return;
            if (string.IsNullOrWhiteSpace(addressKey)) {
                _LogWarning("[AddressableLoadSequenceTester] AddressKey is empty.");
                return;
            }

            isLoading = true;
            _RefreshUi();
            try {
                if (autoReleaseSameRequestBeforeLoad) _ReleaseSameRequestInternal(LoadMode.Address, addressKey);
                var request = new AssetRequest<string>(
                    key: addressKey,
                    loadMode: AssetLoadMode.Addressable,
                    fetchMode: AssetFetchMode.CacheFirst,
                    ownerId: ownerId);
                var asset = await assetProvider.GetAsync(request);
                _RegisterSingleRecord(asset, LoadMode.Address, addressKey, addressKey);
                _LogLoadResult(LoadMode.Address, addressKey, asset);
            }
            finally {
                isLoading = false;
                _RefreshUi();
            }
        }

        private async UniTaskVoid _LoadLabelAllAsync() {
            if (!_CanStartLoad()) return;
            if (string.IsNullOrWhiteSpace(labelName)) {
                _LogWarning("[AddressableLoadSequenceTester] LabelName is empty.");
                return;
            }

            isLoading = true;
            _RefreshUi();
            try {
                if (autoReleaseSameRequestBeforeLoad) _ReleaseSameRequestInternal(LoadMode.LabelAll, labelName);
                var assets = await addressableLabelLoader.LoadAllAsync(labelName);
                _RegisterMultiRecord(assets, LoadMode.LabelAll, labelName, labelName);
                _LogMultiLoadResult(LoadMode.LabelAll, labelName, assets);
            }
            finally {
                isLoading = false;
                _RefreshUi();
            }
        }

        private async UniTaskVoid _LoadLabelFirstAsync() {
            if (!_CanStartLoad()) return;
            if (string.IsNullOrWhiteSpace(labelName)) {
                _LogWarning("[AddressableLoadSequenceTester] LabelName is empty.");
                return;
            }

            isLoading = true;
            _RefreshUi();
            try {
                if (autoReleaseSameRequestBeforeLoad) _ReleaseSameRequestInternal(LoadMode.LabelFirst, labelName);
                var asset = await addressableLabelLoader.LoadFirstAsync(labelName);
                _RegisterSingleRecord(asset, LoadMode.LabelFirst, labelName, labelName);
                _LogLoadResult(LoadMode.LabelFirst, labelName, asset);
            }
            finally {
                isLoading = false;
                _RefreshUi();
            }
        }

        private async UniTaskVoid _LoadLabelSingleAsync() {
            if (!_CanStartLoad()) return;
            if (string.IsNullOrWhiteSpace(labelName)) {
                _LogWarning("[AddressableLoadSequenceTester] LabelName is empty.");
                return;
            }

            isLoading = true;
            _RefreshUi();
            try {
                if (autoReleaseSameRequestBeforeLoad) _ReleaseSameRequestInternal(LoadMode.LabelSingle, labelName);
                var asset = await addressableLabelLoader.LoadSingleAsync(labelName);
                _RegisterSingleRecord(asset, LoadMode.LabelSingle, labelName, labelName);
                _LogLoadResult(LoadMode.LabelSingle, labelName, asset);
            }
            finally {
                isLoading = false;
                _RefreshUi();
            }
        }

        private async UniTaskVoid _LoadLabelIndexAsync() {
            if (!_CanStartLoad()) return;
            if (string.IsNullOrWhiteSpace(labelName)) {
                _LogWarning("[AddressableLoadSequenceTester] LabelName is empty.");
                return;
            }
            if (labelIndex < 0) {
                _LogWarning("[AddressableLoadSequenceTester] LabelIndex must be >= 0.");
                return;
            }

            isLoading = true;
            _RefreshUi();
            try {
                string displayKey = _BuildLabelIndexDisplayKey(labelName, labelIndex);
                if (autoReleaseSameRequestBeforeLoad) _ReleaseSameRequestInternal(LoadMode.LabelIndex, displayKey);
                var asset = await addressableLabelLoader.LoadByIndexAsync(labelName, labelIndex);
                _RegisterSingleRecord(asset, LoadMode.LabelIndex, labelName, displayKey);
                _LogLoadResult(LoadMode.LabelIndex, displayKey, asset);
            }
            finally {
                isLoading = false;
                _RefreshUi();
            }
        }
        #endregion

        #region Private - Release
        private void _ReleaseAddressInternal(string key) {
            if (string.IsNullOrWhiteSpace(key)) return;
            assetProvider.Release(key, ownerId);
            _MarkReleasedByRequest(LoadMode.Address, key);
            _Log($"[AddressableLoadSequenceTester] Release Address :: key={key}, ownerId={ownerId.Value}");
        }

        private void _ReleaseLabelAllInternal(string label) {
            if (string.IsNullOrWhiteSpace(label)) return;
            addressableLabelLoader.ReleaseAllByLabel(label);
            _MarkReleasedByRequest(LoadMode.LabelAll, label);
            _Log($"[AddressableLoadSequenceTester] Release Label All :: label={label}");
        }

        private void _ReleaseLabelFirstInternal(string label) {
            if (string.IsNullOrWhiteSpace(label)) return;
            addressableLabelLoader.ReleaseFirstByLabel(label);
            _MarkReleasedByRequest(LoadMode.LabelFirst, label);
            _Log($"[AddressableLoadSequenceTester] Release Label First :: label={label}");
        }

        private void _ReleaseLabelSingleInternal(string label) {
            if (string.IsNullOrWhiteSpace(label)) return;
            addressableLabelLoader.ReleaseSingleByLabel(label);
            _MarkReleasedByRequest(LoadMode.LabelSingle, label);
            _Log($"[AddressableLoadSequenceTester] Release Label Single :: label={label}");
        }

        private void _ReleaseLabelIndexInternal(string label, int index) {
            if (string.IsNullOrWhiteSpace(label)) return;
            if (index < 0) return;
            addressableLabelLoader.ReleaseByLabelIndex(label, index);
            _MarkReleasedByDisplay(LoadMode.LabelIndex, _BuildLabelIndexDisplayKey(label, index));
            _Log($"[AddressableLoadSequenceTester] Release Label Index :: label={label}, index={index}");
        }

        private void _ReleaseAllInternal() {
            assetProvider?.ReleaseOwner(ownerId);
            addressableLabelLoader?.ReleaseAll();
            for (int k = 0; k < records.Count; k++) records[k].IsReleased = true;
            _Log($"[AddressableLoadSequenceTester] Release All :: ownerId={ownerId.Value}");
        }
        #endregion

        #region Private - Record / Refresh / Utility
        private void _ReleaseSameRequestInternal(LoadMode mode, string keyOrDisplayKey) {
            for (int k = 0; k < records.Count; k++) {
                var record = records[k];
                if (record == null || record.IsReleased) continue;
                if (record.Mode != mode) continue;

                bool isMatch = mode == LoadMode.LabelIndex
                    ? string.Equals(record.DisplayKey, keyOrDisplayKey, StringComparison.Ordinal)
                    : string.Equals(record.RequestKey, keyOrDisplayKey, StringComparison.Ordinal);

                if (isMatch) record.IsReleased = true;
            }
        }

        private void _MarkReleasedByRequest(LoadMode mode, string requestKey) {
            for (int k = 0; k < records.Count; k++) {
                var record = records[k];
                if (record == null) continue;
                if (record.Mode != mode) continue;
                if (string.Equals(record.RequestKey, requestKey, StringComparison.Ordinal)) record.IsReleased = true;
            }
        }

        private void _MarkReleasedByDisplay(LoadMode mode, string displayKey) {
            for (int k = 0; k < records.Count; k++) {
                var record = records[k];
                if (record == null) continue;
                if (record.Mode != mode) continue;
                if (string.Equals(record.DisplayKey, displayKey, StringComparison.Ordinal)) record.IsReleased = true;
            }
        }

        private void _RegisterSingleRecord(UnityEngine.Object asset, LoadMode mode, string requestKey, string displayKey) {
            records.Add(new LoadRecord {
                Id = nextRecordId++,
                Mode = mode,
                RequestKey = requestKey,
                DisplayKey = displayKey,
                Asset = asset,
                LoadedAt = DateTime.Now
            });
        }

        private void _RegisterMultiRecord(IList<UnityEngine.Object> assets, LoadMode mode, string requestKey, string displayKey) {
            LoadRecord record = new LoadRecord {
                Id = nextRecordId++,
                Mode = mode,
                RequestKey = requestKey,
                DisplayKey = displayKey,
                LoadedAt = DateTime.Now
            };

            if (assets != null) {
                for (int k = 0; k < assets.Count; k++) record.Assets.Add(assets[k]);
            }

            records.Add(record);
        }

        private void _RefreshUi() {
            IsVisible = showPanel;
            _PushStateToUi();
            statusText.text =
                $"System :: HUtil.AssetHandler\n" +
                $"PlayMode :: {Application.isPlaying}\n" +
                $"IsLoading :: {isLoading}\n" +
                $"InteractionMode :: {_GetModeDisplayName(interactionMode)}\n" +
                $"AutoReleaseSameRequest :: {autoReleaseSameRequestBeforeLoad}\n" +
                $"LogSuccess :: {logSuccess}\n" +
                $"LogFailure :: {logFailure}\n" +
                $"OwnerId :: {ownerId.Value}\n" +
                $"LoadedCount :: {_GetAliveRecordCount()} / TotalRecordCount :: {records.Count}";
            recordText.text = _BuildRecordText();
        }

        private void _PushStateToUi() {
            addressInputField.text = addressKey ?? string.Empty;
            labelInputField.text = labelName ?? string.Empty;
            labelIndexInputField.text = labelIndex.ToString();
            autoReleaseButtonText.text = $"Mode :: {_GetModeDisplayName(interactionMode)}";
            logSuccessButtonText.text = interactionMode == LoadMode.None
                ? "Load N/A"
                : $"Load :: {_GetModeDisplayName(interactionMode)}";
            logFailureButtonText.text = interactionMode == LoadMode.None
                ? "Release :: All"
                : $"Release :: {_GetModeDisplayName(interactionMode)}";
            logSuccessButton.interactable = interactionMode != LoadMode.None;
        }

        private void _PullStateFromUi() {
            addressKey = addressInputField.text ?? string.Empty;
            labelName = labelInputField.text ?? string.Empty;
            if (int.TryParse(labelIndexInputField.text, out int parsedIndex)) labelIndex = Mathf.Max(0, parsedIndex);
            else labelIndex = 0;
        }

        private int _GetAliveRecordCount() {
            int count = 0;
            for (int k = 0; k < records.Count; k++) {
                if (records[k] != null && !records[k].IsReleased) count++;
            }
            return count;
        }

        private string _BuildRecordText() {
            if (records.Count < 1) return "No Records";

            StringBuilder builder = new StringBuilder();
            for (int k = 0; k < records.Count; k++) {
                LoadRecord record = records[k];
                builder.Append('#').Append(record.Id).Append("  Mode :: ").Append(record.Mode).Append("  Released :: ").Append(record.IsReleased).Append('\n');
                builder.Append("RequestKey :: ").Append(record.RequestKey).Append('\n');
                builder.Append("DisplayKey :: ").Append(record.DisplayKey).Append('\n');
                builder.Append("AssetCount :: ").Append(record.AssetCount).Append('\n');
                builder.Append("LoadedAt :: ").Append(record.LoadedAt.ToString("HH:mm:ss.fff")).Append('\n');

                if (record.IsMulti) {
                    for (int a = 0; a < record.Assets.Count; a++) {
                        var asset = record.Assets[a];
                        builder.Append("  [").Append(a).Append("] ").Append(_GetObjectDisplayName(asset)).Append(" (").Append(_GetObjectTypeName(asset)).Append(')').Append('\n');
                    }
                }
                else {
                    builder.Append("Object :: ").Append(_GetObjectDisplayName(record.Asset)).Append('\n');
                    builder.Append("ObjectType :: ").Append(_GetObjectTypeName(record.Asset)).Append('\n');
                }

                builder.Append('\n');
            }

            return builder.ToString();
        }

        private string _BuildLabelIndexDisplayKey(string label, int index) {
            return $"{label}::{index}";
        }

        private LoadMode _GetNextInteractionMode(LoadMode mode) {
            switch (mode) {
            case LoadMode.Address:
                return LoadMode.LabelAll;
            case LoadMode.LabelAll:
                return LoadMode.LabelFirst;
            case LoadMode.LabelFirst:
                return LoadMode.LabelSingle;
            case LoadMode.LabelSingle:
                return LoadMode.LabelIndex;
            case LoadMode.LabelIndex:
                return LoadMode.None;
            default:
                return LoadMode.Address;
            }
        }

        private string _GetModeDisplayName(LoadMode mode) {
            switch (mode) {
            case LoadMode.Address:
                return "Address";
            case LoadMode.LabelAll:
                return "Label All";
            case LoadMode.LabelFirst:
                return "Label First";
            case LoadMode.LabelSingle:
                return "Label Single";
            case LoadMode.LabelIndex:
                return "Label Index";
            default:
                return "Release All";
            }
        }

        private bool _CanStartLoad() {
            if (!Application.isPlaying) {
                _LogWarning("[AddressableLoadSequenceTester] Play Mode only.");
                return false;
            }
            if (isLoading) {
                _LogWarning("[AddressableLoadSequenceTester] Already loading.");
                return false;
            }

            Assert.IsNotNull(assetProvider, "[AddressableLoadSequenceTester] AssetProvider is null.");
            Assert.IsNotNull(addressableLabelLoader, "[AddressableLoadSequenceTester] AddressableLabelLoader is null.");
            return true;
        }

        private string _GetObjectDisplayName(UnityEngine.Object asset) {
            return asset ? asset.name : "(null)";
        }

        private string _GetObjectTypeName(UnityEngine.Object asset) {
            return asset ? asset.GetType().Name : "(null)";
        }

        private void _LogLoadResult(LoadMode mode, string key, UnityEngine.Object asset) {
            if (asset) {
                if (logSuccess) _Log($"[AddressableLoadSequenceTester] Load Success :: mode={mode}, key={key}, object={asset.name}, type={asset.GetType().Name}");
                return;
            }
            if (logFailure) _LogWarning($"[AddressableLoadSequenceTester] Load Failed :: mode={mode}, key={key}");
        }

        private void _LogMultiLoadResult(LoadMode mode, string key, IList<UnityEngine.Object> assets) {
            int count = assets?.Count ?? 0;
            if (count > 0) {
                if (logSuccess) _Log($"[AddressableLoadSequenceTester] Load Success :: mode={mode}, key={key}, count={count}");
                return;
            }
            if (logFailure) _LogWarning($"[AddressableLoadSequenceTester] Load Failed :: mode={mode}, key={key}, count=0");
        }

        private void _Log(string message) {
            HLogger.Log(message, gameObject);
        }

        private void _LogWarning(string message) {
            HLogger.Warning(message, gameObject);
        }
        #endregion
    }
}

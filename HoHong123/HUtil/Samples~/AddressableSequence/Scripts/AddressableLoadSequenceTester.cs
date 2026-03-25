#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Addressables 범용 로드 시스템 테스트용 IMGUI 컴포넌트입니다.
 *
 * 주의사항 ::
 * 1. Play Mode에서만 테스트해야 합니다.
 * 2. Address / LabelAll / LabelFirst / LabelSingle / LabelIndex를 각각 명시적으로 검증합니다.
 * 3. 이 스크립트는 Addressables에 등록된 에셋 자체를 대상으로 동작합니다.
 * 4. 동일한 요청 키를 중복 로드하면 내부 캐시 정책에 따라 같은 handle 결과를 참조할 수 있습니다.
 * =========================================================
 */

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using HUtil.Sample.Addressable;
using HUtil.Logger;
using HUtil.Inspector;

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

        #region Fields
        [HTitle("GUI")]
        [SerializeField]
        bool showGui = true;
        [SerializeField]
        Rect windowRect = new Rect(0, 0, 720f, 1280f);

        [HTitle("Keys")]
        [SerializeField]
        string addressKey = "60000_Click";
        [SerializeField]
        string labelName = "audio";
        [SerializeField]
        int labelIndex = 0;

        [HTitle("Log")]
        [SerializeField]
        bool autoReleaseSameRequestBeforeLoad = false;
        [SerializeField]
        bool logSuccess = true;
        [SerializeField]
        bool logFailure = true;

        [HTitle("Records")]
        [SerializeField]
        List<LoadRecord> records = new();

        ObjectAddressableLoadSequence loadSequence;
        Vector2 scrollPosition;
        int windowId;
        int nextRecordId = 1;
        bool isLoading;
        #endregion

        #region Properties
        public bool ShowGui {
            get => showGui;
            set => showGui = value;
        }
        #endregion

        #region Constructors / Init
        private void Awake() {
            loadSequence = new ObjectAddressableLoadSequence();
            Assert.IsNotNull(loadSequence);
            windowId = GetInstanceID();
        }
        #endregion

        #region Unity Life Cycle
        private void OnDestroy() {
            _ReleaseAllInternal();
        }
        private void OnGUI() {
            if (!showGui) return;
            windowRect = GUI.Window(windowId, windowRect, _DrawWindow, "Addressable Load Sequence Tester");
        }
        #endregion

        #region === Public Controls Feature ===
        #region Public - Toggle
        public void SetVisible(bool visible) {
            showGui = visible;
        }
        #endregion
        #endregion

        #region === IMGUI Feature ===
        #region Private - Window
        private void _DrawWindow(int id) {
            _DrawHeader();
            _DrawInputFields();
            _DrawButtons();
            _DrawRecords();
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 22f));
        }

        private void _DrawHeader() {
            GUILayout.Label($"PlayMode :: {Application.isPlaying}");
            GUILayout.Label($"IsLoading :: {isLoading}");
            GUILayout.Label($"LoadedCount :: {_GetAliveRecordCount()} / TotalRecordCount :: {records.Count}");
            GUILayout.Space(4f);
        }

        private void _DrawInputFields() {
            GUILayout.Label("Address Request");
            addressKey = GUILayout.TextField(addressKey ?? string.Empty);

            GUILayout.Space(4f);

            GUILayout.Label("Label Request");
            labelName = GUILayout.TextField(labelName ?? string.Empty);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Label Index", GUILayout.Width(90f));

            string indexText = GUILayout.TextField(labelIndex.ToString(), GUILayout.Width(100f));
            if (int.TryParse(indexText, out int parsedIndex)) {
                labelIndex = Mathf.Max(0, parsedIndex);
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            autoReleaseSameRequestBeforeLoad = GUILayout.Toggle(autoReleaseSameRequestBeforeLoad, "Auto Release Same Request Before Load");
            logSuccess = GUILayout.Toggle(logSuccess, "Log Success");
            logFailure = GUILayout.Toggle(logFailure, "Log Failure");

            GUILayout.Space(6f);
        }

        private void _DrawButtons() {
            GUILayout.BeginHorizontal();
            GUI.enabled = Application.isPlaying && !isLoading;

            if (GUILayout.Button("Load Address", GUILayout.Height(28f)))
                _LoadAddressAsync().Forget();

            if (GUILayout.Button("Load Label All", GUILayout.Height(28f)))
                _LoadLabelAllAsync().Forget();

            if (GUILayout.Button("Load Label First", GUILayout.Height(28f)))
                _LoadLabelFirstAsync().Forget();

            if (GUILayout.Button("Load Label Single", GUILayout.Height(28f)))
                _LoadLabelSingleAsync().Forget();

            if (GUILayout.Button("Load Label Index", GUILayout.Height(28f)))
                _LoadLabelIndexAsync().Forget();

            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUI.enabled = Application.isPlaying;

            if (GUILayout.Button("Release Address", GUILayout.Height(24f)))
                _ReleaseAddressInternal(addressKey);

            if (GUILayout.Button("Release Label All", GUILayout.Height(24f)))
                _ReleaseLabelAllInternal(labelName);

            if (GUILayout.Button("Release Label First", GUILayout.Height(24f)))
                _ReleaseLabelFirstInternal(labelName);

            if (GUILayout.Button("Release Label Single", GUILayout.Height(24f)))
                _ReleaseLabelSingleInternal(labelName);

            if (GUILayout.Button("Release Label Index", GUILayout.Height(24f)))
                _ReleaseLabelIndexInternal(labelName, labelIndex);

            if (GUILayout.Button("Release All", GUILayout.Height(24f)))
                _ReleaseAllInternal();

            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
        }

        private void _DrawRecords() {
            GUILayout.Label("Loaded Records");
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(480f));

            if (records.Count < 1) {
                GUILayout.Box("No Records");
                GUILayout.EndScrollView();
                return;
            }

            for (int k = 0; k < records.Count; k++) {
                _DrawRecord(records[k]);
            }

            GUILayout.EndScrollView();
        }

        private void _DrawRecord(LoadRecord record) {
            GUI.enabled = true;

            GUILayout.BeginVertical("box");
            GUILayout.Label($"#{record.Id}  Mode :: {record.Mode}  Released :: {record.IsReleased}");
            GUILayout.Label($"RequestKey :: {record.RequestKey}");
            GUILayout.Label($"DisplayKey :: {record.DisplayKey}");
            GUILayout.Label($"AssetCount :: {record.AssetCount}");
            GUILayout.Label($"LoadedAt :: {record.LoadedAt:HH:mm:ss.fff}");

            if (record.IsMulti) {
                for (int k = 0; k < record.Assets.Count; k++) {
                    UnityEngine.Object asset = record.Assets[k];
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"[{k}] {_GetObjectDisplayName(asset)} ({_GetObjectTypeName(asset)})");

                    GUI.enabled = Application.isPlaying && asset;
                    if (GUILayout.Button("Ping", GUILayout.Width(60f)))
                        _PingObjectInternal(asset);

                    GUI.enabled = true;
                    GUILayout.EndHorizontal();
                }
            }
            else {
                GUILayout.Label($"Object :: {_GetObjectDisplayName(record.Asset)}");
                GUILayout.Label($"ObjectType :: {_GetObjectTypeName(record.Asset)}");

                GUILayout.BeginHorizontal();

                GUI.enabled = Application.isPlaying && !record.IsReleased;
                if (GUILayout.Button("Release This", GUILayout.Height(22f)))
                    _ReleaseRecordInternal(record);

                GUI.enabled = Application.isPlaying && record.Asset;
                if (GUILayout.Button("Ping", GUILayout.Height(22f)))
                    _PingObjectInternal(record.Asset);

                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }

            if (record.IsMulti) {
                GUILayout.BeginHorizontal();

                GUI.enabled = Application.isPlaying && !record.IsReleased;
                if (GUILayout.Button("Release This Group", GUILayout.Height(22f)))
                    _ReleaseRecordInternal(record);

                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
            GUILayout.Space(2f);
        }
        #endregion
        #endregion

        #region === Load Feature ===
        #region Private - Address Load
        private async UniTaskVoid _LoadAddressAsync() {
            if (!_CanStartLoad()) return;

            if (string.IsNullOrWhiteSpace(addressKey)) {
                _LogWarning("[AddressableLoadSequenceTester] AddressKey is empty.");
                return;
            }

            isLoading = true;

            try {
                if (autoReleaseSameRequestBeforeLoad)
                    _ReleaseSameRequestInternal(LoadMode.Address, addressKey);

                UnityEngine.Object asset = await loadSequence.LoadAsync(addressKey);
                _RegisterSingleRecord(asset, LoadMode.Address, addressKey, addressKey);
                _LogLoadResult(LoadMode.Address, addressKey, asset);
            }
            finally {
                isLoading = false;
            }
        }
        #endregion

        #region Private - Label All Load
        private async UniTaskVoid _LoadLabelAllAsync() {
            if (!_CanStartLoad()) return;

            if (string.IsNullOrWhiteSpace(labelName)) {
                _LogWarning("[AddressableLoadSequenceTester] LabelName is empty.");
                return;
            }

            isLoading = true;

            try {
                if (autoReleaseSameRequestBeforeLoad)
                    _ReleaseSameRequestInternal(LoadMode.LabelAll, labelName);

                IList<UnityEngine.Object> assets = await loadSequence.LoadAllByLabelAsync(labelName);
                _RegisterMultiRecord(assets, LoadMode.LabelAll, labelName, labelName);
                _LogMultiLoadResult(LoadMode.LabelAll, labelName, assets);
            }
            finally {
                isLoading = false;
            }
        }
        #endregion

        #region Private - Label First Load
        private async UniTaskVoid _LoadLabelFirstAsync() {
            if (!_CanStartLoad()) return;

            if (string.IsNullOrWhiteSpace(labelName)) {
                _LogWarning("[AddressableLoadSequenceTester] LabelName is empty.");
                return;
            }

            isLoading = true;

            try {
                if (autoReleaseSameRequestBeforeLoad)
                    _ReleaseSameRequestInternal(LoadMode.LabelFirst, labelName);

                UnityEngine.Object asset = await loadSequence.LoadFirstByLabelAsync(labelName);
                _RegisterSingleRecord(asset, LoadMode.LabelFirst, labelName, labelName);
                _LogLoadResult(LoadMode.LabelFirst, labelName, asset);
            }
            finally {
                isLoading = false;
            }
        }
        #endregion

        #region Private - Label Single Load
        private async UniTaskVoid _LoadLabelSingleAsync() {
            if (!_CanStartLoad()) return;

            if (string.IsNullOrWhiteSpace(labelName)) {
                _LogWarning("[AddressableLoadSequenceTester] LabelName is empty.");
                return;
            }

            isLoading = true;

            try {
                if (autoReleaseSameRequestBeforeLoad)
                    _ReleaseSameRequestInternal(LoadMode.LabelSingle, labelName);

                UnityEngine.Object asset = await loadSequence.LoadSingleByLabelAsync(labelName);
                _RegisterSingleRecord(asset, LoadMode.LabelSingle, labelName, labelName);
                _LogLoadResult(LoadMode.LabelSingle, labelName, asset);
            }
            finally {
                isLoading = false;
            }
        }
        #endregion

        #region Private - Label Index Load
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

            try {
                string displayKey = _BuildLabelIndexDisplayKey(labelName, labelIndex);

                if (autoReleaseSameRequestBeforeLoad)
                    _ReleaseSameRequestInternal(LoadMode.LabelIndex, displayKey);

                UnityEngine.Object asset = await loadSequence.LoadByLabelIndexAsync(labelName, labelIndex);
                _RegisterSingleRecord(asset, LoadMode.LabelIndex, labelName, displayKey);
                _LogLoadResult(LoadMode.LabelIndex, displayKey, asset);
            }
            finally {
                isLoading = false;
            }
        }
        #endregion
        #endregion

        #region === Release Feature ===
        #region Private - Release Common
        private void _ReleaseRecordInternal(LoadRecord record) {
            if (record == null) return;
            if (record.IsReleased) return;

            switch (record.Mode) {
            case LoadMode.Address:
                _ReleaseAddressInternal(record.RequestKey);
                break;

            case LoadMode.LabelAll:
                _ReleaseLabelAllInternal(record.RequestKey);
                break;

            case LoadMode.LabelFirst:
                _ReleaseLabelFirstInternal(record.RequestKey);
                break;

            case LoadMode.LabelSingle:
                _ReleaseLabelSingleInternal(record.RequestKey);
                break;

            case LoadMode.LabelIndex:
                if (_TryParseLabelIndexDisplayKey(record.DisplayKey, out string label, out int index))
                    _ReleaseLabelIndexInternal(label, index);
                break;
            }
        }

        private void _ReleaseAddressInternal(string key) {
            if (string.IsNullOrWhiteSpace(key)) return;
            loadSequence.Release(key);
            _MarkReleasedByRequest(LoadMode.Address, key);
            _Log($"[AddressableLoadSequenceTester] Release Address :: key={key}");
        }

        private void _ReleaseLabelAllInternal(string label) {
            if (string.IsNullOrWhiteSpace(label)) return;
            loadSequence.ReleaseAllByLabel(label);
            _MarkReleasedByRequest(LoadMode.LabelAll, label);
            _Log($"[AddressableLoadSequenceTester] Release Label All :: label={label}");
        }

        private void _ReleaseLabelFirstInternal(string label) {
            if (string.IsNullOrWhiteSpace(label)) return;
            loadSequence.ReleaseLabel(label);
            _MarkReleasedByRequest(LoadMode.LabelFirst, label);
            _Log($"[AddressableLoadSequenceTester] Release Label First :: label={label}");
        }

        private void _ReleaseLabelSingleInternal(string label) {
            if (string.IsNullOrWhiteSpace(label)) return;
            loadSequence.ReleaseSingleLabel(label);
            _MarkReleasedByRequest(LoadMode.LabelSingle, label);
            _Log($"[AddressableLoadSequenceTester] Release Label Single :: label={label}");
        }

        private void _ReleaseLabelIndexInternal(string label, int index) {
            if (string.IsNullOrWhiteSpace(label)) return;
            if (index < 0) return;

            loadSequence.ReleaseLabel(label, index);
            _MarkReleasedByDisplay(LoadMode.LabelIndex, _BuildLabelIndexDisplayKey(label, index));
            _Log($"[AddressableLoadSequenceTester] Release Label Index :: label={label}, index={index}");
        }

        private void _ReleaseAllInternal() {
            if (loadSequence == null) return;
            loadSequence.ReleaseAll();

            for (int k = 0; k < records.Count; k++) {
                records[k].IsReleased = true;
            }

            _Log("[AddressableLoadSequenceTester] Release All");
        }

        private void _ReleaseSameRequestInternal(LoadMode mode, string keyOrDisplayKey) {
            for (int k = 0; k < records.Count; k++) {
                LoadRecord record = records[k];
                if (record == null || record.IsReleased) continue;
                if (record.Mode != mode) continue;

                bool isMatch = mode == LoadMode.LabelIndex
                    ? string.Equals(record.DisplayKey, keyOrDisplayKey, StringComparison.Ordinal)
                    : string.Equals(record.RequestKey, keyOrDisplayKey, StringComparison.Ordinal);

                if (!isMatch) continue;

                _ReleaseRecordInternal(record);
            }
        }

        private void _MarkReleasedByRequest(LoadMode mode, string requestKey) {
            for (int k = 0; k < records.Count; k++) {
                LoadRecord record = records[k];
                if (record == null) continue;
                if (record.Mode != mode) continue;
                if (!string.Equals(record.RequestKey, requestKey, StringComparison.Ordinal)) continue;
                record.IsReleased = true;
            }
        }

        private void _MarkReleasedByDisplay(LoadMode mode, string displayKey) {
            for (int k = 0; k < records.Count; k++) {
                LoadRecord record = records[k];
                if (record == null) continue;
                if (record.Mode != mode) continue;
                if (!string.Equals(record.DisplayKey, displayKey, StringComparison.Ordinal)) continue;
                record.IsReleased = true;
            }
        }
        #endregion
        #endregion

        #region === Record Feature ===
        #region Private - Record
        private void _RegisterSingleRecord(UnityEngine.Object asset, LoadMode mode, string requestKey, string displayKey) {
            LoadRecord record = new LoadRecord {
                Id = nextRecordId++,
                Mode = mode,
                RequestKey = requestKey,
                DisplayKey = displayKey,
                Asset = asset,
                LoadedAt = DateTime.Now,
                IsReleased = false
            };

            records.Add(record);
        }

        private void _RegisterMultiRecord(IList<UnityEngine.Object> assets, LoadMode mode, string requestKey, string displayKey) {
            LoadRecord record = new LoadRecord {
                Id = nextRecordId++,
                Mode = mode,
                RequestKey = requestKey,
                DisplayKey = displayKey,
                LoadedAt = DateTime.Now,
                IsReleased = false
            };

            if (assets != null) {
                for (int k = 0; k < assets.Count; k++) {
                    record.Assets.Add(assets[k]);
                }
            }

            records.Add(record);
        }

        private int _GetAliveRecordCount() {
            int count = 0;

            for (int k = 0; k < records.Count; k++) {
                if (records[k] != null && !records[k].IsReleased) count++;
            }

            return count;
        }

        private string _BuildLabelIndexDisplayKey(string label, int index) {
            return $"{label}::{index}";
        }

        private bool _TryParseLabelIndexDisplayKey(string raw, out string label, out int index) {
            label = string.Empty;
            index = -1;

            if (string.IsNullOrWhiteSpace(raw)) return false;

            string[] split = raw.Split(new[] { "::" }, StringSplitOptions.None);
            if (split.Length != 2) return false;

            label = split[0];
            return int.TryParse(split[1], out index);
        }
        #endregion
        #endregion

        #region === Utility Feature ===
        #region Private - Utility
        private bool _CanStartLoad() {
            if (!Application.isPlaying) {
                _LogWarning("[AddressableLoadSequenceTester] Play Mode only.");
                return false;
            }

            if (isLoading) {
                _LogWarning("[AddressableLoadSequenceTester] Already loading.");
                return false;
            }

            Assert.IsNotNull(loadSequence);
            return true;
        }

        private string _GetObjectDisplayName(UnityEngine.Object asset) {
            return asset ? asset.name : "(null)";
        }

        private string _GetObjectTypeName(UnityEngine.Object asset) {
            return asset ? asset.GetType().Name : "(null)";
        }

        private void _PingObjectInternal(UnityEngine.Object asset) {
            if (!asset) return;
            UnityEditor.EditorGUIUtility.PingObject(asset);
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
        #endregion
    }
}
#endif

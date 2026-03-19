using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using HUtil.Logger;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HUI.DebugConsole {
    public partial class HLogConsole {
        #region Public
        public void Open() {
            panelRoot.SetActive(true);
            _RefreshRecycleView(true);
        }

        public void Close() {
            panelRoot.SetActive(false);
        }

        public void Clear() {
            entries.Clear();
            filteredEntries.Clear();
            pendingUnityEchoCountByCondition.Clear();
            isFollowingLatest = true;
            _RefreshRecycleView(true);
        }

        public void Save() {
            string savePath = _GetSaveFilePath();
            if (string.IsNullOrEmpty(savePath)) return;

            StringBuilder builder = new();
            foreach (HLogCellData entry in entries) {
                builder.AppendLine($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] [{entry.Source}] [{entry.Level}] {entry.Message}");
                if (!string.IsNullOrEmpty(entry.Debug)) builder.AppendLine($"Debug :: {entry.Debug}");
            }

            string directoryPath = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(directoryPath)) Directory.CreateDirectory(directoryPath);
            File.WriteAllText(savePath, builder.ToString(), Encoding.UTF8);
            HLogger.Log($"[HLogConsole] Saved logs to {savePath}");
        }
        #endregion

        #region Private
        private void _InitializePanelState() {
            panelRoot.SetActive(false);
        }

        private void _BindUi() {
            openButton.onClick.AddListener(Open);
            closeButton.onClick.AddListener(Close);
            clearButton.onClick.AddListener(Clear);
            saveButton.onClick.AddListener(Save);

            showLogToggle.onValueChanged.AddListener(_OnFilterChanged);
            showWarnToggle.onValueChanged.AddListener(_OnFilterChanged);
            showErrorToggle.onValueChanged.AddListener(_OnFilterChanged);
            showHLoggerToggle.onValueChanged.AddListener(_OnFilterChanged);
            showUnityToggle.onValueChanged.AddListener(_OnFilterChanged);

            recycleView.OnCellClicked = _OnCellClicked;
            recycleView.OnLatestFollowStateChanged = _OnLatestFollowStateChanged;
        }

        private void _UnbindUi() {
            openButton.onClick.RemoveListener(Open);
            closeButton.onClick.RemoveListener(Close);
            clearButton.onClick.RemoveListener(Clear);
            saveButton.onClick.RemoveListener(Save);

            showLogToggle.onValueChanged.RemoveListener(_OnFilterChanged);
            showWarnToggle.onValueChanged.RemoveListener(_OnFilterChanged);
            showErrorToggle.onValueChanged.RemoveListener(_OnFilterChanged);
            showHLoggerToggle.onValueChanged.RemoveListener(_OnFilterChanged);
            showUnityToggle.onValueChanged.RemoveListener(_OnFilterChanged);

            recycleView.OnLatestFollowStateChanged = null;
        }

        private void _OnLatestFollowStateChanged(bool isAtLatest) {
            isFollowingLatest = isAtLatest;
        }

        private void _RefreshRecycleView(bool moveToLatest) {
            if (!IsOpen) return;
            if (!recycleView.gameObject.activeInHierarchy) return;

            recycleView.SetData(filteredEntries);

            if (moveToLatest && isFollowingLatest) {
                recycleView.ScrollToLatest();
            }
        }

        private void _OnFilterChanged(bool isOn) {
            _RefreshVisibleEntries();
        }

        private void _OnHLoggerLogPublished(HLogger.LogEntry entry) {
            HLogCellData data = new(
                HLogSource.HLogger,
                entry.Level,
                entry.Timestamp,
                entry.Message,
                entry.Debug,
                entry.TargetInstanceId);

            _AddEntry(data);
            _AddPendingUnityEcho(entry.ToConsoleString());
        }

        private void _OnUnityLogReceived(string condition, string stackTrace, LogType logType) {
            if (_ConsumePendingUnityEcho(condition)) return;

            HLogCellData data = new(
                HLogSource.Unity,
                HLogCellData.ToLogLevel(logType),
                DateTimeOffset.Now,
                condition,
                stackTrace,
                null);

            _AddEntry(data);
        }

        private void _AddEntry(HLogCellData entry) {
            entries.Add(entry);
            _TrimEntries();

            if (!_PassesFilter(entry)) return;

            filteredEntries.Add(entry);
            _TrimFilteredEntries();

            _RefreshRecycleView(true);
        }

        private void _TrimEntries() {
            if (maxConsoleEntries <= 0) return;
            if (entries.Count <= maxConsoleEntries) return;

            int removeCount = entries.Count - maxConsoleEntries;
            entries.RemoveRange(0, removeCount);
        }

        private void _TrimFilteredEntries() {
            if (maxConsoleEntries <= 0) return;
            if (filteredEntries.Count <= maxConsoleEntries) return;

            int removeCount = filteredEntries.Count - maxConsoleEntries;
            filteredEntries.RemoveRange(0, removeCount);
        }

        private void _RefreshVisibleEntries() {
            filteredEntries.Clear();
            filteredEntries.AddRange(entries.Where(_PassesFilter));
            _TrimFilteredEntries();
            _RefreshRecycleView(false);
        }

        private bool _PassesFilter(HLogCellData entry) {
            if (!_PassesSourceFilter(entry.Source)) return false;
            return _PassesLevelFilter(entry.Level);
        }

        private bool _PassesSourceFilter(HLogSource source) => source switch {
            HLogSource.HLogger => showHLoggerToggle == null || showHLoggerToggle.isOn,
            HLogSource.Unity => showUnityToggle == null || showUnityToggle.isOn,
            _ => true
        };

        private bool _PassesLevelFilter(LogLevel level) => level switch {
            LogLevel.Log or
            LogLevel.Debug => showLogToggle == null || showLogToggle.isOn,
            LogLevel.Warn => showWarnToggle == null || showWarnToggle.isOn,
            LogLevel.Error or
            LogLevel.Fatal or
            LogLevel.Assert => showErrorToggle == null || showErrorToggle.isOn,
            _ => true
        };

        private void _AddPendingUnityEcho(string condition) {
            if (string.IsNullOrEmpty(condition)) return;

            if (!pendingUnityEchoCountByCondition.TryGetValue(condition, out int count)) {
                pendingUnityEchoCountByCondition[condition] = 1;
                return;
            }

            pendingUnityEchoCountByCondition[condition] = count + 1;
        }

        private bool _ConsumePendingUnityEcho(string condition) {
            if (string.IsNullOrEmpty(condition)) return false;
            if (!pendingUnityEchoCountByCondition.TryGetValue(condition, out int count)) return false;

            if (count <= 1)
                pendingUnityEchoCountByCondition.Remove(condition);
            else
                pendingUnityEchoCountByCondition[condition] = count - 1;

            return true;
        }

        private void _OnCellClicked(HLogCellData data) {
            if (data == null) return;

            GUIUtility.systemCopyBuffer = data.ClipboardText;

#if UNITY_EDITOR
            if (!data.TargetInstanceId.HasValue) return;

            UnityEngine.Object targetObject = EditorUtility.InstanceIDToObject(data.TargetInstanceId.Value);
            if (targetObject == null) return;

            Selection.activeObject = targetObject;
            EditorGUIUtility.PingObject(targetObject);
#endif
        }

        private void _UpdateFps() {
            fpsFrameCount++;
            fpsTimer += Time.unscaledDeltaTime;
            fpsAccumulatedDelta += Time.unscaledDeltaTime;
            if (fpsTimer < fpsInterval) return;

            float fps = fpsAccumulatedDelta <= 0f ? 0f : fpsFrameCount / fpsAccumulatedDelta;
            fpsText.text = $"FPS: {fps:0.0}";

            fpsTimer = 0f;
            fpsFrameCount = 0;
            fpsAccumulatedDelta = 0f;
        }

        private void _UpdateNetwork() {
            networkTimer += Time.unscaledDeltaTime;
            if (networkTimer < networkInterval) return;

            networkText.text = $"Network: {Application.internetReachability}";
            networkTimer = 0f;
        }

        private string _GetSaveFilePath() {
            string fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_HLog.txt";
#if UNITY_EDITOR
            string baseFolder = Path.IsPathRooted(editorSaveFolder)
                ? editorSaveFolder
                : Path.Combine(Application.dataPath, editorSaveFolder);
            return Path.Combine(baseFolder, fileName);
#else
            return Path.Combine(Application.persistentDataPath, fileName);
#endif
        }
        #endregion
    }
}

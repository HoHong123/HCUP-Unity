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
            if (panelRoot == null) return;
            panelRoot.SetActive(true);
        }

        public void Close() {
            if (panelRoot == null) return;
            panelRoot.SetActive(false);
        }

        public void Clear() {
            entries.Clear();
            filteredEntries.Clear();
            pendingUnityEchoCountByCondition.Clear();
            if (recycleView != null) recycleView.SetData(filteredEntries);
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
            if (panelRoot == null) return;
            panelRoot.SetActive(false);
        }

        private void _BindUi() {
            if (openButton != null) openButton.onClick.AddListener(Open);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (clearButton != null) clearButton.onClick.AddListener(Clear);
            if (saveButton != null) saveButton.onClick.AddListener(Save);

            if (showLogToggle != null) showLogToggle.onValueChanged.AddListener(_OnFilterChanged);
            if (showWarnToggle != null) showWarnToggle.onValueChanged.AddListener(_OnFilterChanged);
            if (showErrorToggle != null) showErrorToggle.onValueChanged.AddListener(_OnFilterChanged);
            if (showHLoggerToggle != null) showHLoggerToggle.onValueChanged.AddListener(_OnFilterChanged);
            if (showUnityToggle != null) showUnityToggle.onValueChanged.AddListener(_OnFilterChanged);

            if (recycleView != null) recycleView.OnCellClicked = _OnCellClicked;
        }

        private void _UnbindUi() {
            if (openButton != null) openButton.onClick.RemoveListener(Open);
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            if (clearButton != null) clearButton.onClick.RemoveListener(Clear);
            if (saveButton != null) saveButton.onClick.RemoveListener(Save);

            if (showLogToggle != null) showLogToggle.onValueChanged.RemoveListener(_OnFilterChanged);
            if (showWarnToggle != null) showWarnToggle.onValueChanged.RemoveListener(_OnFilterChanged);
            if (showErrorToggle != null) showErrorToggle.onValueChanged.RemoveListener(_OnFilterChanged);
            if (showHLoggerToggle != null) showHLoggerToggle.onValueChanged.RemoveListener(_OnFilterChanged);
            if (showUnityToggle != null) showUnityToggle.onValueChanged.RemoveListener(_OnFilterChanged);
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
            if (recycleView != null) recycleView.SetData(filteredEntries);
        }

        private void _TrimEntries() {
            if (maxConsoleEntries <= 0) return;
            if (entries.Count <= maxConsoleEntries) return;

            int removeCount = entries.Count - maxConsoleEntries;
            entries.RemoveRange(0, removeCount);
        }

        private void _RefreshVisibleEntries() {
            filteredEntries.Clear();
            filteredEntries.AddRange(entries.Where(_PassesFilter));
            if (recycleView != null) recycleView.SetData(filteredEntries);
        }

        private bool _PassesFilter(HLogCellData entry) {
            if (!_PassesSourceFilter(entry.Source)) return false;
            return _PassesLevelFilter(entry.Level);
        }

        private bool _PassesSourceFilter(HLogSource source) {
            if (source == HLogSource.HLogger) return showHLoggerToggle == null || showHLoggerToggle.isOn;
            if (source == HLogSource.Unity) return showUnityToggle == null || showUnityToggle.isOn;
            return true;
        }

        private bool _PassesLevelFilter(LogLevel level) {
            if (level == LogLevel.Log || level == LogLevel.Debug) return showLogToggle == null || showLogToggle.isOn;
            if (level == LogLevel.Warn) return showWarnToggle == null || showWarnToggle.isOn;
            if (level == LogLevel.Error || level == LogLevel.Fatal || level == LogLevel.Assert) return showErrorToggle == null || showErrorToggle.isOn;
            return true;
        }

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

            if (count <= 1) pendingUnityEchoCountByCondition.Remove(condition);
            else pendingUnityEchoCountByCondition[condition] = count - 1;
            return true;
        }

        private void _OnCellClicked(HLogCellData data) {
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
            if (fpsText != null) fpsText.text = $"FPS: {fps:0.0}";

            fpsTimer = 0f;
            fpsFrameCount = 0;
            fpsAccumulatedDelta = 0f;
        }

        private void _UpdateNetwork() {
            networkTimer += Time.unscaledDeltaTime;
            if (networkTimer < networkInterval) return;

            if (networkText != null) networkText.text = $"Network: {Application.internetReachability}";
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

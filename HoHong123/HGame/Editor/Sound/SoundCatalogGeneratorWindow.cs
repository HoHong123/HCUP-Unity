#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HUtil.Formattable;
using HGame.Sound;
using HGame.Sound.Core;

namespace HGame.Editor.Sound {
    public sealed class SoundCatalogGeneratorWindow : EditorWindow {
        #region ===== Types =====
        struct DiscoveredClip {
            public int Uid;
            public SoundMajorCategory Major;
            public string FileName;
            public string AssetPath;
        }
        #endregion

        #region ===== Serialized =====
        [SerializeField]
        DefaultAsset rootFolder;
        [SerializeField]
        DefaultAsset outputFolder;
        [SerializeField]
        string outputFileName = "SoundCatalog";
        [SerializeField]
        SoundCatalogPolicySO policy;
        [SerializeField]
        bool validateUidByPolicy = true;
        [SerializeField]
        List<AudioClip> extraClips = new();
        #endregion

        #region ===== Runtime =====
        readonly List<DiscoveredClip> discovered = new();
        readonly List<string> logs = new();

        Vector2 windowScroll;

        Vector2 discoveredTableScroll;
        Vector2 discoveredTextScroll;
        Vector2 logsTextScroll;

        bool showDiscoveredTextView = true;
        bool showLogsTextView = true;

        string discoveredFind = "";
        string logsFind = "";

        string discoveredTextCache = "";
        string logsTextCache = "";

        bool discoveredDirty = true; // discovered가 오래되었는지 확인
        bool logsDirty = true; // log가 오래되었는지 확인
        #endregion

        #region ===== Menu =====
        [MenuItem("HCUP/Audio/Sound Catalog Generator")]
        private static void Open() {
            GetWindow<SoundCatalogGeneratorWindow>("Sound Catalog Generator");
        }
        #endregion

        #region ===== Unity =====
        private void OnGUI() {
            using (var sv = new EditorGUILayout.ScrollViewScope(windowScroll)) {
                windowScroll = sv.scrollPosition;

                _DrawHeader();
                _DrawSettings();
                _DrawExtras();
                _DrawActions();

                _DrawDiscoveredTable();
                _DrawDiscoveredTextView();
                _DrawLogsTextView();
            }
        }
        #endregion

        #region ===== Draw =====
        private void _DrawHeader() {
            EditorGUILayout.LabelField("Sound Catalog Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space(6);
        }

        private void _DrawSettings() {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
                rootFolder = (DefaultAsset)EditorGUILayout.ObjectField("Root Folder", rootFolder, typeof(DefaultAsset), false);

                outputFolder = (DefaultAsset)EditorGUILayout.ObjectField("Output Folder", outputFolder, typeof(DefaultAsset), false);
                outputFileName = EditorGUILayout.TextField("File Name", outputFileName);

                if (validateUidByPolicy) policy = (SoundCatalogPolicySO)EditorGUILayout.ObjectField("Policy", policy, typeof(SoundCatalogPolicySO), false);
                validateUidByPolicy = EditorGUILayout.Toggle("Validate UID By Policy", validateUidByPolicy);
            }
        }

        private void _DrawExtras() {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Extra Clips", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
                using (new EditorGUILayout.HorizontalScope()) {
                    if (GUILayout.Button("+ Add", GUILayout.Width(110)))
                        extraClips.Add(null);
                    if (GUILayout.Button("Clear", GUILayout.Width(80)))
                        extraClips.Clear();
                }

                int removeIndex = -1;
                for (int k = 0; k < extraClips.Count; k++) {
                    using (new EditorGUILayout.HorizontalScope()) {
                        extraClips[k] = (AudioClip)EditorGUILayout.ObjectField(
                            extraClips[k], typeof(AudioClip), false);

                        if (GUILayout.Button("-", GUILayout.Width(22)))
                            removeIndex = k;
                    }
                }

                if (removeIndex >= 0) extraClips.RemoveAt(removeIndex);
            }
        }

        private void _DrawActions() {
            EditorGUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope()) {
                if (GUILayout.Button("Scan", GUILayout.Height(28))) _Scan();

                bool canGenerate =
                    (rootFolder != null || _HasValidExtraClips()) &&
                    outputFolder != null &&
                    !string.IsNullOrWhiteSpace(outputFileName);
                GUI.enabled = canGenerate;
                if (GUILayout.Button("Generate", GUILayout.Height(28))) _Generate();
                GUI.enabled = true;
            }
        }
        #endregion

        #region ===== Scan / Generate =====
        private void _Scan() {
            discovered.Clear();
            logs.Clear();

            if (!rootFolder) {
                logs.Add("[Error] RootFolder is null.");
                _MarkAllDirty();
                return;
            }

            string rootPath = AssetDatabase.GetAssetPath(rootFolder);
            logs.Add($"[Scan] Start :: root={rootPath}");

            var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { rootPath });

            foreach (var guid in guids) {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid).Replace("\\", "/");
                string fileName = Path.GetFileName(assetPath);

                if (!_TryParseUid(Path.GetFileNameWithoutExtension(fileName), out int uid)) {
                    logs.Add($"[Warn] Invalid filename :: {fileName}");
                    continue;
                }

                var major = _InferMajor(assetPath);

                if (validateUidByPolicy && !_IsUidValidByPolicy(major, uid)) {
                    logs.Add($"[Warn] UID out of policy :: {fileName} ({uid})");
                    continue;
                }

                discovered.Add(new DiscoveredClip {
                    Uid = uid,
                    Major = major,
                    FileName = fileName,
                    AssetPath = assetPath
                });
            }

            logs.Add($"[Scan] Done :: discovered={discovered.Count}");
            _MarkAllDirty();
        }

        private void _Generate() {
            _BuildDiscoveredForGenerate();

            if (discovered.Count == 0) {
                logs.Add("[Generate] Failed :: No valid clips (scan=0, extras=0).");
                _MarkAllDirty();
                return;
            }

            if (!outputFolder) {
                logs.Add("[Generate] Failed :: OutputFolder is null");
                _MarkAllDirty();
                return;
            }

            string folderPath = AssetDatabase.GetAssetPath(outputFolder);
            if (!AssetDatabase.IsValidFolder(folderPath)) {
                logs.Add($"[Generate] Failed :: Invalid folder {folderPath}");
                _MarkAllDirty();
                return;
            }

            string assetPath = $"{folderPath}/{outputFileName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<SoundCatalogSO>(assetPath);
            SoundCatalogSO catalog;

            if (existing) {
                catalog = existing;
                logs.Add($"[Generate] Update :: {assetPath}");
                Undo.RecordObject(catalog, "Update SoundCatalog");
            }
            else {
                catalog = CreateInstance<SoundCatalogSO>();
                AssetDatabase.CreateAsset(catalog, assetPath);
                logs.Add($"[Generate] Create :: {assetPath}");
            }

            catalog.EditorClearEntries();

            foreach (var discover in discovered) {
                var key = new SoundKey(discover.Major, discover.Uid);
                string token = _ToResourcesToken(discover.AssetPath);
                if (string.IsNullOrWhiteSpace(token)) {
                    logs.Add($"[Warn] Not under Resources. Skip :: {discover.AssetPath}");
                    continue;
                }
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(discover.AssetPath);
                catalog.EditorAddEntry(key, token, clip);
            }

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            logs.Add($"[Generate] Done :: entries={discovered.Count}");
            _MarkAllDirty();
        }

        private void _MergeExtras() {
            foreach (var clip in extraClips) {
                if (!clip) continue;

                string assetPath = AssetDatabase.GetAssetPath(clip).Replace("\\", "/");
                if (discovered.Any(d => d.AssetPath == assetPath)) continue;

                string fileName = Path.GetFileName(assetPath);

                if (!_TryParseUid(Path.GetFileNameWithoutExtension(fileName), out int uid)) {
                    logs.Add($"[Warn] Extra invalid filename :: {fileName}");
                    continue;
                }

                var major = _InferMajor(assetPath);

                if (validateUidByPolicy && !_IsUidValidByPolicy(major, uid)) {
                    logs.Add($"[Warn] Extra UID out of policy :: {fileName} ({uid})");
                    continue;
                }

                discovered.Add(new DiscoveredClip {
                    Uid = uid,
                    Major = major,
                    FileName = fileName,
                    AssetPath = assetPath
                });
            }
        }

        private void _BuildDiscoveredForGenerate() {
            discovered.Clear();
            logs.Add("[Generate] Build discovered...");

            if (rootFolder) {
                string rootPath = AssetDatabase.GetAssetPath(rootFolder);
                var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { rootPath });

                for (int k = 0; k < guids.Length; k++) {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[k]).Replace("\\", "/");
                    string fileName = Path.GetFileName(assetPath);

                    if (!_TryParseUid(Path.GetFileNameWithoutExtension(fileName), out int uid)) {
                        logs.Add($"[Warn] Invalid filename :: {fileName}");
                        continue;
                    }

                    var major = _InferMajor(assetPath);

                    if (validateUidByPolicy && !_IsUidValidByPolicy(major, uid)) {
                        logs.Add($"[Warn] UID out of policy :: {fileName} ({uid})");
                        continue;
                    }

                    discovered.Add(new DiscoveredClip {
                        Uid = uid,
                        Major = major,
                        FileName = fileName,
                        AssetPath = assetPath
                    });
                }
            }
            else {
                logs.Add("[Generate] RootFolder is null. Scan skipped (Extras only).");
            }

            // 2) 항상 Extras 포함
            _MergeExtras();

            logs.Add($"[Generate] Build done :: discovered={discovered.Count}");
        }
        #endregion

        #region ===== Discovered Table =====
        private void _Header(string text, float width) => EditorGUILayout.LabelField(text, EditorStyles.boldLabel, GUILayout.Width(width));

        private void _DrawDiscoveredTable() {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Discovered (Table)", EditorStyles.boldLabel);

            using (var sv = new EditorGUILayout.ScrollViewScope(discoveredTableScroll, GUILayout.Height(240))) {
                discoveredTableScroll = sv.scrollPosition;

                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox)) {
                    _Header("UID", 80);
                    _Header("Major", 80);
                    _Header("FileName", 220);
                    _Header("AssetPath", 420);
                }

                foreach (var d in discovered) {
                    using (new EditorGUILayout.HorizontalScope()) {
                        EditorGUILayout.LabelField(d.Uid.ToString(), GUILayout.Width(80));
                        EditorGUILayout.LabelField(d.Major.ToString(), GUILayout.Width(80));
                        EditorGUILayout.LabelField(d.FileName, GUILayout.Width(220));
                        EditorGUILayout.LabelField(d.AssetPath, GUILayout.Width(420));
                    }
                }
            }
        }
        #endregion

        #region ===== Text Views =====
        private void _DrawDiscoveredTextView() {
            EditorGUILayout.Space(6);
            showDiscoveredTextView = EditorGUILayout.ToggleLeft("Discovered (TSV / Excel Copy)", showDiscoveredTextView);

            if (!showDiscoveredTextView)return;

            _RebuildDiscoveredTextIfNeeded();

            using (new EditorGUILayout.HorizontalScope()) {
                EditorGUILayout.LabelField("Find", GUILayout.Width(32));
                discoveredFind = EditorGUILayout.TextField(discoveredFind);

                if (GUILayout.Button("Copy All", GUILayout.Width(90)))
                    EditorGUIUtility.systemCopyBuffer = discoveredTextCache;

                if (GUILayout.Button("Copy Filtered", GUILayout.Width(110)))
                    EditorGUIUtility.systemCopyBuffer = discoveredTextCache.FilterText(discoveredFind);
            }

            using (var sv = new EditorGUILayout.ScrollViewScope(
                discoveredTextScroll, GUILayout.Height(180))) {
                discoveredTextScroll = sv.scrollPosition;
                EditorGUILayout.TextArea(discoveredTextCache.FilterText(discoveredFind), GUILayout.ExpandHeight(true));
            }
        }

        private void _DrawLogsTextView() {
            EditorGUILayout.Space(6);

            showLogsTextView = EditorGUILayout.ToggleLeft("Logs", showLogsTextView);
            if (!showLogsTextView) return;

            _RebuildLogsTextIfNeeded();

            using (new EditorGUILayout.HorizontalScope()) {
                EditorGUILayout.LabelField("Find", GUILayout.Width(32));
                logsFind = EditorGUILayout.TextField(logsFind);

                if (GUILayout.Button("Copy All", GUILayout.Width(90)))
                    EditorGUIUtility.systemCopyBuffer = logsTextCache;

                if (GUILayout.Button("Copy Filtered", GUILayout.Width(110)))
                    EditorGUIUtility.systemCopyBuffer = logsTextCache.FilterText(logsFind);
            }

            using (var sv = new EditorGUILayout.ScrollViewScope(
                logsTextScroll, GUILayout.Height(160))) {
                logsTextScroll = sv.scrollPosition;
                EditorGUILayout.TextArea(logsTextCache.FilterText(logsFind), GUILayout.ExpandHeight(true));
            }
        }
        #endregion

        #region ===== Helpers =====
        private bool _HasValidExtraClips() {
            if (extraClips == null || extraClips.Count < 1) return false;
            for (int k = 0; k < extraClips.Count; k++) {
                if (extraClips[k]) return true;
            }
            return false;
        }

        private string _ToResourcesToken(string assetPath) {
            if (string.IsNullOrWhiteSpace(assetPath)) return string.Empty;

            assetPath = assetPath.Replace("\\", "/");

            const string resourcesRoot = "Assets/Resources/";
            if (!assetPath.StartsWith(resourcesRoot, StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            string relative = assetPath.Substring(resourcesRoot.Length);
            return Path.ChangeExtension(relative, null);
        }

        private void _RebuildDiscoveredTextIfNeeded() {
            if (!discoveredDirty) return;

            var sb = new StringBuilder(4096);
            sb.AppendLine("UID\tMajor\tFileName\tAssetPath");

            foreach (var d in discovered) {
                sb.Append(d.Uid).Append('\t');
                sb.Append(d.Major).Append('\t');
                sb.Append(d.FileName).Append('\t');
                sb.Append(d.AssetPath).AppendLine();
            }

            discoveredTextCache = sb.ToString();
            discoveredDirty = false;
        }

        private void _RebuildLogsTextIfNeeded() {
            if (!logsDirty) return;

            var sb = new StringBuilder(2048);
            foreach (var k in logs) sb.AppendLine(k);

            logsTextCache = sb.ToString();
            logsDirty = false;
        }

        private bool _TryParseUid(string name, out int uid) {
            uid = -1;
            int idx = name.IndexOf('_');
            return idx > 0 && int.TryParse(name.Substring(0, idx), out uid);
        }

        private SoundMajorCategory _InferMajor(string path) {
            path = path.ToLowerInvariant();
            if (path.Contains("/ui/")) return SoundMajorCategory.UI;
            if (path.Contains("/bgm/")) return SoundMajorCategory.BGM;
            if (path.Contains("/voice/")) return SoundMajorCategory.Voice;
            return SoundMajorCategory.SFX;
        }

        private bool _IsUidValidByPolicy(SoundMajorCategory major, int uid) {
            if (!policy) return true;
            if (!policy.TryGetUidRange(major, out var range)) return true;
            return range.Contains(uid);
        }

        private void _MarkAllDirty() {
            discoveredDirty = true;
            logsDirty = true;
        }
        #endregion
    }
}
#endif

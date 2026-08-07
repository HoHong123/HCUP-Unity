#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using HDiagnosis.Logger;
using HAudio.Core;

namespace HAudio.Editor {
    [Serializable]
    public sealed class AudioCatalogEditorPanel {
        #region Nested Class
        public sealed class Row {
            public int Index;
            public AudioMajorCategory Major;
            public AudioClip Clip;
            public AudioCatalogSO Catalog;
            public string Token;
            public string Path;
        }
        #endregion

        #region Fields
        [SerializeField]
        bool catalogsFoldout = true;

        [SerializeField]
        List<AudioCatalogSO> catalogs = new();

        [SerializeField]
        bool showOnlyMissingClip;

        [SerializeField]
        bool showOnlyMissingToken;

        [SerializeField]
        bool showOnlyMissingPath;

        [SerializeField]
        bool groupByCatalog = true;

        Vector2 scroll;

        List<Row> rows = new();
        bool dirtyRows = true;

        string searchText = "";
        bool searchByName = true;
        bool searchByToken = true;
        bool searchByPath = true;
        bool searchByClip = true;

        Dictionary<AudioCatalogSO, bool> catalogFoldouts = new();
        #endregion

        #region Host
        // 이 패널은 SoundToolsWindow 의 탭으로 그려진다. EditorWindow 를 상속하지 않으므로
        // Repaint 같은 창 기능은 호스트를 통해 부른다. [NonSerialized] 인 이유는 창 참조를
        // 직렬화하면 도메인 리로드 후 죽은 참조가 남기 때문 — 매 Draw 에서 다시 받는다.
        [NonSerialized]
        EditorWindow host;

        private void _Repaint() {
            if (host != null) host.Repaint();
        }
        #endregion

        #region Panel Life Cycle
        public void OnEnable() {
            dirtyRows = true;
        }
        #endregion

        #region IMGUI
        public void Draw(EditorWindow owner) {
            host = owner;

            _DrawSearchBar();
            _DrawCatalogList();
            _DrawToolbar();
            EditorGUILayout.Space(6);

            _RebuildRowsIfNeeded();

            using (var scope = new EditorGUILayout.ScrollViewScope(scroll)) {
                scroll = scope.scrollPosition;
                _DrawRows();
            }
        }

        private void _DrawToolbar() {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
                using (new EditorGUILayout.HorizontalScope()) {
                    if (GUILayout.Button("Update All Catalogs (Token/Path From Clips)", GUILayout.Height(24))) {
                        _UpdateAllCatalogsTokenAndPathFromClips();
                        dirtyRows = true;
                    }

                    if (GUILayout.Button("Resolve Clips From Token/Path (All)", GUILayout.Height(24))) {
                        _ResolveAllCatalogsClipsFromTokenAndPath();
                        dirtyRows = true;
                    }
                }

                using (new EditorGUILayout.HorizontalScope()) {
                    bool nextMissingClip = EditorGUILayout.ToggleLeft("Only Missing Clip", showOnlyMissingClip, GUILayout.Width(160));
                    if (nextMissingClip != showOnlyMissingClip) {
                        showOnlyMissingClip = nextMissingClip;
                        dirtyRows = true;
                    }

                    bool nextMissingToken = EditorGUILayout.ToggleLeft("Only Missing Token", showOnlyMissingToken, GUILayout.Width(170));
                    if (nextMissingToken != showOnlyMissingToken) {
                        showOnlyMissingToken = nextMissingToken;
                        dirtyRows = true;
                    }

                    bool nextMissingPath = EditorGUILayout.ToggleLeft("Only Missing Path", showOnlyMissingPath, GUILayout.Width(160));
                    if (nextMissingPath != showOnlyMissingPath) {
                        showOnlyMissingPath = nextMissingPath;
                        dirtyRows = true;
                    }

                    bool nextGroup = EditorGUILayout.ToggleLeft("Group By Catalog", groupByCatalog, GUILayout.Width(160));
                    if (nextGroup != groupByCatalog)
                        groupByCatalog = nextGroup;

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Refresh", GUILayout.Width(100))) {
                        dirtyRows = true;
                        _Repaint();
                    }
                }
            }
        }

        private void _DrawCatalogDropArea() {
            GUILayout.Space(6);

            Rect dropArea = GUILayoutUtility.GetRect(0f, 48f, GUILayout.ExpandWidth(true));

            GUI.Box(dropArea, "Drag & Drop AudioCatalogSO here (Multiple supported)", EditorStyles.helpBox);

            Event evt = Event.current;
            if (!dropArea.Contains(evt.mousePosition)) return;

            switch (evt.type) {
            case EventType.DragUpdated:
            case EventType.DragPerform: {
                    bool hasValid = false;

                    foreach (var obj in DragAndDrop.objectReferences) {
                        if (obj is AudioCatalogSO) {
                            hasValid = true;
                            break;
                        }
                    }

                    if (!hasValid)
                        return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform) {
                        DragAndDrop.AcceptDrag();

                        bool added = false;

                        foreach (var obj in DragAndDrop.objectReferences) {
                            if (obj is not AudioCatalogSO catalog) continue;
                            if (catalogs.Contains(catalog)) continue;

                            catalogs.Add(catalog);
                            added = true;
                        }

                        if (added) {
                            dirtyRows = true;
                            // 패널은 UnityEngine.Object 가 아니다. 직렬화 상태를 들고 있는
                            // 것은 호스트 창이므로 더티 표시도 그쪽에 건다.
                            if (host != null) EditorUtility.SetDirty(host);
                        }
                    }

                    evt.Use();
                    break;
                }
            }
        }

        private void _DrawSearchBar() {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
                using (new EditorGUILayout.HorizontalScope()) {
                    EditorGUILayout.LabelField("Search", GUILayout.Width(50));

                    string nextText = EditorGUILayout.TextField(searchText);
                    if (!string.Equals(nextText, searchText, StringComparison.Ordinal)) {
                        searchText = nextText;
                        dirtyRows = true;
                    }

                    if (GUILayout.Button("Clear", GUILayout.Width(60))) {
                        searchText = string.Empty;
                        dirtyRows = true;
                        GUI.FocusControl(null);
                    }
                }

                using (new EditorGUILayout.HorizontalScope()) {
                    bool nextName = EditorGUILayout.ToggleLeft("Name", searchByName, GUILayout.Width(70));
                    if (nextName != searchByName) { searchByName = nextName; dirtyRows = true; }

                    bool nextToken = EditorGUILayout.ToggleLeft("Token", searchByToken, GUILayout.Width(70));
                    if (nextToken != searchByToken) { searchByToken = nextToken; dirtyRows = true; }

                    bool nextPath = EditorGUILayout.ToggleLeft("Path", searchByPath, GUILayout.Width(70));
                    if (nextPath != searchByPath) { searchByPath = nextPath; dirtyRows = true; }

                    bool nextClip = EditorGUILayout.ToggleLeft("Clip", searchByClip, GUILayout.Width(70));
                    if (nextClip != searchByClip) { searchByClip = nextClip; dirtyRows = true; }
                }
            }
        }

        private void _DrawCatalogList() {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
                catalogsFoldout = EditorGUILayout.Foldout(catalogsFoldout, $"Catalogs ({catalogs.Count})", true);
                if (!catalogsFoldout) return;

                _DrawCatalogDropArea();

                int removeIndex = -1;

                for (int k = 0; k < catalogs.Count; k++) {
                    using (new EditorGUILayout.HorizontalScope()) {
                        var next = (AudioCatalogSO)EditorGUILayout.ObjectField(catalogs[k], typeof(AudioCatalogSO), false);

                        if (next != catalogs[k]) {
                            catalogs[k] = next;
                            dirtyRows = true;
                        }

                        if (GUILayout.Button("Ping", GUILayout.Width(60))) {
                            if (catalogs[k]) EditorGUIUtility.PingObject(catalogs[k]);
                        }

                        if (GUILayout.Button("-", GUILayout.Width(22)))
                            removeIndex = k;
                    }
                }

                if (removeIndex >= 0) {
                    catalogs.RemoveAt(removeIndex);
                    dirtyRows = true;
                    _Repaint();
                }

                using (new EditorGUILayout.HorizontalScope()) {
                    if (GUILayout.Button("+ Add", GUILayout.Width(120))) {
                        catalogs.Add(null);
                        _Repaint();
                    }

                    if (GUILayout.Button("Clear", GUILayout.Width(120))) {
                        catalogs.Clear();
                        dirtyRows = true;
                        _Repaint();
                    }

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Remove Null", GUILayout.Width(120))) {
                        catalogs.RemoveAll(x => !x);
                        dirtyRows = true;
                        _Repaint();
                    }
                }
            }
        }

        private void _DrawRows() {
            if (rows.Count == 0) {
                EditorGUILayout.HelpBox("No entries. Add catalogs or adjust filters.", MessageType.Info);
                return;
            }

            AudioCatalogSO currentCatalog = null;
            bool currentFoldout = true;

            for (int k = 0; k < rows.Count; k++) {
                var row = rows[k];
                if (!row.Catalog) continue;

                if (groupByCatalog) {
                    if (currentCatalog != row.Catalog) {
                        currentCatalog = row.Catalog;

                        EditorGUILayout.Space(8);

                        bool opened = _GetCatalogFoldout(currentCatalog);
                        bool nextOpened = EditorGUILayout.Foldout(opened, currentCatalog.name, true);

                        if (nextOpened != opened) _SetCatalogFoldout(currentCatalog, nextOpened);

                        currentFoldout = nextOpened;
                    }

                    if (!currentFoldout) continue;
                }

                _DrawRow(row);
            }
        }

        private void _DrawRow(Row row) {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
                using (new EditorGUILayout.HorizontalScope()) {
                    EditorGUILayout.LabelField($"[{row.Major}]", GUILayout.Width(140));

                    if (!groupByCatalog) EditorGUILayout.LabelField(row.Catalog.name, GUILayout.Width(220));

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Ping Catalog", GUILayout.Width(100))) {
                        EditorGUIUtility.PingObject(row.Catalog);
                    }
                }

                using (new EditorGUILayout.HorizontalScope()) {
                    var nextClip = (AudioClip)EditorGUILayout.ObjectField(row.Clip, typeof(AudioClip), false);
                    if (nextClip != row.Clip) {
                        _SetEditorClip(row.Catalog, row.Index, nextClip);
                        row.Clip = nextClip;
                    }

                    if (GUILayout.Button("▶", GUILayout.Width(28))) {
                        _TryPreviewClip(row.Clip);
                    }

                    if (GUILayout.Button("■", GUILayout.Width(28))) {
                        _TryStopPreview();
                    }
                }

                using (new EditorGUILayout.HorizontalScope()) {
                    EditorGUILayout.LabelField("Token", GUILayout.Width(42));
                    EditorGUILayout.SelectableLabel(row.Token ?? "", EditorStyles.textField, GUILayout.Height(18));

                    if (GUILayout.Button("Update", GUILayout.Width(80))) {
                        _UpdateSingleTokenAndPathFromClip(row);
                        dirtyRows = true;
                    }

                    if (GUILayout.Button("Resolve", GUILayout.Width(80))) {
                        _ResolveSingleClipFromTokenAndPath(row);
                        dirtyRows = true;
                    }
                }

                using (new EditorGUILayout.HorizontalScope()) {
                    EditorGUILayout.LabelField("Path", GUILayout.Width(42));
                    EditorGUILayout.SelectableLabel(row.Path ?? "", EditorStyles.textField, GUILayout.Height(18));
                }

                using (new EditorGUILayout.HorizontalScope()) {
                    EditorGUILayout.LabelField("ResKey", GUILayout.Width(42));
                    EditorGUILayout.SelectableLabel(
                        AudioCatalogSO.BuildResourcesLoadKey(row.Path, row.Token),
                        EditorStyles.textField,
                        GUILayout.Height(18));
                }
            }
        }
        #endregion

        #region Editor Operations
        private void _RebuildRowsIfNeeded() {
            if (!dirtyRows) return;
            rows.Clear();

            for (int k = 0; k < catalogs.Count; k++) {
                var catalog = catalogs[k];
                if (!catalog) continue;

                var entries = catalog.Entries;
                if (entries == null) continue;

                for (int j = 0; j < entries.Count; j++) {
                    var e = entries[j];
                    if (e == null) continue;

                    var row = new Row {
                        Catalog = catalog,
                        Index = j,
                        Major = e.Major,
                        Token = e.Token,
                        Path = e.Path,
                        Clip = _TryGetEditorClip(e)
                    };

                    if (!_PassFilter(row)) continue;
                    rows.Add(row);
                }
            }

            rows = rows
                .OrderBy(row => row.Catalog ? row.Catalog.name : "")
                .ThenBy(row => (int)row.Major)
                .ThenBy(row => row.Token, StringComparer.Ordinal)
                .ToList();

            dirtyRows = false;
        }

        private bool _PassFilter(Row row) {
            if (showOnlyMissingClip && row.Clip) return false;
            if (showOnlyMissingToken && !string.IsNullOrWhiteSpace(row.Token)) return false;
            if (showOnlyMissingPath && !string.IsNullOrWhiteSpace(row.Path)) return false;
            if (string.IsNullOrWhiteSpace(searchText)) return true;
            return _MatchQuery(row, searchText.Trim());
        }

        private bool _MatchQuery(Row row, string query) {
            if (string.IsNullOrWhiteSpace(query)) return false;
            if (row.Catalog && row.Catalog.name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (row.Major.ToString().IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (searchByName && row.Clip && row.Clip.name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (searchByToken && !string.IsNullOrWhiteSpace(row.Token) &&
                row.Token.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (searchByPath && !string.IsNullOrWhiteSpace(row.Path) &&
                row.Path.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (searchByClip && row.Clip &&
                row.Clip.name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            return false;
        }

        private void _UpdateAllCatalogsTokenAndPathFromClips() {
            catalogs.RemoveAll(x => !x);

            foreach (var catalog in catalogs) {
                if (!catalog) continue;
                catalog.EditorUpdateTokenAndPathFromClips();
                EditorUtility.SetDirty(catalog);
            }

            AssetDatabase.SaveAssets();
            _Repaint();
        }

        private void _ResolveAllCatalogsClipsFromTokenAndPath() {
            catalogs.RemoveAll(x => !x);

            foreach (var catalog in catalogs) {
                if (!catalog) continue;
                catalog.EditorResolveClipsFromTokenAndPath();
                EditorUtility.SetDirty(catalog);
            }

            AssetDatabase.SaveAssets();
            _Repaint();
        }

        private void _UpdateSingleTokenAndPathFromClip(Row row) {
            if (!row.Catalog || !row.Clip) return;
            row.Catalog.EditorUpdateTokenAndPathFromClips();
            EditorUtility.SetDirty(row.Catalog);
            AssetDatabase.SaveAssets();
        }

        private void _ResolveSingleClipFromTokenAndPath(Row row) {
            if (!row.Catalog) return;
            row.Catalog.EditorResolveClipsFromTokenAndPath();
            EditorUtility.SetDirty(row.Catalog);
            AssetDatabase.SaveAssets();
        }

        private AudioClip _TryGetEditorClip(AudioCatalogSO.Entry entry) {
            try {
                var prop = entry.GetType().GetProperty("EditorClip");
                if (prop != null) return prop.GetValue(entry) as AudioClip;
            }
            catch (System.Reflection.TargetInvocationException) {
                // 선택적 EditorClip 프로퍼티 탐침 — getter 예외만 무시하고 그 외 예외는 드러낸다.
            }

            return null;
        }

        private void _SetEditorClip(AudioCatalogSO catalog, int index, AudioClip clip) {
            Assert.IsNotNull(catalog);
            if (!catalog) return;

            var so = new SerializedObject(catalog);
            var entriesProp = so.FindProperty("entries");
            if (entriesProp == null || !entriesProp.isArray) return;
            if (index < 0 || index >= entriesProp.arraySize) return;

            var entryProp = entriesProp.GetArrayElementAtIndex(index);
            var clipProp = entryProp.FindPropertyRelative("editorClip");
            if (clipProp == null) return;

            clipProp.objectReferenceValue = clip;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        private void _TryPreviewClip(AudioClip clip) {
            if (!clip) return;
            if (!EditorAudioPreview.CanUse) {
                HLogger.Warning("[AudioCatalogEditorPanel] EditorAudioPreview is not available on this Unity version.");
                return;
            }
            EditorAudioPreview.Play(clip, loop: false, single: true);
        }

        private void _TryStopPreview() {
            if (!EditorAudioPreview.CanUse) return;
            EditorAudioPreview.StopAll();
        }

        private bool _GetCatalogFoldout(AudioCatalogSO catalog) {
            if (!catalog) return true;

            if (!catalogFoldouts.TryGetValue(catalog, out bool opened)) {
                opened = true;
                catalogFoldouts[catalog] = opened;
            }

            return opened;
        }

        private void _SetCatalogFoldout(AudioCatalogSO catalog, bool opened) {
            if (!catalog) return;
            catalogFoldouts[catalog] = opened;
        }
        #endregion
    }
}
#endif
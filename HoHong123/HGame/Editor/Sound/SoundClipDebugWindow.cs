#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HGame.Sound.Load;

namespace HGame.Sound.Editor {
    public sealed class SoundClipDiagnosticsWindow : EditorWindow {
        Vector2 scroll;

        string search = string.Empty;
        bool loadedOnly = true;
        bool depPositiveOnly;

        bool autoRefresh;
        double refreshIntervalSec = 0.5;
        double nextRefreshTime;

        IAudioClipDiagnostics diagnostics;
        AudioClipProviderSnapshot snapshot;

        readonly Dictionary<string, bool> catalogFold = new();
        string selectedCatalog;

        [MenuItem("HCUP/Audio/Data Diagnostics")]
        public static void Open() {
            var window = GetWindow<SoundClipDiagnosticsWindow>();
            window.titleContent = new GUIContent("Data Diagnostics");
            window.Show();
        }

        private void OnEnable() {
            EditorApplication.playModeStateChanged += _OnPlayModeChanged;
            _ResetState();
        }

        private void OnDisable() {
            EditorApplication.playModeStateChanged -= _OnPlayModeChanged;
            _ResetState();
        }

        private void _OnPlayModeChanged(PlayModeStateChange state) {
            // 플레이 모드 전환 시점마다 정리
            _ResetState();
            Repaint();
        }

        private void _ResetState() {
            diagnostics = null;
            snapshot = null;

            selectedCatalog = null;
            catalogFold.Clear();

            scroll = Vector2.zero;

            // UI 상태도 요구대로 초기화 (원하면 유지해도 됨)
            search = string.Empty;
            loadedOnly = true;
            depPositiveOnly = false;

            autoRefresh = false;

            nextRefreshTime = EditorApplication.timeSinceStartup + refreshIntervalSec;
        }

        private void OnGUI() {
            // PlayMode가 아니라면 UI 자체를 최소화하고, 데이터도 유지하지 않는다.
            if (!EditorApplication.isPlaying) {
                EditorGUILayout.HelpBox("Play Mode에서만 표시됩니다.", MessageType.Info);

                //using (new EditorGUILayout.HorizontalScope()) {
                //    if (GUILayout.Button("Clear", GUILayout.Width(80))) _ResetState();
                //}
                return;
            }

            // PlayMode일 때만 툴바를 그린다.
            _DrawToolbar();

            _EnsureDiagnostics();

            if (diagnostics == null) {
                EditorGUILayout.HelpBox(
                    "Diagnostics를 찾을 수 없습니다.\n" +
                    "- SoundManager 존재 여부\n" +
                    "- Provider가 IAudioClipDiagnostics 구현 여부\n" +
                    "- SoundManager.TryGetClipDiagnostics() 확인",
                    MessageType.Warning);
                return;
            }

            _AutoRefreshIfNeeded();

            if (snapshot == null) {
                EditorGUILayout.HelpBox("Snapshot이 없습니다. Refresh를 눌러 갱신하세요.", MessageType.Info);
                return;
            }

            _DrawSnapshot(snapshot);
        }

        private void _DrawToolbar() {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar)) {
                if (GUILayout.Button("Bind", EditorStyles.toolbarButton, GUILayout.Width(50))) {
                    diagnostics = null;
                    _EnsureDiagnostics(force: true);
                }

                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
                    _RefreshSnapshot();

                if (GUILayout.Button("Prune", EditorStyles.toolbarButton, GUILayout.Width(55)))
                    _Prune();

                GUILayout.Space(10);

                loadedOnly = GUILayout.Toggle(loadedOnly, "Loaded", EditorStyles.toolbarButton);
                depPositiveOnly = GUILayout.Toggle(depPositiveOnly, "Dep>0", EditorStyles.toolbarButton);

                GUILayout.Space(10);

                autoRefresh = GUILayout.Toggle(autoRefresh, "Auto", EditorStyles.toolbarButton);
                using (new EditorGUI.DisabledScope(!autoRefresh)) {
                    GUILayout.Label("Interval", GUILayout.Width(50));
                    refreshIntervalSec = EditorGUILayout.DoubleField(refreshIntervalSec, GUILayout.Width(60));
                    if (refreshIntervalSec < 0.1) refreshIntervalSec = 0.1;
                }

                GUILayout.FlexibleSpace();

                _DrawSearchField();
            }
        }

        private void _DrawSearchField() {
            // Unity 버전에 따라 toolbar search style이 null이거나 내부 TextEditor NRE가 나는 경우가 있음.
            try {
                // 2021+에서 주로 안전
                search = EditorGUILayout.TextField(
                    search,
                    EditorStyles.toolbarSearchField,
                    GUILayout.MinWidth(160));

                if (GUILayout.Button("X", EditorStyles.toolbarButton, GUILayout.Width(25))) {
                    search = string.Empty;
                    GUI.FocusControl(null);
                }
            }
            catch (Exception) {
                search = EditorGUILayout.TextField(search, GUILayout.MinWidth(160));
                if (GUILayout.Button("X", EditorStyles.toolbarButton, GUILayout.Width(25)))
                    search = string.Empty;
            }
        }

        private void _EnsureDiagnostics(bool force = false) {
            if (!force && diagnostics != null) return;

            var sound = FindFirstObjectByType<SoundManager>();
            if (!sound) {
                diagnostics = null;
                return;
            }

            diagnostics = sound.TryGetClipDiagnostics(out var diag) ? diag : null;
        }

        private void _AutoRefreshIfNeeded() {
            if (!autoRefresh) return;

            var now = EditorApplication.timeSinceStartup;
            if (now < nextRefreshTime) return;

            nextRefreshTime = now + refreshIntervalSec;
            _RefreshSnapshot();
        }

        private void _RefreshSnapshot() {
            if (diagnostics == null) return;
            snapshot = diagnostics.CreateSnapshot();
            Repaint();
        }

        private void _Prune() {
            if (diagnostics == null) return;

            int removed = diagnostics.PruneUnusedTokens();
            if (removed > 0) _RefreshSnapshot();

            ShowNotification(new GUIContent($"Pruned: {removed}"));
        }

        private void _DrawSnapshot(AudioClipProviderSnapshot snap) {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
                EditorGUILayout.LabelField($"Provider: {snap.ProviderName}");
                EditorGUILayout.LabelField($"Tokens: {snap.TokenCount}");
                EditorGUILayout.LabelField($"Loaded: {snap.LoadedCount}");
                EditorGUILayout.LabelField($"Entries: {snap.EntryCount}");
                if (!string.IsNullOrEmpty(selectedCatalog))
                    EditorGUILayout.LabelField($"Selected Catalog: {selectedCatalog}");
            }

            EditorGUILayout.Space(6);
            _DrawCatalogs(snap);

            EditorGUILayout.Space(6);
            _DrawEntries(snap);
        }

        private void _DrawCatalogs(AudioClipProviderSnapshot snap) {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
                EditorGUILayout.LabelField("Catalogs", EditorStyles.boldLabel);

                if (snap.Catalogs == null || snap.Catalogs.Count == 0) {
                    EditorGUILayout.LabelField("(No catalogs)");
                    return;
                }

                foreach (var catalog in snap.Catalogs.OrderByDescending(c => c.RefCount).ThenBy(c => c.Name)) {
                    if (!catalogFold.ContainsKey(catalog.Name))
                        catalogFold[catalog.Name] = false;

                    using (new EditorGUILayout.HorizontalScope()) {
                        catalogFold[catalog.Name] = EditorGUILayout.Foldout(catalogFold[catalog.Name], catalog.Name, true);

                        GUILayout.FlexibleSpace();
                        GUILayout.Label($"Ref:{catalog.RefCount}", GUILayout.Width(55));
                        GUILayout.Label($"Loaded:{catalog.LoadedCount}/{catalog.EntryCount}", GUILayout.Width(130));

                        bool isSelected = selectedCatalog == catalog.Name;
                        bool newSelected = GUILayout.Toggle(isSelected, "Select", EditorStyles.miniButton, GUILayout.Width(55));
                        if (newSelected != isSelected) selectedCatalog = newSelected ? catalog.Name : null;
                    }

                    if (catalogFold[catalog.Name]) {
                        var preview = (snap.Entries ?? new List<AudioClipProviderSnapshot.Entry>())
                            .Where(entry => entry.CatalogNames != null && entry.CatalogNames.Contains(catalog.Name))
                            .OrderByDescending(entry => entry.Dependency)
                            .ThenByDescending(entry => entry.IsLoaded)
                            .Take(10)
                            .ToList();

                        int depPositive = preview.Count(x => x.Dependency > 0);
                        EditorGUILayout.LabelField($"  Preview Top10 (Dep>0:{depPositive})");

                        foreach (var entry in preview) {
                            using (new EditorGUILayout.HorizontalScope()) {
                                GUILayout.Space(14);
                                GUILayout.Label(entry.IsLoaded ? "●" : "○", GUILayout.Width(18));
                                EditorGUILayout.LabelField($"ID:{entry.Id}", GUILayout.Width(80));
                                EditorGUILayout.LabelField($"Dep:{entry.Dependency}", GUILayout.Width(70));
                                EditorGUILayout.LabelField(entry.IsLoaded ? entry.ClipName : "(not loaded)", GUILayout.Width(220));

                                GUILayout.FlexibleSpace();

                                using (new EditorGUI.DisabledScope(!entry.IsLoaded || !entry.Clip)) {
                                    if (GUILayout.Button("Ping", EditorStyles.miniButton, GUILayout.Width(45)))
                                        EditorGUIUtility.PingObject(entry.Clip);
                                }
                            }
                        }

                        EditorGUILayout.Space(4);
                    }
                }

                if (GUILayout.Button("Clear Selection", EditorStyles.miniButton))
                    selectedCatalog = null;
            }
        }
        private void _DrawEntries(AudioClipProviderSnapshot snap) {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            IEnumerable<AudioClipProviderSnapshot.Entry> view =
                snap.Entries ?? Enumerable.Empty<AudioClipProviderSnapshot.Entry>();

            if (!string.IsNullOrEmpty(selectedCatalog))
                view = view.Where(v => v.CatalogNames != null && v.CatalogNames.Contains(selectedCatalog));

            if (loadedOnly) view = view.Where(v => v.IsLoaded);
            if (depPositiveOnly) view = view.Where(v => v.Dependency > 0);

            if (!string.IsNullOrWhiteSpace(search)) {
                var target = search.Trim();

                view = view.Where(v => {
                    if (v.Id.ToString().Contains(target))
                        return true;

                    if (!string.IsNullOrEmpty(v.Token) &&
                        v.Token.IndexOf(target, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;

                    if (!string.IsNullOrEmpty(v.ClipName) &&
                        v.ClipName.IndexOf(target, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;

                    if (v.CatalogNames != null &&
                        v.CatalogNames.Any(n => !string.IsNullOrEmpty(n) &&
                            n.IndexOf(target, StringComparison.OrdinalIgnoreCase) >= 0))
                        return true;

                    return false;
                });
            }

            view = view
                .OrderByDescending(v => v.IsLoaded)
                .ThenByDescending(v => v.Dependency)
                .ThenBy(v => v.Id);

            foreach (var entry in view) {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox)) {
                    GUILayout.Label(entry.IsLoaded ? "●" : "○", GUILayout.Width(18));
                    EditorGUILayout.LabelField($"ID:{entry.Id}", GUILayout.Width(80));
                    EditorGUILayout.LabelField($"Dep:{entry.Dependency}", GUILayout.Width(70));

                    string catalogsText = (entry.CatalogNames == null || entry.CatalogNames.Count == 0)
                        ? "(Unassigned)"
                        : string.Join(", ", entry.CatalogNames);

                    EditorGUILayout.LabelField(catalogsText, GUILayout.Width(220));

                    EditorGUILayout.LabelField(entry.IsLoaded ? entry.ClipName : "(not loaded)", GUILayout.Width(220));
                    EditorGUILayout.LabelField(entry.IsLoaded ? $"{entry.ClipLength:0.00}s" : "-", GUILayout.Width(70));

                    GUILayout.FlexibleSpace();

                    using (new EditorGUI.DisabledScope(!entry.IsLoaded || !entry.Clip)) {
                        if (GUILayout.Button("Ping", EditorStyles.miniButton, GUILayout.Width(45)))
                            EditorGUIUtility.PingObject(entry.Clip);
                    }

                    EditorGUILayout.SelectableLabel(entry.Token ?? string.Empty, GUILayout.Height(18));
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
#endif

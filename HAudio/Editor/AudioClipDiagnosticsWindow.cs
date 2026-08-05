#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * AudioManager 클립 로드 상태를 에디터에서 실시간 진단하는 창입니다.
 *
 * 사용법 ::
 * + HCUP/Audio/Data Diagnostics 메뉴로 창을 엽니다.
 * + Play Mode에서만 데이터가 표시됩니다.
 * + Bind: AudioManager 인스턴스를 재탐색합니다.
 * + Refresh: 스냅샷을 갱신합니다.
 * + Auto 토글로 자동 갱신 간격을 설정합니다.
 *
 * 주의사항 ::
 * + Play Mode 진입·종료 시 상태가 자동 초기화됩니다.
 * =========================================================
 */

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HAudio.Editor {
    public sealed class AudioClipDiagnosticsWindow : EditorWindow {
        Vector2 scroll;

        string search = string.Empty;
        bool loadedOnly = true;

        bool autoRefresh;
        double refreshIntervalSec = 0.5;
        double nextRefreshTime;

        bool isBound;
        AudioClipManagerSnapshot snapshot;

        readonly Dictionary<string, bool> catalogFold = new();
        string selectedCatalog;

        [MenuItem("HCUP/Audio/Data Diagnostics")]
        public static void Open() {
            var window = GetWindow<AudioClipDiagnosticsWindow>();
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
            _ResetState();
            Repaint();
        }

        private void _ResetState() {
            isBound = false;
            snapshot = null;

            selectedCatalog = null;
            catalogFold.Clear();

            scroll = Vector2.zero;

            search = string.Empty;
            loadedOnly = true;
            autoRefresh = false;

            nextRefreshTime = EditorApplication.timeSinceStartup + refreshIntervalSec;
        }

        private void OnGUI() {
            if (!EditorApplication.isPlaying) {
                EditorGUILayout.HelpBox("Play Mode에서만 표시됩니다.", MessageType.Info);
                return;
            }

            _DrawToolbar();

            _EnsureManager();

            if (!isBound) {
                EditorGUILayout.HelpBox(
                    "AudioManager를 찾을 수 없습니다.\n" +
                    "- [BM] AudioManager가 씬에 존재하는지 확인하세요.\n" +
                    "- Bind 버튼으로 재탐색합니다.",
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
                    isBound = false;
                    _EnsureManager(force: true);
                }

                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
                    _RefreshSnapshot();

                GUILayout.Space(10);

                loadedOnly = GUILayout.Toggle(loadedOnly, "Loaded", EditorStyles.toolbarButton);

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
            try {
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

        private void _EnsureManager(bool force = false) {
            if (!force && isBound) return;
            isBound = AudioManager.HasInstance;
        }

        private void _AutoRefreshIfNeeded() {
            if (!autoRefresh) return;

            var now = EditorApplication.timeSinceStartup;
            if (now < nextRefreshTime) return;

            nextRefreshTime = now + refreshIntervalSec;
            _RefreshSnapshot();
        }

        private void _RefreshSnapshot() {
            if (!AudioManager.HasInstance) {
                isBound = false;
                return;
            }
            snapshot = AudioManager.Instance.CreateSnapshot();
            Repaint();
        }

        private void _DrawSnapshot(AudioClipManagerSnapshot snap) {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
                EditorGUILayout.LabelField($"Manager: {snap.ManagerName}");
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

        private void _DrawCatalogs(AudioClipManagerSnapshot snap) {
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
                        var preview = (snap.Entries ?? new List<AudioClipManagerSnapshot.Entry>())
                            .Where(entry => entry.CatalogNames != null && entry.CatalogNames.Contains(catalog.Name))
                            .OrderByDescending(entry => entry.IsLoaded)
                            .Take(10)
                            .ToList();

                        EditorGUILayout.LabelField($"  Preview Top10");

                        foreach (var entry in preview) {
                            using (new EditorGUILayout.HorizontalScope()) {
                                GUILayout.Space(14);
                                GUILayout.Label(entry.IsLoaded ? "●" : "○", GUILayout.Width(18));
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

        private void _DrawEntries(AudioClipManagerSnapshot snap) {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            IEnumerable<AudioClipManagerSnapshot.Entry> view =
                snap.Entries ?? Enumerable.Empty<AudioClipManagerSnapshot.Entry>();

            if (!string.IsNullOrEmpty(selectedCatalog))
                view = view.Where(v => v.CatalogNames != null && v.CatalogNames.Contains(selectedCatalog));

            if (loadedOnly) view = view.Where(v => v.IsLoaded);

            if (!string.IsNullOrWhiteSpace(search)) {
                var target = search.Trim();

                view = view.Where(v => {
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
                .ThenBy(v => v.Token);

            foreach (var entry in view) {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox)) {
                    GUILayout.Label(entry.IsLoaded ? "●" : "○", GUILayout.Width(18));

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

/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.24 AudioManager 기반으로 리팩토링
 *
 * # 변경
 * - SoundManager / IAudioClipDiagnostics 의존 제거
 * - AudioManager.Instance.CreateSnapshot() → AudioClipManagerSnapshot 사용으로 전환
 * - diagnostics 필드 제거 → isBound(bool) + AudioManager.HasInstance로 대체
 * - depPositiveOnly 토글 제거 (AudioClipManagerSnapshot.Entry에 Dependency 없음)
 * - Prune 버튼 제거 (AudioManager에 PruneUnusedTokens 미노출)
 * - 엔트리 열 재구성: Id·Dep 열 제거, Token을 우측 SelectableLabel로 유지
 * - using HAudio.Load 제거
 *
 * # 이유
 * - Bootstrap 씬에서 SoundManager가 AudioManager로 교체됨
 * - IAudioClipDiagnostics는 AudioClipProvider(구 시스템) 전용 인터페이스
 * - AudioManager는 CreateSnapshot()으로 AudioClipManagerSnapshot을 직접 반환
 *
 * =============================================================================
 */
#endif

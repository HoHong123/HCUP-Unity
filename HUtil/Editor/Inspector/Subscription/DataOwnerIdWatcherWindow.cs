#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HUtil.Editor.Subscription {
    public sealed class DataOwnerIdWatcherWindow : EditorWindow {
        #region Fields
        Vector2 scrollPosition;
        string searchText = string.Empty;
        bool showOnlyUnityObjects = true;
        bool showOnlyAlive = true;
        #endregion

        #region Menu
        [MenuItem("HCUP/Data/Owner Watcher")]
        public static void Open() {
            DataOwnerIdWatcherWindow window = GetWindow<DataOwnerIdWatcherWindow>();
            window.titleContent = new GUIContent("OwnerId Watcher");
            window.Show();
        }
        #endregion

        #region Unity Life Cycle
        private void OnGUI() {
            _DrawToolbar();
            _DrawHeader();
            _DrawBody();
        }
        #endregion

        #region Private - Draw
        void _DrawToolbar() {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar)) {
                GUILayout.Label("Search", GUILayout.Width(50f));

                searchText = GUILayout.TextField(searchText, EditorStyles.toolbarTextField, GUILayout.ExpandWidth(true));

                showOnlyUnityObjects = GUILayout.Toggle(showOnlyUnityObjects, "Unity Only", EditorStyles.toolbarButton, GUILayout.Width(90f));

                showOnlyAlive = GUILayout.Toggle(showOnlyAlive, "Alive Only", EditorStyles.toolbarButton, GUILayout.Width(90f));

                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                    Repaint();

                if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                    DataOwnerIdWatchRegistry.Clear();
            }
        }

        void _DrawHeader() {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox)) {
                GUILayout.Label("OwnerId", EditorStyles.boldLabel, GUILayout.Width(80f));
                GUILayout.Label("Owner", EditorStyles.boldLabel, GUILayout.Width(220f));
                GUILayout.Label("Class", EditorStyles.boldLabel, GUILayout.Width(180f));
                GUILayout.Label("Container", EditorStyles.boldLabel, GUILayout.Width(220f));
                GUILayout.Label("Alive", EditorStyles.boldLabel, GUILayout.Width(50f));
                GUILayout.Label("CreatedAt", EditorStyles.boldLabel);
            }
        }

        void _DrawBody() {
            IReadOnlyDictionary<int, DataOwnerIdWatchRegistry.Entry> table = DataOwnerIdWatchRegistry.Table;

            IEnumerable<DataOwnerIdWatchRegistry.Entry> entries = table.Values
                .OrderBy(entry => entry.OwnerId);

            if (!string.IsNullOrWhiteSpace(searchText)) {
                string query = searchText.Trim();

                entries = entries.Where(entry =>
                    (entry.OwnerId.ToString().IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrWhiteSpace(entry.OwnerDisplayName) && entry.OwnerDisplayName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrWhiteSpace(entry.ClassName) && entry.ClassName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (!string.IsNullOrWhiteSpace(entry.ContainerName) && entry.ContainerName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0));
            }

            if (showOnlyUnityObjects)
                entries = entries.Where(entry => entry.IsUnityObject);

            if (showOnlyAlive)
                entries = entries.Where(entry => entry.IsAlive);

            using (EditorGUILayout.ScrollViewScope scope = new(scrollPosition)) {
                scrollPosition = scope.scrollPosition;

                foreach (DataOwnerIdWatchRegistry.Entry entry in entries)
                    _DrawEntry(entry);
            }
        }

        void _DrawEntry(DataOwnerIdWatchRegistry.Entry entry) {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox)) {
                GUILayout.Label(entry.OwnerId.ToString(), GUILayout.Width(80f));

                using (new EditorGUI.DisabledScope(!entry.IsUnityObject || entry.UnityOwner == null)) {
                    if (GUILayout.Button(entry.OwnerDisplayName ?? "(null)", GUILayout.Width(220f))) {
                        EditorGUIUtility.PingObject(entry.UnityOwner);
                        Selection.activeObject = entry.UnityOwner;
                    }
                }

                GUILayout.Label(entry.ClassName ?? "(null)", GUILayout.Width(180f));
                GUILayout.Label(entry.ContainerName ?? "(null)", GUILayout.Width(220f));
                GUILayout.Label(entry.IsAlive ? "Y" : "N", GUILayout.Width(50f));
                GUILayout.Label(entry.CreatedAt ?? "(null)");
            }
        }
        #endregion
    }
}
#endif
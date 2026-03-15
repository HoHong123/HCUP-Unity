using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using HUtil.Inspector;

namespace HGame.Sound.Core {
    [CreateAssetMenu(
        menuName = "HCUP/Audio/Sound Catalog", 
        fileName = "SoundCatalog")]
    public sealed class SoundCatalogSO : ScriptableObject {
        #region Const
        const string RESOURCE_ROOT = "Assets/Resources/";
        #endregion

        #region Nested class
        [Serializable]
        public sealed class Entry {
            [HTitle("Sound Key (Sound Data Data)")]
            [SerializeField]
            SoundKey key;

            [HTitle("Load Token (Resources Key / Addressables Key)")]
            [SerializeField]
            string token;

#if UNITY_EDITOR
            [HTitle("Editor Only (Ping/Preview Source Data)")]
            [SerializeField]
            AudioClip editorClip;
#endif

            public SoundKey Key => key;
            public string Token => token;

#if UNITY_EDITOR
            public AudioClip EditorClip => editorClip;
            public void EditorSetClip(AudioClip clip) => editorClip = clip;
            public void EditorSetToken(string value) => token = value;
#endif
        }
        #endregion

        #region Fields
        [SerializeField]
        List<Entry> entries = new();

        Dictionary<SoundKey, Entry> table;
        #endregion

        #region Properties
        public IReadOnlyList<Entry> Entries => entries;
        #endregion

        #region Init
        public void BuildCache() {
            table = new Dictionary<SoundKey, Entry>(entries.Count);

            for (int k = 0; k < entries.Count; k++) {
                var entity = entries[k];
                Assert.IsTrue(entity.Key.Id > -1, $"SoundCatalog :: id must be >= 0. index={k}");
                Assert.IsFalse(table.ContainsKey(entity.Key), $"SoundCatalog :: duplicated Key '{entity.Key}'. index={k}");
                Assert.IsFalse(string.IsNullOrWhiteSpace(entity.Token), $"SoundCatalog :: token is empty. Key={entity.Key}");
                table.Add(entity.Key, entity);
            }
        }
        #endregion

        #region Public API
        public bool TryGet(in SoundKey key, out Entry entry) {
            if (table == null) BuildCache();
            return table.TryGetValue(key, out entry);
        }
        #endregion

#if UNITY_EDITOR
        #region Update
        [ContextMenu("Editor/Update Tokens From Clips")]
        public void EditorUpdateTokensFromClips() {
            bool changed = false;

            foreach (var entry in entries) {
                if (!entry.EditorClip) continue;

                string path = AssetDatabase.GetAssetPath(entry.EditorClip);
                string token = _ToResourcesToken(path);
                if (string.IsNullOrWhiteSpace(token)) continue;

                if (!string.Equals(entry.Token, token, StringComparison.Ordinal)) {
                    entry.EditorSetToken(token);
                    changed = true;
                }
            }

            if (changed) {
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
            }
        }

        [ContextMenu("Editor/Resolve EditorClip From Token")]
        public void EditorResolveClipsFromTokens() {
            bool changed = false;
            foreach (var entry in entries) {
                if (entry.EditorClip) continue;
                if (string.IsNullOrWhiteSpace(entry.Token)) continue;

                var clip = _TryLoadClipFromToken(entry.Token);
                if (!clip) continue;

                entry.EditorSetClip(clip);
                changed = true;
            }

            if (changed) {
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
            }
        }

        public void EditorClearEntries() {
            entries.Clear();
        }

        public void EditorAddEntry(
            SoundKey key,
            string token,
            AudioClip editorClip) {
            var entry = new Entry();
            typeof(Entry)
                .GetField("key", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(entry, key);
            typeof(Entry)
                .GetField("token", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(entry, token);
            entry.EditorSetClip(editorClip);
            entries.Add(entry);
        }


        private static string _ToResourcesToken(string assetPath) {
            if (string.IsNullOrWhiteSpace(assetPath)) return string.Empty;
            assetPath = assetPath.Replace("\\", "/");

            if (assetPath.StartsWith(RESOURCE_ROOT, StringComparison.OrdinalIgnoreCase))
                assetPath = assetPath.Substring(RESOURCE_ROOT.Length);

            assetPath = System.IO.Path.ChangeExtension(assetPath, null);
            return assetPath;
        }

        private static AudioClip _TryLoadClipFromToken(string token) {
            var guids = AssetDatabase.FindAssets($"t:AudioClip {System.IO.Path.GetFileName(token)}");
            foreach (var guid in guids) {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains(token, StringComparison.OrdinalIgnoreCase))
                    return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }
            return null;
        }
        #endregion
#endif
    }
}
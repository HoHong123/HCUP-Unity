using System;
using System.Collections.Generic;
using UnityEngine;
using HInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HAudio.Core {
    [CreateAssetMenu(
        fileName = "SoundCatalog",
        menuName = "HCUP/Sound/Sound Catalog")]
    public sealed class AudioCatalogSO : ScriptableObject {
        #region Private - Const
        const string RESOURCE_ROOT = "Assets/Resources/";
        #endregion

        #region Public - Nested Types
        [Serializable]
        public sealed class Entry {
            #region Private - Serialized Fields
            // 분류용 enum. 개발자가 카탈로그를 읽을 때의 라벨이며 로드 키가 아니다.
            [SerializeField]
            AudioMajorCategory major;

            [HTitle("Load Token")]
            [SerializeField]
            string token;

            [SerializeField]
            string path;

#if UNITY_EDITOR
            [HTitle("Editor Preview")]
            [SerializeField]
            AudioClip editorClip;
#endif
            #endregion

            #region Public - Properties
            public AudioMajorCategory Major => major;
            public string Token => token;
            public string Path => path;
#if UNITY_EDITOR
            public AudioClip EditorClip => editorClip;
#endif
            #endregion

#if UNITY_EDITOR
            #region Public - Editor
            public void EditorSetMajor(AudioMajorCategory value) => major = value;
            public void EditorSetToken(string value) => token = value;
            public void EditorSetPath(string value) => path = value;
            public void EditorSetClip(AudioClip value) => editorClip = value;
            #endregion
#endif
        }
        #endregion

        #region Private - Fields
        [SerializeField]
        List<Entry> entries = new();

        Dictionary<string, Entry> entryMap;
        #endregion

        #region Public - Properties
        public IReadOnlyList<Entry> Entries => entries;
        #endregion

        #region Public - Cache
        public void BuildCache() {
            entryMap = new Dictionary<string, Entry>(entries.Count, StringComparer.Ordinal);

            for (int k = 0; k < entries.Count; k++) {
                Entry entry = entries[k];
                // Assert 는 릴리즈에서 제거된다 — 유효하지 않은 행은 런타임에도 건너뛴다.
                if (entry == null) {
                    Debug.LogError($"[AudioCatalogSO] Entry is null. index={k}", this);
                    continue;
                }
                string token = _NormalizeRuntimeToken(entry.Token);
                if (string.IsNullOrWhiteSpace(token)) {
                    Debug.LogError($"[AudioCatalogSO] Token is empty. index={k}", this);
                    continue;
                }
                if (entryMap.ContainsKey(token)) {
                    Debug.LogError($"[AudioCatalogSO] Duplicated token detected. token={token}, index={k}", this);
                    continue;
                }
                entryMap.Add(token, entry);
            }
        }

        public bool TryGet(string token, out Entry entry) {
            if (entryMap == null) BuildCache();
            return entryMap.TryGetValue(_NormalizeRuntimeToken(token), out entry);
        }

        // 에디터의 _NormalizeToken 은 확장자 제거까지 수행하는 저작용이라 런타임에서 쓸 수 없다
        // (#if UNITY_EDITOR 안에 있다). 런타임 정규화는 registry/repository 와 동일하게 Trim 만 한다.
        private static string _NormalizeRuntimeToken(string token) {
            if (string.IsNullOrWhiteSpace(token)) return string.Empty;
            return token.Trim();
        }
        #endregion

        #region Public - Load Key Builder
        public static string BuildResourcesLoadKey(string path, string token) {
            if (string.IsNullOrWhiteSpace(token)) return string.Empty;
            if (string.IsNullOrWhiteSpace(path)) return token;
            return $"{path.Trim('/')}/{token}";
        }

        public static string BuildAddressableLoadKey(string token) {
            return string.IsNullOrWhiteSpace(token) ? string.Empty : token;
        }
        #endregion

#if UNITY_EDITOR
        #region Public - Editor
        [ContextMenu("Editor/Clear Entries")]
        public void EditorClearEntries() {
            entries.Clear();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        public void EditorAddEntry(
            AudioMajorCategory major,
            string token,
            string path,
            AudioClip clip) {
            Entry entry = new Entry();
            entry.EditorSetMajor(major);
            entry.EditorSetToken(_NormalizeToken(token));
            entry.EditorSetPath(_NormalizeFolderPath(path));
            entry.EditorSetClip(clip);
            entries.Add(entry);
        }

        [ContextMenu("Editor/Update Token And Path From Clips")]
        public void EditorUpdateTokenAndPathFromClips() {
            bool changed = false;

            foreach (Entry entry in entries) {
                if (entry == null || entry.EditorClip == null) continue;

                string assetPath = AssetDatabase.GetAssetPath(entry.EditorClip);
                if (!_TryParseResourcesAsset(assetPath, out string folderPath, out string token))
                    continue;

                string normalizedToken = _NormalizeToken(token);
                string normalizedPath = _NormalizeFolderPath(folderPath);

                if (!string.Equals(entry.Token, normalizedToken, StringComparison.Ordinal)) {
                    entry.EditorSetToken(normalizedToken);
                    changed = true;
                }

                if (!string.Equals(entry.Path, normalizedPath, StringComparison.Ordinal)) {
                    entry.EditorSetPath(normalizedPath);
                    changed = true;
                }
            }

            if (!changed) return;

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        [ContextMenu("Editor/Resolve Clips From Token And Path")]
        public void EditorResolveClipsFromTokenAndPath() {
            bool changed = false;

            foreach (Entry entry in entries) {
                if (entry == null) continue;
                if (entry.EditorClip != null) continue;
                if (string.IsNullOrWhiteSpace(entry.Token)) continue;

                string resourcesLoadKey = BuildResourcesLoadKey(entry.Path, entry.Token);
                AudioClip clip = _TryLoadClipFromResourcesLoadKey(resourcesLoadKey);
                if (clip == null) continue;

                entry.EditorSetClip(clip);
                changed = true;
            }

            if (!changed) return;

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
        #endregion

        #region Private - Editor Parse
        private static bool _TryParseResourcesAsset(
            string assetPath,
            out string folderPath,
            out string token) {
            folderPath = string.Empty;
            token = string.Empty;

            if (string.IsNullOrWhiteSpace(assetPath))
                return false;

            string normalizedAssetPath = assetPath.Replace("\\", "/");
            if (!normalizedAssetPath.StartsWith(RESOURCE_ROOT, StringComparison.OrdinalIgnoreCase))
                return false;

            string relativePath = normalizedAssetPath.Substring(RESOURCE_ROOT.Length);
            string pathWithoutExtension = System.IO.Path.ChangeExtension(relativePath, null)?.Replace("\\", "/");
            if (string.IsNullOrWhiteSpace(pathWithoutExtension))
                return false;

            token = System.IO.Path.GetFileName(pathWithoutExtension);
            folderPath = System.IO.Path.GetDirectoryName(pathWithoutExtension)?.Replace("\\", "/") ?? string.Empty;

            token = _NormalizeToken(token);
            folderPath = _NormalizeFolderPath(folderPath);
            return !string.IsNullOrWhiteSpace(token);
        }

        private static AudioClip _TryLoadClipFromResourcesLoadKey(string resourcesLoadKey) {
            if (string.IsNullOrWhiteSpace(resourcesLoadKey)) return null;

            string targetKey = resourcesLoadKey.Replace("\\", "/").Trim('/');
            string fileName = System.IO.Path.GetFileName(targetKey);
            if (string.IsNullOrWhiteSpace(fileName)) return null;

            string[] guids = AssetDatabase.FindAssets($"t:AudioClip {fileName}");
            foreach (string guid in guids) {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid).Replace("\\", "/");
                if (!_TryParseResourcesAsset(assetPath, out string folderPath, out string token)) continue;

                string candidateKey = BuildResourcesLoadKey(folderPath, token);
                if (!string.Equals(candidateKey, targetKey, StringComparison.Ordinal)) continue;

                return AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            }

            return null;
        }

        private static string _NormalizeToken(string token) {
            if (string.IsNullOrWhiteSpace(token)) return string.Empty;
            return System.IO.Path.ChangeExtension(token.Trim(), null)?.Trim() ?? string.Empty;
        }

        private static string _NormalizeFolderPath(string path) {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;

            string normalized = path.Replace("\\", "/").Trim();

            if (normalized.StartsWith(RESOURCE_ROOT, StringComparison.OrdinalIgnoreCase)) {
                normalized = normalized.Substring(RESOURCE_ROOT.Length);
            }

            normalized = normalized.Trim('/');

            string fileName = System.IO.Path.GetFileName(normalized);
            string extension = System.IO.Path.GetExtension(fileName);

            if (!string.IsNullOrWhiteSpace(extension)) {
                normalized = System.IO.Path.GetDirectoryName(normalized)?.Replace("\\", "/")?.Trim('/') ?? string.Empty;
            }

            return normalized;
        }
        #endregion
#endif
    }
}

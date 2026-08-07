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
        #region Public - Const
        // 인스펙터 클립 드롭다운([HDropdown])이 항목 공급자를 찾는 키.
        // 런타임 속성 인자와 에디터 등록부가 같은 상수를 봐야 오타로 어긋나지 않는다.
        public const string DROPDOWN_SOURCE_ID = "HAudio.Clips";
        #endregion

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

            // 런타임 신원. token 접두의 숫자와 항상 같은 값이며, 생성기가 token 에서 뽑아 채운다.
            // 별도 필드로 두는 이유 : 재생마다 token 문자열을 파싱하지 않기 위해서다.
            // 이 값이 게임 쪽 AudioClips enum 의 원소 값이 된다.
            [HTitle("Identity")]
            [SerializeField]
            int uid;

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
            public int Uid => uid;
            public string Token => token;
            public string Path => path;
#if UNITY_EDITOR
            public AudioClip EditorClip => editorClip;
#endif
            #endregion

#if UNITY_EDITOR
            #region Public - Editor
            public void EditorSetMajor(AudioMajorCategory value) => major = value;
            public void EditorSetUid(int value) => uid = value;
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

        #region Public - Token Identity
        // token 은 "{uid}_{이름}" 규약이다 (예: "600003_Click"). 이 규약은 파일명에서 그대로 오며,
        // uid 는 엑셀 데이터 넘버링·enum 원소 값·탐색기 정렬·로드 유일키 네 역할을 겸한다.
        //
        // 아래 두 함수가 그 규약의 단일 해석 지점이다. 파싱은 저작·생성 시점에만 수행하고,
        // 런타임 재생 경로는 Entry.Uid 를 직접 읽는다 (문자열을 만지지 않는다).

        /// <summary> token 접두의 uid 를 뽑는다. 규약을 어긴 token 이면 false. </summary>
        public static bool TryParseUid(string token, out int uid) {
            uid = 0;
            if (string.IsNullOrWhiteSpace(token)) return false;

            string trimmed = token.Trim();
            int separator = trimmed.IndexOf('_');
            // 구분자가 없거나, 맨 앞이거나, 숫자부가 없으면 규약 위반이다.
            if (separator < 1) return false;

            for (int k = 0; k < separator; k++) {
                if (!char.IsDigit(trimmed[k])) return false;
            }

            // int.Parse 대신 TryParse — 자릿수가 int 범위를 넘는 token 도 조용히 통과시키지 않는다.
            return int.TryParse(
                trimmed.Substring(0, separator),
                System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture,
                out uid);
        }

        /// <summary> token 의 uid 접두를 제외한 이름부를 뽑는다 (enum 멤버 이름의 원천). </summary>
        public static bool TryParseName(string token, out string name) {
            name = string.Empty;
            if (string.IsNullOrWhiteSpace(token)) return false;

            string trimmed = token.Trim();
            int separator = trimmed.IndexOf('_');
            if (separator < 1 || separator + 1 >= trimmed.Length) return false;

            name = trimmed.Substring(separator + 1);
            return !string.IsNullOrWhiteSpace(name);
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
            string normalizedToken = _NormalizeToken(token);

            Entry entry = new Entry();
            entry.EditorSetMajor(major);
            entry.EditorSetToken(normalizedToken);
            entry.EditorSetPath(_NormalizeFolderPath(path));
            entry.EditorSetClip(clip);

            // uid 는 여기 한 곳에서만 채워진다 — 카탈로그로 들어오는 모든 경로가 이 함수를 지난다.
            // 규약 위반은 조용히 0 으로 두지 않고 드러낸다 (0 이면 재생 경로에서 조회가 실패한다).
            if (TryParseUid(normalizedToken, out int uid)) {
                entry.EditorSetUid(uid);
            }
            else {
                Debug.LogError(
                    $"[AudioCatalogSO] Token does not follow the \"{{uid}}_{{name}}\" convention. uid is left unset. token={normalizedToken}", this);
            }

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

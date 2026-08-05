#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * SceneKey와 실제 Scene 이름을 매핑하는 ScriptableObject입니다.
 * 씬 이름 문자열 의존성을 제거하고 열거형 기반 씬 관리 시스템을 구성하기 위해 사용됩니다.
 *
 * 구조 ::
 * SceneKey -> SceneRef -> SceneName
 *
 * 특징 ::
 * - 프로젝트에서 사용하는 씬을 Catalog 형태로 관리
 * - SceneKey 기반 씬 로딩 지원
 * =========================================================
 */
#endif

using System;
using System.Collections.Generic;
using UnityEngine;

namespace HCore.Scene {
    [CreateAssetMenu(menuName = "HCUP/Scene/Scene Catalog")]
    public sealed class SceneCatalogSO : ScriptableObject {
        #region Nested
        [Serializable]
        private struct Entry {
            public SceneKey Key;
            public SceneRef Scene;
        }
        #endregion

        #region Fields
        [SerializeField] 
        List<Entry> entries = new();

        Dictionary<SceneKey, string> scenes;
        #endregion

        #region Public - Resolve
        public bool TryResolve(SceneKey key, out string sceneName) {
            _EnsureBuilt();
            return scenes.TryGetValue(key, out sceneName);
        }
        #endregion

        #region Private - Build
        private void _EnsureBuilt() {
            if (scenes != null) return;
            scenes = new Dictionary<SceneKey, string>(entries.Count);

            // 조용히 스킵하면 SceneLoader 가 "매핑되지 않음" 으로 오진한다 — 실제 원인은 깨진 SceneRef 다.
            foreach (var entry in entries) {
                if (entry.Scene == null) {
                    Debug.LogError($"[SceneCatalog] '{name}': SceneKey '{entry.Key}' has no SceneRef assigned.", this);
                    continue;
                }

                var sceneName = entry.Scene.SceneName;
                if (string.IsNullOrEmpty(sceneName)) {
                    Debug.LogError($"[SceneCatalog] '{name}': SceneKey '{entry.Key}' has a SceneRef with an empty scene name.", this);
                    continue;
                }

                // 중복 키는 last-wins 로 조용히 덮어써지고 있었다. 편집 실수를 드러낸다.
                if (scenes.ContainsKey(entry.Key)) {
                    Debug.LogError($"[SceneCatalog] '{name}': duplicate SceneKey '{entry.Key}'. The last entry wins.", this);
                }

                scenes[entry.Key] = sceneName;
            }
        }
        #endregion

#if UNITY_EDITOR
        #region Private - Validation
        private void OnValidate() {
            foreach (var entry in entries) entry.Scene?.SyncNameFromAsset();
            scenes = null;
        }
        #endregion
#endif
    }
}

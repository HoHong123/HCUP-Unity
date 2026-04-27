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

            foreach (var entry in entries) {
                if (entry.Scene == null) continue;

                var name = entry.Scene.SceneName;
                if (string.IsNullOrEmpty(name)) continue;

                scenes[entry.Key] = name;
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

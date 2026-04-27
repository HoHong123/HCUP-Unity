#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Scene Catalog에서 사용하는 씬 레퍼런스 데이터 클래스입니다.
 * SceneAsset을 이용한 안전한 씬 참조와 런타임에서의 문자열 기반 씬 로딩을 동시에 지원합니다.
 * Editor 환경에서 SceneAsset을 기반으로 SceneName을 자동 동기화합니다.
 *
 * 구성 ::
 * - SceneName : 실제 씬 이름
 * - SceneAsset : 에디터에서 사용하는 SceneAsset 참조
 * =========================================================
 */
#endif

using System;
using UnityEngine;

namespace HCore.Scene {
    [Serializable]
    public sealed class SceneRef {
        [SerializeField]
        string sceneName;
#if UNITY_EDITOR
        [SerializeField]
        UnityEditor.SceneAsset sceneAsset;
#endif

        public string SceneName => sceneName;

#if UNITY_EDITOR
        public void SyncNameFromAsset() {
            if (sceneAsset == null) return;
            sceneName = sceneAsset.name;
        }
#endif
    }
}

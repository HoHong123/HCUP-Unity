#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 씬 제어 기능을 정의하는 인터페이스입니다.
 *
 * 제공 API ::
 * 1. SceneKey 기반 씬 로드 / 언로드 / 재로드
 * 2. SceneName 기반 씬 로드 / 언로드 / 재로드
 * =========================================================
 */
#endif

using System;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace HCore.Scene {
    public interface ISceneControl {
        #region Scene Key API
        UniTask LoadSceneAsync(
            SceneKey key,
            LoadSceneMode mode = LoadSceneMode.Single,
            Action<float> onProgress = null,
            Action onComplete = null,
            SceneKey? loadingKey = null);

        UniTask UnloadSceneAsync(
            SceneKey key,
            Action<float> onProgress = null,
            Action onComplete = null);

        UniTask ReloadActiveSceneAsync(
            Action<float> onProgress = null,
            Action onComplete = null,
            SceneKey? loadingKey = null);
        #endregion

        #region String API
        UniTask LoadSceneAsync(
            string sceneName,
            LoadSceneMode mode = LoadSceneMode.Single,
            Action<float> onProgress = null,
            Action onComplete = null,
            string loadingScene = null);

        UniTask UnloadSceneAsync(
            string sceneName,
            Action<float> onProgress = null,
            Action onComplete = null);

        UniTask ReloadActiveSceneAsync(
            Action<float> onProgress = null,
            Action onComplete = null,
            string loadingScene = null);
        #endregion
    }
}
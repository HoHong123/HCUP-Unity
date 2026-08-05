#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 씬 제어 기능을 정의하는 인터페이스입니다.
 *
 * 제공 API ::
 * 1. SceneKey 기반 씬 로드 / 언로드 / 재로드
 * 2. SceneName 기반 씬 로드 / 언로드 / 재로드
 *
 * 반환 규약 ::
 * 모든 API 는 UniTask<bool> 을 반환한다. false = 요청이 수행되지 않음
 * (SceneKey 매핑 실패 / 씬 미등록 / 이미 로드 진행 중 / 언로드 대상 부재).
 * =========================================================
 */
#endif

using System;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace HCore.Scene {
    public interface ISceneControl {
        #region Scene Key API
        UniTask<bool> LoadSceneAsync(
            SceneKey key,
            LoadSceneMode mode = LoadSceneMode.Single,
            Action<float> onProgress = null,
            Action onComplete = null,
            SceneKey? loadingKey = null);

        UniTask<bool> UnloadSceneAsync(
            SceneKey key,
            Action<float> onProgress = null,
            Action onComplete = null);

        UniTask<bool> ReloadActiveSceneAsync(
            Action<float> onProgress = null,
            Action onComplete = null,
            SceneKey? loadingKey = null);
        #endregion

        #region String API
        UniTask<bool> LoadSceneAsync(
            string sceneName,
            LoadSceneMode mode = LoadSceneMode.Single,
            Action<float> onProgress = null,
            Action onComplete = null,
            string loadingScene = null);

        UniTask<bool> UnloadSceneAsync(
            string sceneName,
            Action<float> onProgress = null,
            Action onComplete = null);

        UniTask<bool> ReloadActiveSceneAsync(
            Action<float> onProgress = null,
            Action onComplete = null,
            string loadingScene = null);
        #endregion
    }
}
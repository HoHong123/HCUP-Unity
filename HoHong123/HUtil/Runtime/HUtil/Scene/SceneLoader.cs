#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 씬 전환 전역 스크립트 입니다.
 * 
 * ** 사용법 **
 * 1. 전역 함수를 통해 비동기 씬 로드를 진행합니다.
 * 2. 필요에 따라 비동기 함수 진행 중 진행상황에 이벤트를 처리합니다.
 * 3. 각 씬 전환 이벤트가 끝날때 필요한 이벤트를 대리자에 등록할 수 있습니다.
 * =========================================================
 */
#endif

using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using HUtil.Logger;

namespace HUtil.Scene {
    public static class SceneLoader {
        public static event Action OnSceneLoaded;
        public static event Action OnSceneUnloaded;

        public static async UniTask LoadSceneAsync(
            string sceneName,
            LoadSceneMode mode = LoadSceneMode.Single,
            Action<float> onProgress = null,
            Action onComplete = null,
            string loadingScene = null) {
            if (!string.IsNullOrEmpty(loadingScene) && mode == LoadSceneMode.Single) {
                await SceneManager.LoadSceneAsync(loadingScene);
            }

            var asyncOp = SceneManager.LoadSceneAsync(sceneName, mode);
            asyncOp.allowSceneActivation = false;

            while (asyncOp.progress < 0.9f) {
                onProgress?.Invoke(asyncOp.progress);
                await UniTask.Yield();
            }

            onProgress?.Invoke(1f);
            asyncOp.allowSceneActivation = true;

            await asyncOp.ToUniTask();
            onComplete?.Invoke();
            OnSceneLoaded?.Invoke();
        }

        public static async UniTask UnloadSceneAsync(
            string sceneName,
            Action<float> onProgress = null,
            Action onComplete = null) {
            if (!SceneManager.GetSceneByName(sceneName).isLoaded) {
                HLogger.Warning($"Scene '{sceneName}' is not loaded.");
                return;
            }

            var unloadOp = SceneManager.UnloadSceneAsync(sceneName);

            while (!unloadOp.isDone) {
                onProgress?.Invoke(unloadOp.progress);
                await UniTask.Yield();
            }

            onComplete?.Invoke();
            OnSceneUnloaded?.Invoke();
        }

        public static UniTask ReloadActiveSceneAsync(
            Action<float> onProgress = null,
            Action onComplete = null,
            string loadingScene = null) {
            if (Time.timeScale == 0f) Time.timeScale = 1f;
            var active = SceneManager.GetActiveScene().name;
            return LoadSceneAsync(active, LoadSceneMode.Single, onProgress, onComplete, loadingScene);
        }
    }
}

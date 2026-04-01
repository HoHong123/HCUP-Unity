#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 유니티 씬 전환 커스텀 스크립트입니다.
 * 
 * 1. 비동기 로드/제거/재로딩 시스템 제공
 * 2. 각 씬로드/언로드 시퀀스에 초기화/소멸 단계 액션처리를 제공
 * 3. 필요에 따라, 로딩씬 호출
 * + 로딩씬 종료 여부는 목표 씬 호출 후, 해당 씬 초기화가 끝나는 시점을 개발자가 결정하여 종료해야 합니다.
 * =========================================================
 */
#endif

using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using HUtil.Logger;

namespace HUtil.Scene {
    public static class SceneLoader {
        #region Nested
        private static class SceneLoaderCore {
            public static async UniTask LoadSceneAsync(
                string sceneName,
                LoadSceneMode mode,
                Action<float> onProgress,
                Action onComplete,
                string loadingScene) {
                if (!string.IsNullOrEmpty(loadingScene) && mode == LoadSceneMode.Single)
                    await SceneManager.LoadSceneAsync(loadingScene);

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
            }

            public static async UniTask<bool> UnloadSceneAsync(
                string sceneName,
                Action<float> onProgress,
                Action onComplete) {
                if (!SceneManager.GetSceneByName(sceneName).isLoaded) {
                    HLogger.Error($"[SceneLoader] Scene '{sceneName}' is not loaded.");
                    return false;
                }

                var unloadOp = SceneManager.UnloadSceneAsync(sceneName);

                while (!unloadOp.isDone) {
                    onProgress?.Invoke(unloadOp.progress);
                    await UniTask.Yield();
                }

                onComplete?.Invoke();
                return true;
            }
        }
        #endregion

        #region Fields
        #region Scene Catalog
        static bool isInitialized;
        static SceneCatalogSO baseCatalog;
        static SceneCatalogSO overrideCatalog;

        public static bool IsInitialized => isInitialized;
        public static SceneCatalogSO BaseCatalog => baseCatalog;
        public static SceneCatalogSO OverrideCatalog => overrideCatalog;
        #endregion

        #region Clean Up Events
        public static event Action OnSceneLoaded;
        public static event Action OnSceneUnloaded;
        #endregion

        #region Loading Progress
        public static float LoadProgress { get; private set; }
        #endregion
        #endregion

        #region Init
        public static void Initialize(SceneCatalogSO baseRef, SceneCatalogSO overrideRef = null) {
            Assert.IsFalse(isInitialized, "[SceneLoader] Initialize() must be called only once per play session.");
            Assert.IsNotNull(baseRef, "[SceneLoader] baseRef must not be null.");
            if (isInitialized == true) return;
            baseCatalog = baseRef;
            overrideCatalog = overrideRef;
            isInitialized = true;
        }

        public static void SetOverrideCatalog(SceneCatalogSO overrideRef) {
            overrideCatalog = overrideRef;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void _ResetStatics() {
            baseCatalog = null;
            overrideCatalog = null;
            isInitialized = false;
        }
        #endregion

        #region Scene Key API
        public static UniTask LoadSceneAsync(
            SceneKey key,
            LoadSceneMode mode = LoadSceneMode.Single,
            Action<float> onProgress = null,
            Action onComplete = null,
            SceneKey? loadingKey = null) {
            var sceneName = _ResolveSceneName(key);
            var loadingSceneName = loadingKey.HasValue ? _ResolveSceneName(loadingKey.Value) : null;
            return LoadSceneAsync(sceneName, mode, onProgress, onComplete, loadingSceneName);
        }

        public static UniTask UnloadSceneAsync(
            SceneKey key,
            Action<float> onProgress = null,
            Action onComplete = null) {
            var sceneName = _ResolveSceneName(key);
            return UnloadSceneAsync(sceneName, onProgress, onComplete);
        }

        public static UniTask ReloadActiveSceneAsync(
            Action<float> onProgress = null,
            Action onComplete = null,
            SceneKey? loadingKey = null) {
            var loadingSceneName = loadingKey.HasValue ? _ResolveSceneName(loadingKey.Value) : null;
            return ReloadActiveSceneAsync(onProgress, onComplete, loadingSceneName);
        }
        #endregion

        #region String API
        public static async UniTask LoadSceneAsync(
            string sceneName,
            LoadSceneMode mode = LoadSceneMode.Single,
            Action<float> onProgress = null,
            Action onComplete = null,
            string loadingScene = null) {
            await SceneLoaderCore.LoadSceneAsync(
                sceneName,
                mode,
                progress => {
                    LoadProgress = progress;
                    onProgress?.Invoke(progress);
                },
                onComplete,
                loadingScene);

            OnSceneLoaded?.Invoke();
        }

        public static async UniTask UnloadSceneAsync(
            string sceneName,
            Action<float> onProgress = null,
            Action onComplete = null) {
            var success = await SceneLoaderCore.UnloadSceneAsync(sceneName, onProgress, onComplete);
            if (success) OnSceneUnloaded?.Invoke();
        }

        public static UniTask ReloadActiveSceneAsync(
            Action<float> onProgress = null,
            Action onComplete = null,
            string loadingScene = null) {
            if (Time.timeScale < 1f) Time.timeScale = 1f;
            var active = SceneManager.GetActiveScene().name;
            return LoadSceneAsync(active, LoadSceneMode.Single, onProgress, onComplete, loadingScene);
        }
        #endregion

        #region Private
        private static string _ResolveSceneName(SceneKey key) {
            // 프로젝트 표준 API(SceneKey)를 쓰려면, BaseCatalog는 "반드시" 있어야 정상 플로우가 성립함.
            Assert.IsNotNull(BaseCatalog, "[SceneLoader] BaseCatalog must be assigned before using SceneKey APIs.");

            if (OverrideCatalog != null && OverrideCatalog.TryResolve(key, out var overrideName))
                return overrideName;

            if (BaseCatalog.TryResolve(key, out var baseName))
                return baseName;

            Assert.IsTrue(false, $"[SceneLoader] SceneKey '{key}' is not mapped in catalogs.");
            return null;
        }
        #endregion
    }
}

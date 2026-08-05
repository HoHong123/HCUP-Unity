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
using UnityEngine.SceneManagement;
using HDiagnosis.Logger;

namespace HCore.Scene {
    public static class SceneLoader {
        #region Nested
        private static class SceneLoaderCore {
            public static async UniTask<bool> LoadSceneAsync(
                string sceneName,
                LoadSceneMode mode,
                Action<float> onProgress,
                Action onComplete,
                string loadingScene) {
                if (!string.IsNullOrEmpty(loadingScene)) {
                    if (mode != LoadSceneMode.Single) {
                        // 종전에는 조용히 무시됐다 — 호출자가 로딩 화면을 요청한 사실을 알린다.
                        HLogger.Warning($"[SceneLoader] loadingScene '{loadingScene}' ignored: supported only with LoadSceneMode.Single.");
                    }
                    else {
                        var loadingOp = SceneManager.LoadSceneAsync(loadingScene);
                        if (loadingOp == null) {
                            HLogger.Error($"[SceneLoader] Loading scene '{loadingScene}' could not be loaded. Check Build Settings.");
                            return false;
                        }
                        await loadingOp;
                    }
                }

                // 빌드 세팅 미등록 씬이면 Unity 는 자체 오류만 남기고 null 을 반환한다.
                var asyncOp = SceneManager.LoadSceneAsync(sceneName, mode);
                if (asyncOp == null) {
                    HLogger.Error($"[SceneLoader] Scene '{sceneName}' could not be loaded. Check Build Settings.");
                    return false;
                }
                asyncOp.allowSceneActivation = false;

                // onProgress 는 사용자 코드다. 예외가 이 구간을 빠져나가면 allowSceneActivation 이
                // false 로 남아 이후 모든 씬 전환이 영구 정지한다 — finally 로 활성화를 보장한다.
                try {
                    float lastReported = -1f;
                    while (asyncOp.progress < 0.9f) {
                        float progress = asyncOp.progress;
                        // 0.9 스톨 구간에서 값이 변하지 않아도 매 프레임 호출되던 것을 억제한다.
                        if (progress != lastReported) {
                            lastReported = progress;
                            onProgress?.Invoke(progress);
                        }
                        await UniTask.Yield();
                    }
                    onProgress?.Invoke(1f);
                }
                finally {
                    asyncOp.allowSceneActivation = true;
                }

                await asyncOp.ToUniTask();
                onComplete?.Invoke();
                return true;
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
                if (unloadOp == null) {
                    HLogger.Error($"[SceneLoader] Scene '{sceneName}' could not be unloaded (last remaining scene?).");
                    return false;
                }

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

        #region In-Flight Guard
        // 정적 LoadProgress 와 Unity 의 씬 활성화는 동시 로드를 표현할 수 없다.
        // 두 번째 요청을 거부해 두 로드가 겹치는 것을 막는다 (재진입 포함).
        static bool isLoading;
        public static bool IsLoading => isLoading;
        #endregion
        #endregion

        #region Init
        public static void Initialize(SceneCatalogSO baseRef, SceneCatalogSO overrideRef = null) {
            // Assert 는 릴리즈 빌드에서 통째로 제거된다 — 방어는 런타임 검사로 유지한다.
            // 씬 재로드로 매니저가 교체되면 새 카탈로그로 재초기화되어야 한다.
            // 종전에는 첫 호출만 살아남아 새 씬의 카탈로그가 조용히 무시됐다.
            if (isInitialized && !ReferenceEquals(baseCatalog, baseRef)) {
                HLogger.Warning(
                    $"[SceneLoader] Re-initializing with a different base catalog "
                    + $"('{(baseCatalog == null ? "null" : baseCatalog.name)}' -> '{(baseRef == null ? "null" : baseRef.name)}').");
            }
            if (baseRef == null) {
                HLogger.Throw(new ArgumentNullException(nameof(baseRef), "[SceneLoader] baseRef must not be null."));
                return;
            }
            baseCatalog = baseRef;
            overrideCatalog = overrideRef;
            isInitialized = true;
        }

        public static void SetOverrideCatalog(SceneCatalogSO overrideRef) {
            if (!isInitialized) {
                HLogger.Error("[SceneLoader] SetOverrideCatalog() called before Initialize(). Ignored.");
                return;
            }
            overrideCatalog = overrideRef;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void _ResetStatics() {
            baseCatalog = null;
            overrideCatalog = null;
            isInitialized = false;
            OnSceneLoaded = null;      // Domain Reload 비활성 시 이전 플레이의 구독 잔존 방지
            OnSceneUnloaded = null;
            LoadProgress = 0f;
            isLoading = false;
        }
        #endregion

        #region Scene Key API
        // 반환 bool = "요청이 실제로 수행되었는가". 종전에는 매핑 실패가 CompletedTask 로
        // 위장되어 호출자가 성공과 실패를 구분할 수 없었다.
        public static UniTask<bool> LoadSceneAsync(
            SceneKey key,
            LoadSceneMode mode = LoadSceneMode.Single,
            Action<float> onProgress = null,
            Action onComplete = null,
            SceneKey? loadingKey = null) {
            var sceneName = _ResolveSceneName(key);
            if (sceneName == null) return UniTask.FromResult(false);   // 매핑 실패가 LoadSceneAsync(null) 로 진행되지 않도록 차단
            string loadingSceneName = null;
            if (loadingKey.HasValue) {
                loadingSceneName = _ResolveSceneName(loadingKey.Value);
                if (loadingSceneName == null) {
                    // 로딩씬 해석 실패를 무시하고 진행하면 로딩 화면 없이 전환되어 원인 추적이 어렵다.
                    HLogger.Warning($"[SceneLoader] loadingKey '{loadingKey.Value}' is not mapped. Proceeding without a loading scene.");
                }
            }
            return LoadSceneAsync(sceneName, mode, onProgress, onComplete, loadingSceneName);
        }

        public static UniTask<bool> UnloadSceneAsync(
            SceneKey key,
            Action<float> onProgress = null,
            Action onComplete = null) {
            var sceneName = _ResolveSceneName(key);
            if (sceneName == null) return UniTask.FromResult(false);
            return UnloadSceneAsync(sceneName, onProgress, onComplete);
        }

        public static UniTask<bool> ReloadActiveSceneAsync(
            Action<float> onProgress = null,
            Action onComplete = null,
            SceneKey? loadingKey = null) {
            string loadingSceneName = null;
            if (loadingKey.HasValue) {
                loadingSceneName = _ResolveSceneName(loadingKey.Value);
                if (loadingSceneName == null) {
                    HLogger.Warning($"[SceneLoader] loadingKey '{loadingKey.Value}' is not mapped. Proceeding without a loading scene.");
                }
            }
            return ReloadActiveSceneAsync(onProgress, onComplete, loadingSceneName);
        }
        #endregion

        #region String API
        public static async UniTask<bool> LoadSceneAsync(
            string sceneName,
            LoadSceneMode mode = LoadSceneMode.Single,
            Action<float> onProgress = null,
            Action onComplete = null,
            string loadingScene = null) {
            if (isLoading) {
                HLogger.Error($"[SceneLoader] LoadSceneAsync('{sceneName}') rejected: another load is already in flight.");
                return false;
            }
            isLoading = true;

            bool success;
            try {
                success = await SceneLoaderCore.LoadSceneAsync(
                    sceneName,
                    mode,
                    progress => {
                        LoadProgress = progress;
                        onProgress?.Invoke(progress);
                    },
                    onComplete,
                    loadingScene);
            }
            finally {
                // 완료 후에도 1f 로 고착되던 것을 유휴 상태로 되돌린다.
                LoadProgress = 0f;
                isLoading = false;
            }

            if (success) _InvokeSafely(OnSceneLoaded, nameof(OnSceneLoaded));
            return success;
        }

        public static async UniTask<bool> UnloadSceneAsync(
            string sceneName,
            Action<float> onProgress = null,
            Action onComplete = null) {
            var success = await SceneLoaderCore.UnloadSceneAsync(sceneName, onProgress, onComplete);
            if (success) _InvokeSafely(OnSceneUnloaded, nameof(OnSceneUnloaded));
            return success;
        }

        public static UniTask<bool> ReloadActiveSceneAsync(
            Action<float> onProgress = null,
            Action onComplete = null,
            string loadingScene = null) {
            // 종전에는 1 미만만 보정해 배속(>1) 상태가 새 씬으로 이월됐다. 재로드는 항상 등속에서 시작한다.
            Time.timeScale = 1f;
            var active = SceneManager.GetActiveScene().name;
            return LoadSceneAsync(active, LoadSceneMode.Single, onProgress, onComplete, loadingScene);
        }
        #endregion

        #region Private
        // 멀티캐스트 델리게이트는 구독자 하나가 던지면 뒤 구독자를 호출하지 않는다.
        // 정리(clean-up) 성격의 이벤트에서 "일부만 정리되고 중단" 은 최악의 반쪽 상태다.
        private static void _InvokeSafely(Action handlers, string eventName) {
            if (handlers == null) return;
            foreach (Action handler in handlers.GetInvocationList()) {
                try {
                    handler();
                }
                catch (Exception e) {
                    HLogger.Error($"[SceneLoader] {eventName} subscriber threw: {e}");
                }
            }
        }

        private static string _ResolveSceneName(SceneKey key) {
            // 프로젝트 표준 API(SceneKey)를 쓰려면, BaseCatalog는 "반드시" 있어야 정상 플로우가 성립함.
            if (BaseCatalog == null) {
                HLogger.Error("[SceneLoader] BaseCatalog must be assigned before using SceneKey APIs.");
                return null;
            }

            if (OverrideCatalog != null && OverrideCatalog.TryResolve(key, out var overrideName))
                return overrideName;

            if (BaseCatalog.TryResolve(key, out var baseName))
                return baseName;

            HLogger.Error($"[SceneLoader] SceneKey '{key}' is not mapped in catalogs.");
            return null;
        }
        #endregion
    }
}

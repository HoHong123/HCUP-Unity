#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 프로젝트에서 사용하는 씬 관리 매니저의 베이스 클래스입니다.
 *
 * 특징 ::
 * - SingletonBehaviour 기반 씬 매니저
 * - SceneLoader 초기화 담당
 * - SceneKey 기반 씬 전환 API 제공
 *
 * 추가 기능 ::
 * 개발 환경에서는 Dev Scene Catalog를 사용하여, 별도의 씬 레퍼런스를 사용할 수 있습니다.
 * =========================================================
 */
#endif

using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using HCore;
using HInspector;

namespace HCore.Scene {
    public class BaseSceneManager : SingletonBehaviour<BaseSceneManager>, ISceneControl {
        #region Fields
        [HTitle("Release Scene Reference")]
        [SerializeField]
        protected SceneCatalogSO releaseRef;

#if UNITY_EDITOR || DEBUG
        [HTitle("Dev Scene Reference")]
        [SerializeField]
        protected bool useDevRef;
        [HShowIf(nameof(useDevRef))]
        [SerializeField]
        protected SceneCatalogSO devRef;
#endif
        #endregion

        #region Protected - Unity Life Cycle
        protected override void Awake() {
            base.Awake();
#if UNITY_EDITOR || DEBUG
            if (useDevRef) Assert.IsNotNull(devRef, "[Dr2SceneManager] useDevRef is true but devRef is null.");
            SceneLoader.Initialize(releaseRef, useDevRef ? devRef : null);
#else
            SceneLoader.Initialize(releaseRef);
#endif
        }
        #endregion

        #region Public - Load Scene
        public virtual UniTask LoadSceneAsync(
            SceneKey key,
            LoadSceneMode mode = LoadSceneMode.Single,
            Action<float> onProgress = null,
            Action onComplete = null,
            SceneKey? loadingKey = null)
            => SceneLoader.LoadSceneAsync(key, mode, onProgress, onComplete, loadingKey);

        public virtual UniTask LoadSceneAsync(
            string sceneName,
            LoadSceneMode mode = LoadSceneMode.Single,
            Action<float> onProgress = null,
            Action onComplete = null,
            string loadingScene = null)
            => SceneLoader.LoadSceneAsync(sceneName, mode, onProgress, onComplete, loadingScene);
        #endregion

        #region Public - Reload Same Scene
        public virtual UniTask ReloadActiveSceneAsync(
            Action<float> onProgress = null,
            Action onComplete = null,
            SceneKey? loadingKey = null)
            => SceneLoader.ReloadActiveSceneAsync(onProgress, onComplete, loadingKey);

        public virtual UniTask ReloadActiveSceneAsync(
            Action<float> onProgress = null,
            Action onComplete = null,
            string loadingScene = null)
            => SceneLoader.ReloadActiveSceneAsync(onProgress, onComplete, loadingScene);
        #endregion

        #region Public - Unload Scene
        public virtual UniTask UnloadSceneAsync(
            SceneKey key,
            Action<float> onProgress = null,
            Action onComplete = null)
            => SceneLoader.UnloadSceneAsync(key, onProgress, onComplete);

        public virtual UniTask UnloadSceneAsync(
            string sceneName,
            Action<float> onProgress = null,
            Action onComplete = null)
            => SceneLoader.UnloadSceneAsync(sceneName, onProgress, onComplete);
        #endregion
    }
}
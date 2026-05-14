#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 로컬리제이션 언어 SO 로드·교체 및 HTextLocalizer 델리게이트 연결을 담당하는 싱글톤 매니저.
 *
 * 특징 ::
 * - SingletonBehaviour<T> 상속 — DontDestroyOnLoad Inspector 옵션으로 씬 간 유지
 * - 언어 타입: LocalizationLanguage 열거형 — 타입 안정성 보장, 문자열 오타 방지
 * - _LoadSavedLanguage(): PlayerPrefs 복원 시 Enum.TryParse 로 유효성 검증 후 폴백
 *
 * 주의사항 ::
 * - InitializeAsync() 는 반드시 1회 호출 — 미호출 시 HTextLocalizer.GetText 는 passthrough
 * - Addressable key 규약: Localization_{language} (예: Localization_Korean)
 * - DontDestroyOnLoad 옵션은 Inspector 에서 수동으로 활성화할 것
 *
 * 사용 ::
 * - 초기화: await LocalizationManager.Instance.InitializeAsync(LocalizationLanguage.Korean)
 * - 전환:   await LocalizationManager.Instance.SwitchLanguageAsync(LocalizationLanguage.English)
 * - 조회:   HTextLocalizer.GetText("UI.MAIN.PLAY")
 * =========================================================
 */
#endif

using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using HUtil.Core;
using HUtil.Logger;
using HUI.TextUI;

namespace HLocalization {
    public sealed class LocalizationManager : SingletonBehaviour<LocalizationManager> {
        #region Const
        const string PREFS_LANGUAGE_KEY = "LocalizationManager.Language";
        const LocalizationLanguage DEFAULT_LANGUAGE = LocalizationLanguage.Korean;
        #endregion

        #region Fields
        AsyncOperationHandle<LocalizationSO> currentHandle;
        bool isHandleValid;
        LocalizationSO currentSO;
        LocalizationLanguage? currentLanguage;
        #endregion

        #region Public - Properties
        public LocalizationLanguage? CurrentLanguage => currentLanguage;
        #endregion

        #region Protected - Lifecycle
        protected override void Awake() {
            base.Awake();
            HTextLocalizer.GetText = uid => uid;
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            HTextLocalizer.GetText = null;
            if (isHandleValid) Addressables.Release(currentHandle);
        }
        #endregion

        #region Public - Initialize
        public async UniTask InitializeAsync(LocalizationLanguage defaultLanguage = DEFAULT_LANGUAGE) {
            LocalizationLanguage startLanguage = _LoadSavedLanguage(defaultLanguage);
            bool loaded = await _LoadLanguageAsync(startLanguage);
            if (loaded && currentLanguage.HasValue) HTextLocalizer.RaiseLanguageChanged(currentLanguage.Value.ToString());
        }
        #endregion

        #region Public - Switch
        public async UniTask SwitchLanguageAsync(LocalizationLanguage language) {
            if (language == currentLanguage) return;

            bool wasValid = isHandleValid;
            AsyncOperationHandle<LocalizationSO> prevHandle = currentHandle;

            bool loaded = await _LoadLanguageAsync(language);
            if (!loaded) return;

            // 새 SO 완전히 로드된 후 구 핸들 해제 (교체 간 gap 방지 + 실패 시 구 SO 유지)
            if (wasValid) Addressables.Release(prevHandle);
            PlayerPrefs.SetString(PREFS_LANGUAGE_KEY, language.ToString());
            HTextLocalizer.RaiseLanguageChanged(language.ToString());
        }
        #endregion

        #region Private - Load
        private async UniTask<bool> _LoadLanguageAsync(LocalizationLanguage language) {
            string key = _ToKey(language);
            AsyncOperationHandle<LocalizationSO> handle = Addressables.LoadAssetAsync<LocalizationSO>(key);
            await handle.ToUniTask();

            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null) {
                HLogger.Error(
                    $"[LocalizationManager] Addressable '{key}' 를 찾을 수 없습니다. " +
                    "Addressables Groups 에 해당 키로 LocalizationSO 를 등록했는지 확인하세요.");
                Addressables.Release(handle);
                return false;
            }

            currentHandle   = handle;
            isHandleValid   = true;
            currentSO       = handle.Result;
            currentLanguage = language;
            HTextLocalizer.GetText = _GetText;
            return true;
        }
        #endregion

        #region Private - GetText
        private string _GetText(string uid) {
            if (currentSO != null && currentSO.TryGetText(uid, out string text)) return text;
            HLogger.Log($"[LocalizationManager] UID '{uid}' 번역 없음. (Language: {currentLanguage})");
            return uid;
        }
        #endregion

        #region Private - Helpers
        private static string _ToKey(LocalizationLanguage language) => $"Localization_{language}";

        private LocalizationLanguage _LoadSavedLanguage(LocalizationLanguage defaultLanguage) {
            string saved = PlayerPrefs.GetString(PREFS_LANGUAGE_KEY, defaultLanguage.ToString());
            return Enum.TryParse<LocalizationLanguage>(saved, out LocalizationLanguage result) ? result : defaultLanguage;
        }
        #endregion
    }
}

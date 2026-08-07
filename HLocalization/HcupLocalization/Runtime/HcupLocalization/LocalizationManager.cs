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
using HCore;
using HResource.Data;
using HResource.Provider;
using HDiagnosis.Logger;
using HUI.TextUI;

namespace HcupLocalization {
    public sealed class LocalizationManager : SingletonBehaviour<LocalizationManager> {
        #region Const
        const string PREFS_LANGUAGE_KEY = "LocalizationManager.Language";
        const LocalizationLanguage DEFAULT_LANGUAGE = LocalizationLanguage.Korean;
        #endregion

        #region Fields
        [SerializeField]
        LocalizationLanguage? currentLanguage;

        AssetProvider<string, LocalizationSO> provider;
        LocalizationSO currentSO;
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
            // InitializeAsync 에서 이 매니저가 provider 를 만들었으므로 폐기 책임도 여기에 있다.
            provider?.Dispose();
            provider = null;
        }
        #endregion

        #region Public - Initialize
        public async UniTask InitializeAsync(LocalizationLanguage defaultLanguage = DEFAULT_LANGUAGE) {
            provider = AssetProviderFactory.CreateAddressable<LocalizationSO>();
            LocalizationLanguage startLanguage = _LoadSavedLanguage(defaultLanguage);
            bool loaded = await _LoadLanguageAsync(startLanguage);
            if (loaded && currentLanguage.HasValue) HTextLocalizer.RaiseLanguageChanged(currentLanguage.Value.ToString());
        }
        #endregion

        #region Public - Switch
        public async UniTask SwitchLanguageAsync(LocalizationLanguage language) {
            if (language == currentLanguage) return;

            string prevKey = currentLanguage.HasValue ? _ToKey(currentLanguage.Value) : null;
            bool loaded = await _LoadLanguageAsync(language);
            if (!loaded) return;

            // 새 SO 완전히 로드된 후 구 SO 해제 (교체 간 gap 방지 + 실패 시 구 SO 유지)
            if (prevKey != null) provider.Release(prevKey);
            PlayerPrefsHandler.SetString(PREFS_LANGUAGE_KEY, language.ToString());
            HTextLocalizer.RaiseLanguageChanged(language.ToString());
        }
        #endregion

        #region Private - Load
        private async UniTask<bool> _LoadLanguageAsync(LocalizationLanguage language) {
            string key = _ToKey(language);
            var so = await provider.GetAsync(key, AssetLoadMode.Addressable);

            if (so == null) {
                HLogger.Error(
                    $"[LocalizationManager] Addressable '{key}' 를 찾을 수 없습니다. " +
                    "Addressables Groups 에 해당 키로 LocalizationSO 를 등록했는지 확인하세요.");
                return false;
            }

            currentSO = so;
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
            string saved = PlayerPrefsHandler.GetString(PREFS_LANGUAGE_KEY, defaultLanguage.ToString());
            return Enum.TryParse<LocalizationLanguage>(saved, out LocalizationLanguage result) ? result : defaultLanguage;
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.07.03 HLocalization → HcupLocalization 패키지 개칭
 *
 * # 변경
 * - 네임스페이스: HLocalization → HcupLocalization
 * - PlayerPrefs 키 / Addressable 키 규약은 유지 — 기존 세이브·에셋 호환
 *
 * =============================================================================
 * @Jason - PKH 2026.05.13 HUI.TextUI 에서 HLocalization 으로 이전 — Enum 타입 전환
 *
 * # 변경
 * - 네임스페이스: HUI.TextUI → HLocalization
 * - string currentLanguageCode → LocalizationLanguage? currentLanguage (Nullable Enum)
 * - InitializeAsync(string) → InitializeAsync(LocalizationLanguage)
 * - SwitchLanguageAsync(string) → SwitchLanguageAsync(LocalizationLanguage)
 * - _LoadLanguageAsync(string) → _LoadLanguageAsync(LocalizationLanguage)
 * - _ToKey(string) → _ToKey(LocalizationLanguage)
 * - _LoadSavedLanguage() 추가 — PlayerPrefs 복원 시 Enum.TryParse 유효성 검증
 * - const DEFAULT_LANGUAGE = LocalizationLanguage.Korean (문자열 상수 제거)
 * - HCUP.HLocalization.asmdef 로 어셈블리 분리 (HCUP.HUI 에서 독립)
 *
 * # 설계 결정
 * - currentLanguage: LocalizationLanguage? — 초기화 전 미로드 상태를 null 로 표현
 *   (enum default 0 = Korean 과 "Korean 으로 초기화됨" 을 구분)
 * - SwitchLanguageAsync prevKey: currentLanguage.HasValue 조건부 — 초기화 전 호출 시
 *   존재하지 않는 키 해제 방지
 * - _LoadSavedLanguage(): PlayerPrefs 손상 / 잘못된 언어코드 저장 시 defaultLanguage 폴백
 *
 * =============================================================================
 * @Jason - PKH 2026.05.13 Phase 4 정적 검토 — _LoadLanguageAsync 실패 처리 버그 수정
 *
 * # 변경
 * - _LoadLanguageAsync 반환형 UniTask → UniTask<bool>
 * - InitializeAsync: loaded && HasValue 조건 후 RaiseLanguageChanged 호출
 * - SwitchLanguageAsync: loaded false 시 early return
 *
 * =============================================================================
 * @Jason - PKH 2026.05.13 최초 작성 (Phase 3)
 *
 * # 목적
 * - HCUP-2.1.0 Localization Phase 3 — LocalizationManager 구현
 * =============================================================================
 */
#endif

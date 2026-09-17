#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Unity 네이티브 Localization 의 언어 전환과 문자열 토큰 조회를 담당하는 싱글톤 매니저.
 *
 * 특징 ::
 * - SingletonBehaviour<T> 상속 - 매니저에 등록할 기본 언어를 인스펙터에서 지정한다
 * - 전역 진입점은 static 파사드 - SetLanguage / SetLanguageAsync / GetText
 * - 언어 전환 시점에 StringTable 을 1회 캐시한다. 이후 토큰 조회는 메모리 조회다
 *
 * 주의사항 ::
 * 1. 시작 언어 순서는 저장값 → 매니저 기본값 → 앱 언어다. 적용 언어가 바뀌면 그때마다 로컬에 저장한다.
 * 2. Import 가 만든 엔트리는 Smart 가 아니다. 이름 있는 자리표시자는 이 클래스가 직접 치환한다.
 * 3. 준비 전 조회는 값이 아니라 토큰 자체를 돌려주고 에러를 남긴다.
 * 4. 매니저가 없으면 시작 언어는 Startup Selector 가 정한다 (System → Specific). UI 텍스트는 그대로 동작한다.
 *
 * 사용 ::
 * - 전환: await UnityLocalizationManager.SetLanguageAsync(LocalizationLanguage.English)
 * - 조회: UnityLocalizationManager.GetText("some.token")
 * - 조합: UnityLocalizationManager.GetText("some.token", ("count", 3))
 * =========================================================
 */
#endif

using System;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using HCore;
using HcupLocalization;
using HDiagnosis.Logger;
using HInspector;

namespace HUnityLocalization {
    public sealed class UnityLocalizationManager : SingletonBehaviour<UnityLocalizationManager> {
        #region 상수
        const string DEFAULT_TABLE_NAME = "Localization";
        // HcupLocalization 갈래가 "LocalizationManager.Language" 를 쓰므로 키를 겹치지 않게 둔다.
        const string PREFS_LANGUAGE_KEY = "UnityLocalizationManager.Language";
        const char PLACEHOLDER_OPEN = '{';
        const char PLACEHOLDER_CLOSE = '}';
        #endregion

        #region 변수
        [HTitle("Localization")]
        [Tooltip("기본 언어를 매니저가 지정할지. 끄면 앱(기기) 언어를 쓴다")]
        [SerializeField]
        bool useDefaultLanguage;

        [Tooltip("useDefaultLanguage 가 켜진 경우 저장값이 없을 때 적용할 언어")]
        [SerializeField]
        LocalizationLanguage defaultLanguage = LocalizationLanguage.Korean;

        [SerializeField]
        string tableName = DEFAULT_TABLE_NAME;

        StringTable table;
        string cachedLocaleCode;
        LocalizationLanguage? appliedLanguage;
        #endregion

        #region Events
        /// <summary> 언어 전환이 실제로 적용된 뒤 1회 발화. 언어에 매인 리소스(폰트 등)가 구독한다. </summary>
        public static event Action<LocalizationLanguage> LanguageChanged;
        #endregion

        #region Getter/Setter
        /// <summary> 토큰 조회가 가능한 상태인지. 테이블 캐시가 끝나야 true. </summary>
        public static bool IsReady => HasInstance && instance.table != null;

        /// <summary> 매니저에 등록된 기본 언어. useDefaultLanguage 가 꺼져 있으면 null. </summary>
        public LocalizationLanguage? DefaultLanguage => useDefaultLanguage ? (LocalizationLanguage?)defaultLanguage : null;

        /// <summary> 현재 적용된 언어. 초기화 전이면 null. </summary>
        public LocalizationLanguage? AppliedLanguage => appliedLanguage;

        public string TableName => tableName;
        #endregion


        #region Protected - Lifecycle
        protected override void Awake() {
            base.Awake();
            // 중복 인스턴스는 base 가 파괴를 예약한다. 그 개체가 이벤트를 구독하면 해제 주체가 어긋난다.
            if (instance != this) return;

            LocalizationSettings.SelectedLocaleChanged += _OnSelectedLocaleChanged;
        }

        protected override void OnDestroy() {
            // base.OnDestroy() 가 instance 를 비우므로 소유자 판정을 먼저 캡처한다.
            bool isOwner = instance == this;
            base.OnDestroy();
            if (!isOwner) return;

            LocalizationSettings.SelectedLocaleChanged -= _OnSelectedLocaleChanged;
            table = null;
            cachedLocaleCode = null;
        }

        void Start() {
            _InitializeAsync().Forget();
        }
        #endregion


        #region Public - Static 파사드
        /// <summary> 언어를 전환한다. 완료를 기다리지 않는 호출 경로. </summary>
        public static void SetLanguage(LocalizationLanguage language) {
            if (!_TryGetManager(out UnityLocalizationManager manager)) return;

            manager._ApplyLanguageAsync(language).Forget();
        }

        /// <summary> 언어를 전환하고 테이블 준비까지 기다린다. 성공 여부를 반환한다. </summary>
        public static UniTask<bool> SetLanguageAsync(LocalizationLanguage language) {
            if (!_TryGetManager(out UnityLocalizationManager manager)) return UniTask.FromResult(false);

            return manager._ApplyLanguageAsync(language);
        }

        /// <summary> 토큰의 현재 언어 문자열. 자리표시자는 치환하지 않는다. 실패 시 토큰을 그대로 반환. </summary>
        public static string GetText(string token) {
            return _TryGetValue(token, out string value) ? value : token;
        }

        /// <summary> 이름 있는 자리표시자를 값으로 치환한 문자열. ("count", 3) 이면 {count} 가 3 으로 바뀐다. </summary>
        public static string GetText(string token, params (string name, object value)[] values) {
            if (!_TryGetValue(token, out string source)) return token;

            return _Compose(source, values);
        }
        #endregion


        #region Private - 전환
        /// <summary> 언어를 적용한다. 적용 언어가 바뀌면 _SetAppliedLanguage 가 그 값을 로컬에 저장한다. </summary>
        private async UniTask<bool> _ApplyLanguageAsync(LocalizationLanguage language) {
            if (!LocaleCodeMap.TryGetSystemLanguage(language, out SystemLanguage systemLanguage)) {
                HLogger.Error(
                    $"[UnityLocalizationManager] Language '{language}' has no SystemLanguage mapping. "
                    + "Add the language to LocaleCodeMap.");
                return false;
            }

            // Locale 목록은 Addressables 로 로드된다. 초기화 전에 읽으면 WebGL 에서 빈 목록이 온다.
            await LocalizationSettings.InitializationOperation.Task;

            Locale locale = LocalizationSettings.AvailableLocales.GetLocale(systemLanguage);
            if (locale == null) {
                HLogger.Error(
                    $"[UnityLocalizationManager] No Locale asset for '{language}' ({systemLanguage}). "
                    + "Run the HUnityLocalization import and check the Locale assets are registered in Addressables.");
                return false;
            }

            // Locale 대입보다 테이블 캐시를 먼저 한다. 대입이 부르는 SelectedLocaleChanged 핸들러가
            // 같은 Locale 을 다시 로드하지 않게 되고, UI 갱신 시점에 테이블이 이미 준비된다.
            if (!await _CacheTableAsync(locale)) return false;

            _SetAppliedLanguage(language);
            LocalizationSettings.SelectedLocale = locale;
            LanguageChanged?.Invoke(language);
            return true;
        }

        /// <summary> 시작 언어를 정한다. 순서는 저장값 → 매니저 기본값 → 앱 언어다. </summary>
        private async UniTask _InitializeAsync() {
            await LocalizationSettings.InitializationOperation.Task;

            // 1. 사람이 한 번 고른 언어가 최우선이다. OS 언어가 바뀌어도 그 선택을 덮지 않는다.
            if (_TryGetSavedLanguage(out LocalizationLanguage savedLanguage)) {
                await _ApplyLanguageAsync(savedLanguage);
                return;
            }

            // 2. 매니저에 등록된 기본값. 적용과 동시에 저장되므로 다음 실행부터 1번 경로로 들어온다.
            if (useDefaultLanguage) {
                await _ApplyLanguageAsync(defaultLanguage);
                return;
            }

            // 3. 기본값을 쓰지 않으면 앱(기기) 언어를 쓴다. 이 값도 적용 시점에 저장된다.
            if (_TryGetAppDefaultLanguage(out LocalizationLanguage appLanguage)) {
                await _ApplyLanguageAsync(appLanguage);
                return;
            }

            // 앱 언어가 매핑에 없다. 이때는 강제하지 않고 Startup Selector 가 고른 Locale 을 그대로 쓴다.
            Locale locale = LocalizationSettings.SelectedLocale;
            if (locale == null) {
                HLogger.Error(
                    "[UnityLocalizationManager] No locale is selected after initialization. "
                    + "Check the Startup Locale Selectors in the Localization Settings asset.");
                return;
            }

            _SyncAppliedLanguage(locale);
            await _CacheTableAsync(locale);
        }

        private void _OnSelectedLocaleChanged(Locale locale) {
            if (locale == null) return;

            _SyncAppliedLanguage(locale);
            _CacheTableAsync(locale).Forget();
        }

        /// <summary> 외부에서 Locale 이 바뀐 경우 적용값을 실제 Locale 과 맞춘다. </summary>
        private void _SyncAppliedLanguage(Locale locale) {
            if (!LocaleCodeMap.TryGetLanguage(locale.Identifier, out LocalizationLanguage language)) {
                HLogger.Error(
                    $"[UnityLocalizationManager] Selected locale '{locale.Identifier.Code}' has no LocalizationLanguage mapping. "
                    + "Add the mapping to LocaleCodeMap so the applied language can be reported.");
                return;
            }

            _SetAppliedLanguage(language);
        }
        #endregion


        #region Private - 저장과 기본값
        /// <summary> 로컬에 저장된 언어. 저장값이 없거나 유효하지 않으면 false. </summary>
        private static bool _TryGetSavedLanguage(out LocalizationLanguage language) {
            language = default;
            // GetString 은 키가 없으면 기본값을 그대로 저장한다. 첫 실행을 구분하려면 HasKey 를 먼저 본다.
            if (!PlayerPrefsHandler.HasKey(PREFS_LANGUAGE_KEY)) return false;

            string saved = PlayerPrefsHandler.GetString(PREFS_LANGUAGE_KEY);
            // TryParse 는 범위를 벗어난 숫자 문자열도 통과시키므로 IsDefined 로 한 번 더 막는다.
            if (Enum.TryParse(saved, out language) && Enum.IsDefined(typeof(LocalizationLanguage), language)) return true;

            language = default;
            HLogger.Error(
                $"[UnityLocalizationManager] Saved language '{saved}' is not a valid LocalizationLanguage. "
                + "The manager default is used instead. The stored value is rewritten on the next change.");
            return false;
        }

        /// <summary> 앱(기기) 언어를 LocalizationLanguage 로 환산한다. 매핑이 없으면 false. </summary>
        private static bool _TryGetAppDefaultLanguage(out LocalizationLanguage language) {
            return LocaleCodeMap.TryGetLanguage(new LocaleIdentifier(Application.systemLanguage), out language);
        }

        /// <summary> 적용 언어를 갱신한다. 값이 바뀐 경우에만 로컬에 저장한다. </summary>
        private void _SetAppliedLanguage(LocalizationLanguage language) {
            // SetString 이 매 호출마다 PlayerPrefs.Save 를 부르므로 같은 값이면 쓰지 않는다.
            if (appliedLanguage.HasValue && appliedLanguage.Value == language) return;

            appliedLanguage = language;
            PlayerPrefsHandler.SetString(PREFS_LANGUAGE_KEY, language.ToString());
        }
        #endregion


        #region Private - 테이블 캐시
        private async UniTask<bool> _CacheTableAsync(Locale locale) {
            if (table != null && cachedLocaleCode == locale.Identifier.Code) return true;

            AsyncOperationHandle<StringTable> handle = LocalizationSettings.StringDatabase.GetTableAsync(tableName, locale);
            await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null) {
                table = null;
                cachedLocaleCode = null;
                HLogger.Error(
                    $"[UnityLocalizationManager] Failed to load string table '{tableName}' for locale '{locale.Identifier.Code}'. "
                    + "Check the table collection name matches the name used by the import.");
                return false;
            }

            table = handle.Result;
            cachedLocaleCode = locale.Identifier.Code;
            return true;
        }
        #endregion


        #region Private - 조회
        private static bool _TryGetManager(out UnityLocalizationManager manager) {
            manager = Instance;
            if (manager != null) return true;

            HLogger.Error(
                "[UnityLocalizationManager] No manager exists in the loaded scenes. "
                + "Place the localization manager prefab in the startup scene before calling the global API.");
            return false;
        }

        private static bool _TryGetValue(string token, out string value) {
            value = null;
            if (string.IsNullOrEmpty(token)) {
                HLogger.Error(
                    "[UnityLocalizationManager] An empty token was requested. "
                    + "Pass the string key defined in the localization excel.");
                return false;
            }

            return _TryGetManager(out UnityLocalizationManager manager) && manager._TryGetEntryValue(token, out value);
        }

        private bool _TryGetEntryValue(string token, out string value) {
            value = null;
            if (table == null) {
                HLogger.Error(
                    $"[UnityLocalizationManager] Table '{tableName}' is not loaded yet, so token '{token}' cannot be resolved. "
                    + "Await SetLanguageAsync, or request the token after the manager has started.");
                return false;
            }

            StringTableEntry entry = table.GetEntry(token);
            if (entry == null) {
                HLogger.Error(
                    $"[UnityLocalizationManager] Token '{token}' does not exist in table '{tableName}'. "
                    + "Add the row to the localization excel and run the import.");
                return false;
            }

            value = entry.LocalizedValue;
            if (!string.IsNullOrEmpty(value)) return true;

            HLogger.Error(
                $"[UnityLocalizationManager] Token '{token}' has an empty value for '{appliedLanguage}'. "
                + "Fill the language column in the localization excel and run the import.");
            return false;
        }

        /// <summary> 이름 있는 자리표시자를 치환한다. Import 엔트리는 Smart 가 아니라 string.Format 을 쓸 수 없다. </summary>
        private static string _Compose(string source, (string name, object value)[] values) {
            if (values == null || values.Length == 0) return source;

            StringBuilder builder = new StringBuilder(source);
            for (int k = 0; k < values.Length; k++) {
                string name = values[k].name;
                if (string.IsNullOrEmpty(name)) {
                    HLogger.Error(
                        "[UnityLocalizationManager] A placeholder name is empty and was skipped. "
                        + "Pass pairs such as (\"count\", 3).");
                    continue;
                }

                string replacement = values[k].value == null ? string.Empty : values[k].value.ToString();
                builder.Replace($"{PLACEHOLDER_OPEN}{name}{PLACEHOLDER_CLOSE}", replacement);
            }
            return builder.ToString();
        }
        #endregion


#if UNITY_EDITOR
        #region Debug
        // 인스펙터에서 기본 언어를 바꾸면 플레이 중에는 즉시 전환하고 저장한다. 사람이 고른 것과 같게 취급한다.
        void OnValidate() {
            if (!Application.isPlaying) return;
            if (instance != this) return;
            if (!useDefaultLanguage) return;
            if (appliedLanguage.HasValue && appliedLanguage.Value == defaultLanguage) return;

            _ApplyLanguageAsync(defaultLanguage).Forget();
        }
        #endregion
#endif
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.09.17 직렬화 필드의 Nullable 제거 - 인스펙터 무한 정지 수정
 *
 * # 변경
 * - defaultLanguage(LocalizationLanguage?) → useDefaultLanguage(bool) + defaultLanguage(LocalizationLanguage) 두 필드.
 * - LocalizationLanguage? 는 DefaultLanguage 프로퍼티의 반환형으로만 남긴다. 직렬화·드로잉 대상이 아니라 안전하다.
 *
 * # 이유
 * - Odin 환경에서 이 컴포넌트를 인스펙터에 그리면 OnInspectorGUI 에서 빠져나오지 못해 에디터가 멈췄다.
 *   직전 유일한 변경이 "직렬화되는 Nullable<enum> 필드 추가" 였다.
 * - 같은 클래스의 appliedLanguage 는 그전부터 Nullable 이었지만 [SerializeField] 가 없어 그려지지 않으며,
 *   그 시점의 인스펙터는 정상이었다. 필드를 직렬화 대상으로 만든 직후부터 정지가 재현됐다.
 * - HInspectorToOdinBridge 는 HTitle 을 Odin TitleAttribute 로 번역하는 순수 속성 매퍼다. 그리기 루프도
 *   타입 분기도 없어 원인이 아니다. 남는 경로는 Odin 이 직렬화된 Nullable 멤버를 인스펙터 트리에 올리는 쪽이다.
 * - HCUP 전체에 Nullable 직렬화 필드를 인스펙터에 그린 선례가 사실상 없다. 밟히지 않은 경로였다.
 *
 * # 결과
 * - 인스펙터에 토글 하나와 언어 enum 하나가 보인다. 토글이 꺼져 있으면 시작 언어는 앱(기기) 언어다.
 *
 * # 주의
 * - 직렬화 필드에 Nullable<T> 를 쓰지 않는다. 선택적 값은 동반 bool 로 표현한다.
 * - 기존 프리팹·씬에 남은 targetLanguage / defaultLanguage 직렬화 값은 이어지지 않는다. 다시 지정해야 한다.
 *
 * =============================================================================
 * @Jason - PKH 2026.09.17 시작 언어 결정 순서 도입 + PlayerPrefs 저장
 *
 * # 변경
 * - targetLanguage(LocalizationLanguage) → defaultLanguage(LocalizationLanguage?) 로 교체.
 *   인스펙터 필드의 의미를 "현재값 겸 컨트롤" 에서 "매니저에 등록된 기본값" 으로 좁혔다.
 * - 현재 적용 언어는 appliedLanguage 가 단독으로 갖는다. 프로퍼티도 DefaultLanguage / AppliedLanguage 로 갈랐다.
 * - _InitializeAsync 가 시작 언어를 정한다. 저장값 → 매니저 기본값(저장 동반) → 앱 언어 → Startup Selector 결과.
 * - 적용 경로는 _ApplyLanguageAsync 하나다. appliedLanguage 를 바꾸는 _SetAppliedLanguage 가 저장까지 맡는다.
 * - _TryGetSavedLanguage / _TryGetAppDefaultLanguage / _SetAppliedLanguage 추가. 키는 PREFS_LANGUAGE_KEY.
 *
 * # 이유
 * - 종전 설계는 Start 에서 언어를 강제하지 않고 Startup Selector 결과만 따라갔다(아래 최초 작성 항목).
 *   저장된 선택을 복원하는 주체가 없어 세션을 넘기면 사용자의 선택이 사라졌다.
 *   PlayerPrefLocaleSelector 는 XML 주석과 달리 PostInitialization 에서 1회만 기록하므로 대체가 되지 않는다.
 * - defaultLanguage 를 nullable 로 둔 것은 "기본값 미설정" 이 표현되어야 3단 순서의 마지막 갈래(앱 언어)로
 *   내려갈 수 있기 때문이다. enum 필드는 항상 0번 항목을 값으로 가져 미설정을 표현하지 못한다.
 * - 저장 지점을 appliedLanguage 대입 한 곳에 붙였다. 언어가 바뀌는 입구가 3개(명시 전환 / 초기화 복원 /
 *   외부 Locale 변경)라 입구마다 저장을 부르면 새 입구가 생길 때 빠뜨린다. 상태 변경 지점에 붙이면 누락이
 *   구조적으로 불가능하다. 같은 값이면 쓰지 않는 것은 SetString 이 매번 PlayerPrefs.Save 를 부르기 때문이다.
 *
 * # 결과
 * - 시작 시 인스펙터 값이 실제 Locale 로 덮이는 현상이 사라졌다. 종전에는 _SyncTargetLanguage 가 필드를 썼다.
 * - 저장 키가 HcupLocalization 의 "LocalizationManager.Language" 와 겹치지 않는다.
 *
 * # 주의
 * - PlayerPrefsHandler.GetString 은 키가 없으면 기본값을 즉시 저장한다. 그래서 HasKey 로 첫 실행을 먼저 가른다.
 *   순서를 바꾸면 "사람이 고른 적 없음" 상태가 첫 조회에서 소멸한다.
 * - 미설정은 useDefaultLanguage(bool)로 표현한다. 직렬화 필드에 Nullable 을 쓰지 않는다. 이유는 아래 항목 참조.
 * - 필드 이름이 바뀌었으므로 기존 씬/프리팹에 직렬화된 targetLanguage 값은 이어지지 않는다. 다시 지정해야 한다.
 * - 첫 실행 이후에는 저장값이 항상 존재하므로 OS 언어를 바꿔도 앱 언어는 따라 바뀌지 않는다. 저장값이
 *   최우선이라는 규칙의 직접적 결과다. 기기 언어를 다시 따르게 하려면 저장 키를 지워야 한다.
 *
 * =============================================================================
 * @Jason - PKH 2026.09.17 최초 작성
 *
 * # 목적
 * - Unity 네이티브 Localization 의 언어 전환을 단일 경로로 모은다. 인스펙터와 코드 양쪽에서 같은 경로를 쓴다.
 * - 코드에서 문자열 토큰을 요청하는 경로를 제공한다. 동적 값이 끼는 문자열은 UI 배선만으로 만들 수 없다.
 *
 * # 설계 결정
 * - 이름이 LocalizationManager 가 아니라 UnityLocalizationManager 다. HcupLocalization 에 같은 이름의
 *   매니저가 이미 있고 두 어셈블리 모두 autoReferenced 라, 같은 파일에서 두 네임스페이스를 함께 쓰면
 *   CS0104(모호한 참조)가 난다. 타입 이름 자체로 갈래를 드러내 충돌을 없앤다.
 * - Start 에서 언어를 강제하지 않는다. 시작 Locale 은 Localization Settings 의 Startup Selector 가 정하고
 *   (CommandLine → System → Specific), 매니저는 그 결과의 테이블을 캐시하는 쪽만 맡는다. 강제하면
 *   시스템 언어 감지가 인스펙터 값에 덮인다.
 * - 조회는 LocalizedStringDatabase 의 동기 API 를 쓰지 않는다. 그 경로는 AsyncOperationUtility.SynchronousLoad 를
 *   타고, 미로드 핸들에서는 WaitForCompletion 으로 떨어진다. WebGL 이 이를 지원하지 않는다.
 *   대신 전환 시 StringTable 을 캐시하고 GetEntry 로 메모리 조회한다.
 * - 전환 순서는 "테이블 캐시 → SelectedLocale 대입" 이다. 역순이면 대입이 부른 SelectedLocaleChanged
 *   핸들러와 전환 로직이 같은 테이블을 이중으로 로드한다. 캐시 키(cachedLocaleCode)로 멱등을 보장한다.
 * - 자리표시자 치환을 자체 구현한다. Import 는 SmartFormatTag 를 심지 않아 모든 엔트리가 비 Smart 이고,
 *   비 Smart 엔트리는 string.Format 규칙을 따른다. 이름 있는 자리표시자에 인자를 넘기면 FormatException 이
 *   나고, 넘기지 않으면 중괄호가 그대로 출력된다. 그래서 StringBuilder.Replace 로 직접 치환한다.
 *
 * # 주의
 * - 이 어셈블리는 HUnityLocalization 최초의 런타임 코드다. 종전까지 HUnityLocalization 은 에디터 전용이었다.
 * - 조회 실패는 전부 토큰 문자열 반환 + 에러 로그다. 조용한 빈 문자열을 만들지 않는다.
 *
 * =============================================================================
 */
#endif

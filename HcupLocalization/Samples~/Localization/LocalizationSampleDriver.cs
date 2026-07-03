/*
 * =========================================================
 * HCUP-2.1.0 Localization — End-to-End 샘플 드라이버
 *
 * 목적 ::
 * LocalizationManager 초기화·언어 전환의 올바른 호출 패턴을 보여주는 참조 구현.
 * Samples~ 폴더이므로 Unity에서 컴파일되지 않습니다. 실제 실험은 05_Study 씬에서 수행하세요.
 *
 * Unity Editor 사전 설정 체크리스트 ::
 * 1. NPOI 로더: HCUP/Windows/Data Editor Window → "00. Localization"
 *    - Excel 파일 할당 (컬럼: UID | Korean | English | Japanese | Chinese | Russian)
 *    - dataOutputPath 지정 (예: Assets/Data/Localization)
 *    - Import 버튼 클릭 → Localization_Korean.asset 등 5개 SO 생성 확인
 * 2. Addressables 등록: Window → Asset Management → Addressables → Groups
 *    - 5개 SO 에셋을 각각 키 "Localization_Korean" 등으로 등록
 * 3. 씬 설정:
 *    - LocalizationManager GameObject 배치 → dontDestroyOnLoad 체크
 *    - TextMeshProUGUI에 HTmpLocalizerAddon 부착 → localizationId에 UID 입력
 * =========================================================
 */

using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using HUI.TextUI;
using HcupLocalization;

namespace HcupLocalization.Samples {
    /// <summary>
    /// 씬 부트스트랩 예시 — 실제 게임 프로젝트에서는 SceneManager 또는 GameManager가 담당합니다.
    /// </summary>
    public class LocalizationSampleDriver : MonoBehaviour {
        [SerializeField] Button koreanBtn;
        [SerializeField] Button englishBtn;
        [SerializeField] Button japaneseBtn;

        async void Start() {
            // 1. 초기화 — PlayerPrefs 에 저장된 언어로 복원, 없으면 Korean 기본값
            await LocalizationManager.Instance.InitializeAsync(LocalizationLanguage.Korean);

            // 2. 버튼 이벤트 연결
            koreanBtn?.onClick.AddListener(  () => _SwitchAsync(LocalizationLanguage.Korean).Forget());
            englishBtn?.onClick.AddListener( () => _SwitchAsync(LocalizationLanguage.English).Forget());
            japaneseBtn?.onClick.AddListener(() => _SwitchAsync(LocalizationLanguage.Japanese).Forget());
        }

        async UniTaskVoid _SwitchAsync(LocalizationLanguage language) {
            await LocalizationManager.Instance.SwitchLanguageAsync(language);
            // → HTmpLocalizerAddon / HTextLocalizerAddon 이 OnLanguageChanged 수신 후 자동 갱신
        }
    }

    /// <summary>
    /// HTextLocalizer.GetText 직접 호출 예시 (동적 텍스트가 필요한 경우).
    /// Addon 컴포넌트 없이 UID 를 직접 번역해야 할 때 사용합니다.
    /// </summary>
    public class LocalizationDirectUsageExample : MonoBehaviour {
        void _ShowDynamicMessage(string uid, string playerName) {
            // UID "MSG.WELCOME" → "{0}님, 환영합니다!" (Korean)
            string template = HTextLocalizer.GetText?.Invoke(uid) ?? uid;
            string message  = string.Format(template, playerName);
            Debug.Log(message);
        }
    }
}

/*
 * =========================================================
 * End-to-End 검증 체크리스트 (Unity Editor에서 직접 수행)
 * =========================================================
 *
 * [엑셀 Import → SO 생성]
 * □ Data Editor Window 에서 Import 실행
 * □ Assets/Data/Localization/ 에 5개 SO 생성 확인
 * □ SO Inspector 에서 HDictionary 항목 수·UID 값 확인
 * □ 중복 UID 포함 엑셀 → Error 로그 + Import 중단 확인
 * □ 빈 UID 포함 엑셀 → Error 로그 + Import 중단 확인
 *
 * [LocalizationManager 초기화]
 * □ 씬 진입 → Console 에서 LocalizationTableLoader 로드 로그 확인
 * □ HTmpLocalizerAddon 부착 텍스트가 UID 대신 번역 문자열 표시 확인
 * □ PlayerPrefs 없는 첫 실행 → LocalizationLanguage.Korean 으로 초기화 확인
 *
 * [언어 전환]
 * □ English 버튼 클릭 → 모든 HTmpLocalizerAddon 텍스트 즉시 갱신 확인
 * □ 앱 재시작 후 마지막 언어("English")로 복원 확인
 * □ 없는 언어코드 PlayerPrefs 저장값 → _LoadSavedLanguage 폴백 → Korean 초기화 확인
 *
 * [Export]
 * □ Data Editor Window → Export 버튼 → xlsx 저장 확인
 * □ 저장된 xlsx 를 다시 Import → 동일 항목 수 유지 확인
 * =========================================================
 */

/*
 * =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.07.03 HcupLocalization 개칭 반영
 *
 * # 변경
 * - 네임스페이스: HLocalization → HcupLocalization (패키지 개칭 추종)
 * =============================================================================
 */

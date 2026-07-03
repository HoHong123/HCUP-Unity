#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 언어별 번역 데이터를 저장하는 ScriptableObject.
 *
 * 특징 ::
 * - language 필드로 언어 식별 (LocalizationLanguage 열거형)
 * - HDictionary<string, string> — UID → 번역 문자열 캐시
 * - 변경 API (SetLanguageCode / SetEntry / ClearTable) #if UNITY_EDITOR 가드
 *
 * 주의사항 ::
 * - 파일명 포맷: Localization_{language} (예: Localization_Korean)
 * - 런타임 읽기 전용 — Import/Export는 LocalizationTableLoader 경유
 *
 * 사용 ::
 * - 조회: so.TryGetText(uid, out string text)
 * - 에디터 생성: LocalizationTableLoader.ImportData()
 * =========================================================
 */
#endif

using UnityEngine;
using HCollection;

namespace HLocalization {
    [CreateAssetMenu(fileName = "Localization_Language", menuName = "HCUP/Localization/LocalizationSO")]
    public class LocalizationSO : ScriptableObject {
        [SerializeField]
        LocalizationLanguage language;
        [SerializeField]
        HDictionary<string, string> table = new();

        public LocalizationLanguage Language => language;
        public bool TryGetText(string uid, out string text) => table.TryGetValue(uid, out text);

#if UNITY_EDITOR
        public void SetLanguageCode(LocalizationLanguage lang) { language = lang; }
        public bool SetEntry(string uid, string text) => table.TryAddOrReplace(uid, text);
        public void ClearTable() { table.Clear(); }
        public System.Collections.Generic.IEnumerable<string> GetUIDs() => table.Keys;
        public string GetRawText(string uid) => table.TryGetValue(uid, out string t) ? t : "";
#endif
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.13 HUI.TextUI 에서 HLocalization 으로 이전 — Enum 타입 전환
 *
 * # 변경
 * - 네임스페이스: HUI.TextUI → HLocalization
 * - string languageCode 필드 → LocalizationLanguage language (Enum)
 * - string LanguageCode 프로퍼티 → LocalizationLanguage Language
 * - SetLanguageCode(string) → SetLanguageCode(LocalizationLanguage)
 * - HCUP.HLocalization.asmdef 로 어셈블리 분리 (HCUP.HUI 에서 독립)
 *
 * =============================================================================
 * @Jason - PKH 2026.05.13 최초 작성 (Phase 1)
 *
 * # 목적
 * - HCUP-2.1.0 Localization Phase 1 — LocalizationSO 구현
 * =============================================================================
 */
#endif

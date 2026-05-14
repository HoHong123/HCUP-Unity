#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 언어별 번역 데이터를 저장하는 ScriptableObject.
 *
 * 특징 ::
 * - language 필드로 언어 식별 (LocalizationLanguage 열거형)
 * - SerializableDictionary<string, string> — UID → 번역 문자열 캐시
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
using HUtil.Collection;

namespace HLocalization {
    [CreateAssetMenu(fileName = "Localization_Language", menuName = "HCUP/Localization/LocalizationSO")]
    public class LocalizationSO : ScriptableObject {
        [SerializeField]
        LocalizationLanguage language;
        [SerializeField]
        SerializableDictionary<string, string> table = new();

        public LocalizationLanguage Language => language;
        public bool TryGetText(string uid, out string text) => table.TryGetValue(uid, out text);

#if UNITY_EDITOR
        public void SetLanguageCode(LocalizationLanguage lang) { language = lang; }
        public bool SetEntry(string uid, string text) { table.Add(uid, text); return true; }
        public void ClearTable() { table.Clear(); }
        public System.Collections.Generic.IEnumerable<string> GetUIDs() => table.Dictionary.Keys;
        public string GetRawText(string uid) => table.TryGetValue(uid, out string t) ? t : "";
#endif
    }
}

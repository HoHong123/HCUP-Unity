#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * LocalizationLanguage 와 Unity 언어 식별자 사이의 매핑 단일 소스.
 *
 * 특징 ::
 * - Locale 생성·조회 시 언어 식별의 유일한 기준 (매직 값 금지)
 * - 정방향(언어 → SystemLanguage) 과 역방향(LocaleIdentifier → 언어) 을 함께 제공
 * - Chinese 는 간체(ChineseSimplified, zh-Hans) 채택 - 번체 필요 시 enum 확장으로 대응
 *
 * 주의사항 ::
 * - 역방향 비교 기준은 LocaleIdentifier(SystemLanguage).Code 다. Import 가 Locale 을 만든 방식과 같다.
 *
 * 사용 ::
 * - LocaleCodeMap.TryGetSystemLanguage(LocalizationLanguage.Korean, out SystemLanguage lang)
 * - LocaleCodeMap.TryGetLanguage(locale.Identifier, out LocalizationLanguage language)
 * =========================================================
 */
#endif

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using HcupLocalization;

namespace HUnityLocalization {
    public static class LocaleCodeMap {
        static readonly Dictionary<LocalizationLanguage, SystemLanguage> systemLanguageMap = new() {
            { LocalizationLanguage.Korean,   SystemLanguage.Korean },
            { LocalizationLanguage.English,  SystemLanguage.English },
            { LocalizationLanguage.Japanese, SystemLanguage.Japanese },
            { LocalizationLanguage.Chinese,  SystemLanguage.ChineseSimplified },
            { LocalizationLanguage.Russian,  SystemLanguage.Russian },
        };

        /// <summary> 매핑된 SystemLanguage 반환. 매핑 없으면 false. </summary>
        public static bool TryGetSystemLanguage(LocalizationLanguage language, out SystemLanguage systemLanguage)
            => systemLanguageMap.TryGetValue(language, out systemLanguage);

        /// <summary> Locale 식별자에 해당하는 언어 반환. 일치하는 코드가 없으면 false. </summary>
        public static bool TryGetLanguage(LocaleIdentifier identifier, out LocalizationLanguage language) {
            foreach (KeyValuePair<LocalizationLanguage, SystemLanguage> pair in systemLanguageMap) {
                if (new LocaleIdentifier(pair.Value).Code != identifier.Code) continue;

                language = pair.Key;
                return true;
            }

            language = default;
            return false;
        }
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.09.17 Runtime 어셈블리로 이동 + 역방향 매핑 추가
 *
 * # 변경
 * - 파일 위치: Editor/HUnityLocalization → Runtime/HUnityLocalization
 *   (어셈블리도 HCUP.HUnityLocalization.Editor → HCUP.HUnityLocalization)
 * - TryGetLanguage(LocaleIdentifier, out LocalizationLanguage) 추가
 *
 * # 이유
 * - 런타임 언어 전환 매니저가 같은 매핑을 필요로 한다. Editor 어셈블리에 있으면 참조가 불가해
 *   런타임 쪽에 매핑이 복제된다. 단일 소스를 유지하려면 런타임으로 내리는 것이 옳다.
 * - 역방향은 "현재 선택된 Locale 이 어느 언어인가" 를 알기 위한 것이다. 이것이 없으면 매니저의
 *   인스펙터 표시값이 실제 선택 Locale 과 어긋난 채로 남는다.
 *
 * # 결과
 * - Editor 어셈블리는 HCUP.HUnityLocalization 을 참조해 종전과 같이 사용한다.
 *
 * # 주의
 * - 역방향은 선형 탐색이다. 항목이 5개라 비용이 무의미하므로 역방향 딕셔너리를 따로 두지 않았다.
 *
 * =============================================================================
 * @Jason - PKH 2026.07.03 최초 작성
 *
 * # 목적
 * - HCUP-2.1.0 HUnityLocalization - Locale 코드 매핑 단일 소스
 * - ko / en / ja / zh-Hans / ru (LocaleIdentifier(SystemLanguage) 경유 자동 결정)
 *
 * =============================================================================
 */
#endif

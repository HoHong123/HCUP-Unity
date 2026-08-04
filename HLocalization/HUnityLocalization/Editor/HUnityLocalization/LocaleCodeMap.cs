#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * LocalizationLanguage → Unity SystemLanguage 매핑 단일 소스.
 *
 * 특징 ::
 * - Locale 생성·조회 시 언어 식별의 유일한 기준 (매직 값 금지)
 * - Chinese 는 간체(ChineseSimplified, zh-Hans) 채택 — 번체 필요 시 enum 확장으로 대응
 *
 * 사용 ::
 * - LocaleCodeMap.TryGetSystemLanguage(LocalizationLanguage.Korean, out var lang)
 * =========================================================
 */
#endif

using System.Collections.Generic;
using UnityEngine;
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
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.07.03 최초 작성
 *
 * # 목적
 * - HCUP-2.1.0 HUnityLocalization — Locale 코드 매핑 단일 소스
 * - ko / en / ja / zh-Hans / ru (LocaleIdentifier(SystemLanguage) 경유 자동 결정)
 *
 * =============================================================================
 */
#endif

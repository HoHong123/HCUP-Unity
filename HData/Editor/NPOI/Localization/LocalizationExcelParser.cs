#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 로컬리제이션 엑셀 병합 JArray 를 행 DTO 목록으로 파싱·검증하는 공용 파서.
 *
 * 특징 ::
 * - HcupLocalizationTableLoader / HUnityLocalizationTableLoader 가 공용 호출
 * - UID 전역 검증 — 빈 UID / 중복 UID 발견 시 null 반환 (Import 중단 신호)
 *
 * 주의사항 ::
 * - 반환 null = 검증 실패. 에러 로그는 내부에서 발화하므로 호출부는 return 만.
 * =========================================================
 */
#endif

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using HDiagnosis.Logger;
using HcupLocalization;

namespace HData.NPOI.Localization {
    public static class LocalizationExcelParser {
        /// <summary> 병합 JArray를 LocalizationData 목록으로 파싱. 검증 실패 시 null. </summary>
        public static List<LocalizationData> Parse(JArray merged) {
            if (merged == null || merged.Count == 0) {
                HLogger.Error("[LocalizationExcelParser] 유효한 시트 데이터가 없습니다. 컬럼(UID/Korean/…)을 확인하세요.");
                return null;
            }

            var uidSet = new HashSet<string>(StringComparer.Ordinal);
            var dataList = new List<LocalizationData>(merged.Count);

            foreach (JObject row in merged) {
                string uid = row["UID"]?.Value<string>();
                if (string.IsNullOrWhiteSpace(uid)) {
                    HLogger.Error("[LocalizationExcelParser] 빈 UID 발견. Import를 중단합니다.");
                    return null;
                }
                if (!uidSet.Add(uid)) {
                    HLogger.Error($"[LocalizationExcelParser] 중복 UID '{uid}' 발견. Import를 중단합니다.");
                    return null;
                }
                dataList.Add(new LocalizationData {
                    uid      = uid,
                    korean   = row[nameof(LocalizationLanguage.Korean)]?.Value<string>()   ?? "",
                    english  = row[nameof(LocalizationLanguage.English)]?.Value<string>()  ?? "",
                    japanese = row[nameof(LocalizationLanguage.Japanese)]?.Value<string>() ?? "",
                    chinese  = row[nameof(LocalizationLanguage.Chinese)]?.Value<string>()  ?? "",
                    russian  = row[nameof(LocalizationLanguage.Russian)]?.Value<string>()  ?? ""
                });
            }
            return dataList;
        }
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.07.03 최초 작성
 *
 * # 목적
 * - HCUP-2.1.0 HUnityLocalization — 두 로더의 공용 파싱·UID 검증 로직 추출
 * - 기존 LocalizationTableLoader.ImportData() 내 블록을 무변경 이동 (로그 접두만 변경)
 *
 * =============================================================================
 */
#endif

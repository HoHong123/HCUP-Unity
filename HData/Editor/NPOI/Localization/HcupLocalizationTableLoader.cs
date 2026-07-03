#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 로컬리제이션 엑셀을 파싱해 언어별 LocalizationSO 를 생성·갱신하는 NPOI Loader.
 *
 * 특징 ::
 * - ExcelLoader<T> 상속 — 엑셀 파일·시트 선택, Import/Export UI 는 ExcelLoaderEditor 가 담당
 * - ExcelToJsonAllSheets() 로 전체 시트 병합 → 언어당 단일 마스터 SO 생성
 * - UID 전역 중복 검사 — 발견 즉시 Import 중단 (데이터 일관성 보장)
 *
 * 주의사항 ::
 * - dataOutputPath 설정 필수 — 미설정 시 Import/Export 중단
 * - 엑셀 모든 시트에 UID / Korean / English / Japanese / Chinese / Russian 컬럼 필요
 * - ExportData() 는 Korean SO 의 UID 목록 기준으로 Export (다른 언어 누락 UID 는 빈 문자열)
 *
 * 사용 ::
 * - DataEditorWindow → "00. HcupLocalization" → 엑셀 파일 할당 → ImportData()
 * - 생성 SO: Localization_Korean / English / Japanese / Chinese / Russian
 *
 * 엑셀 규격 ::
 * | UID | Korean | English | Japanese | Chinese | Russian |
 * =========================================================
 */
#endif

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;
using HDiagnosis.Logger;
using HData.NPOI.Core;
using HcupLocalization;

namespace HData.NPOI.Localization {
    [DataEditorEntry("00. HcupLocalization")]
    public class HcupLocalizationTableLoader : ExcelLoader<HcupLocalizationTableLoader> {
#if UNITY_EDITOR
        #region Protected - Keys
        protected override string[] keys => LocalizationExcelParser.HEADER_KEYS;
        #endregion

        #region Public - Import
        public override void ImportData() {
            if (workBook == null) {
                HLogger.Error("[HcupLocalizationTableLoader] 엑셀 파일을 먼저 로드하세요.");
                return;
            }
            if (string.IsNullOrEmpty(DataOutputPath)) {
                HLogger.Error("[HcupLocalizationTableLoader] 데이터 출력 경로(dataOutputPath)를 설정하세요.");
                return;
            }

            var dataList = LocalizationExcelParser.Parse(ExcelToJsonAllSheets());
            if (dataList == null) return;

            AssetFolderUtility.EnsureFolder(DataOutputPath);

            _WriteLanguageSO(LocalizationLanguage.Korean,   dataList);
            _WriteLanguageSO(LocalizationLanguage.English,  dataList);
            _WriteLanguageSO(LocalizationLanguage.Japanese, dataList);
            _WriteLanguageSO(LocalizationLanguage.Chinese,  dataList);
            _WriteLanguageSO(LocalizationLanguage.Russian,  dataList);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            HLogger.Log($"[HcupLocalizationTableLoader] Import 완료 — UID {dataList.Count}개 / 5개 언어 SO 생성·갱신.");
        }
        #endregion

        #region Public - Export
        public override void ExportData() {
            if (string.IsNullOrEmpty(DataOutputPath)) {
                HLogger.Error("[HcupLocalizationTableLoader] 데이터 출력 경로(dataOutputPath)를 설정하세요.");
                return;
            }

            var langs = (LocalizationLanguage[])Enum.GetValues(typeof(LocalizationLanguage));
            var soMap = new Dictionary<LocalizationLanguage, LocalizationSO>(langs.Length);

            foreach (var lang in langs) {
                string path = $"{DataOutputPath}/Localization_{lang}.asset";
                var so = AssetDatabase.LoadAssetAtPath<LocalizationSO>(path);
                if (so == null) {
                    HLogger.Error($"[HcupLocalizationTableLoader] {path} 를 찾을 수 없습니다. Import를 먼저 실행하세요.");
                    return;
                }
                soMap[lang] = so;
            }

            // Korean SO 의 UID 목록 기준으로 정렬 Export
            var uids = new List<string>(soMap[LocalizationLanguage.Korean].GetUIDs());
            uids.Sort(StringComparer.Ordinal);

            var arr = new JArray();
            foreach (var uid in uids) {
                var json = new JObject();
                json["UID"] = uid;
                foreach (var lang in langs) {
                    json[lang.ToString()] = soMap[lang].GetRawText(uid);
                }
                arr.Add(json);
            }

            JsonToExcel(arr, "Localization");
        }
        #endregion

        #region Private - SO Write
        private void _WriteLanguageSO(LocalizationLanguage language, List<LocalizationData> data) {
            string assetPath = $"{DataOutputPath}/Localization_{language}.asset";

            var so = AssetDatabase.LoadAssetAtPath<LocalizationSO>(assetPath);
            if (so == null) {
                so = ScriptableObject.CreateInstance<LocalizationSO>();
                so.SetLanguageCode(language);
                for (int k = 0; k < data.Count; k++) {
                    so.SetEntry(data[k].uid, data[k].GetText(language));
                }
                AssetDatabase.CreateAsset(so, assetPath);
            } else {
                so.SetLanguageCode(language);
                so.ClearTable();
                for (int k = 0; k < data.Count; k++) {
                    so.SetEntry(data[k].uid, data[k].GetText(language));
                }
                EditorUtility.SetDirty(so);
            }
        }
        #endregion
#endif
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.07.04 keys 를 LocalizationExcelParser.HEADER_KEYS 공용 상수로 교체
 *
 * # 변경
 * - keys 배열 리터럴 제거 — LocalizationExcelParser.HEADER_KEYS 참조 (HUnityLocalizationTableLoader 와 헤더 규격 단일 소스화)
 *
 * =============================================================================
 * @Jason - PKH 2026.07.03 HcupLocalizationTableLoader 개칭 + 공용 파서 적용
 *
 * # 변경
 * - 클래스명: LocalizationTableLoader → HcupLocalizationTableLoader (HUnityLocalization 과 구별)
 * - 파싱·UID 검증 블록 → LocalizationExcelParser.Parse() 로 추출 (공용화)
 * - _EnsureDirectory → AssetFolderUtility.EnsureFolder 로 추출 (공용화)
 * - _WriteLanguageSO selector 파라미터 제거 — LocalizationData.GetText 사용
 *
 * =============================================================================
 * @Jason - PKH 2026.05.13 LocalizationLanguage 열거형 도입 — 타입 안정성 강화
 *
 * # 변경
 * - using HUI.TextUI → using HLocalization
 * - keys 배열: nameof(LocalizationLanguage.Korean) 등으로 하드코딩 제거
 * - ImportData(): row 접근 키 nameof 사용 (enum과 컬럼명 자동 동기화)
 * - ImportData(): _WriteLanguageSO 호출 시 enum 값 전달
 * - ExportData(): string[] langs → LocalizationLanguage[] (Enum.GetValues)
 *   Dictionary<string, SO> → Dictionary<LocalizationLanguage, SO>
 * - _WriteLanguageSO(string) → _WriteLanguageSO(LocalizationLanguage)
 *
 * # 효과
 * - 언어 추가 시 enum 에 한 줄 추가만으로 keys·Import·Export 전체 자동 반영
 *
 * =============================================================================
 * @Jason - PKH 2026.05.13 최초 작성
 *
 * # 목적
 * - HCUP-2.1.0 Localization Phase 2 — NPOI 엑셀 → 5개 언어 LocalizationSO 생성
 *
 * # 구조 결정
 * - ExcelToJsonAllSheets() 사용: 다중 시트 병합으로 UID 네임스페이스 유연하게 관리
 * - UID 중복 시 즉시 return — 데이터 일관성을 빌드타임에 강제
 * - ExportData() Korean SO 기준 정렬: 동일 UID 순서로 재Import 시 Excel 안정성 확보
 * - HCUP.HData.NPOI.asmdef 에 GUID:633df54f5635b1f4c95d4e6926f70597 (HCUP.HUI) 참조 추가
 *
 * =============================================================================
 */
#endif

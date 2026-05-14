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
 * - DataEditorWindow → "00. Localization" → 엑셀 파일 할당 → ImportData()
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
using HLocalization;

namespace HData.NPOI.Localization {
    public class LocalizationTableLoader : ExcelLoader<LocalizationTableLoader> {
#if UNITY_EDITOR
        #region Protected - Keys
        protected override string[] keys => new[] {
            "UID",
            nameof(LocalizationLanguage.Korean),
            nameof(LocalizationLanguage.English),
            nameof(LocalizationLanguage.Japanese),
            nameof(LocalizationLanguage.Chinese),
            nameof(LocalizationLanguage.Russian)
        };
        #endregion

        #region Public - Import
        public override void ImportData() {
            if (workBook == null) {
                HLogger.Error("[LocalizationTableLoader] 엑셀 파일을 먼저 로드하세요.");
                return;
            }
            if (string.IsNullOrEmpty(DataOutputPath)) {
                HLogger.Error("[LocalizationTableLoader] 데이터 출력 경로(dataOutputPath)를 설정하세요.");
                return;
            }

            var merged = ExcelToJsonAllSheets();
            if (merged == null || merged.Count == 0) {
                HLogger.Error("[LocalizationTableLoader] 유효한 시트 데이터가 없습니다. 컬럼(UID/Korean/…)을 확인하세요.");
                return;
            }

            // UID 중복 검사 + 행 파싱
            var uidSet = new HashSet<string>(StringComparer.Ordinal);
            var dataList = new List<LocalizationData>(merged.Count);

            foreach (JObject row in merged) {
                string uid = row["UID"]?.Value<string>();
                if (string.IsNullOrWhiteSpace(uid)) {
                    HLogger.Error("[LocalizationTableLoader] 빈 UID 발견. Import를 중단합니다.");
                    return;
                }
                if (!uidSet.Add(uid)) {
                    HLogger.Error($"[LocalizationTableLoader] 중복 UID '{uid}' 발견. Import를 중단합니다.");
                    return;
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

            _EnsureDirectory(DataOutputPath);

            _WriteLanguageSO(LocalizationLanguage.Korean,   dataList, d => d.korean);
            _WriteLanguageSO(LocalizationLanguage.English,  dataList, d => d.english);
            _WriteLanguageSO(LocalizationLanguage.Japanese, dataList, d => d.japanese);
            _WriteLanguageSO(LocalizationLanguage.Chinese,  dataList, d => d.chinese);
            _WriteLanguageSO(LocalizationLanguage.Russian,  dataList, d => d.russian);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            HLogger.Log($"[LocalizationTableLoader] Import 완료 — UID {dataList.Count}개 / 5개 언어 SO 생성·갱신.");
        }
        #endregion

        #region Public - Export
        public override void ExportData() {
            if (string.IsNullOrEmpty(DataOutputPath)) {
                HLogger.Error("[LocalizationTableLoader] 데이터 출력 경로(dataOutputPath)를 설정하세요.");
                return;
            }

            var langs = (LocalizationLanguage[])Enum.GetValues(typeof(LocalizationLanguage));
            var soMap = new Dictionary<LocalizationLanguage, LocalizationSO>(langs.Length);

            foreach (var lang in langs) {
                string path = $"{DataOutputPath}/Localization_{lang}.asset";
                var so = AssetDatabase.LoadAssetAtPath<LocalizationSO>(path);
                if (so == null) {
                    HLogger.Error($"[LocalizationTableLoader] {path} 를 찾을 수 없습니다. Import를 먼저 실행하세요.");
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
        private void _WriteLanguageSO(LocalizationLanguage language, List<LocalizationData> data, Func<LocalizationData, string> textSelector) {
            string assetPath = $"{DataOutputPath}/Localization_{language}.asset";

            var so = AssetDatabase.LoadAssetAtPath<LocalizationSO>(assetPath);
            if (so == null) {
                so = ScriptableObject.CreateInstance<LocalizationSO>();
                so.SetLanguageCode(language);
                for (int k = 0; k < data.Count; k++) {
                    so.SetEntry(data[k].uid, textSelector(data[k]));
                }
                AssetDatabase.CreateAsset(so, assetPath);
            } else {
                so.SetLanguageCode(language);
                so.ClearTable();
                for (int k = 0; k < data.Count; k++) {
                    so.SetEntry(data[k].uid, textSelector(data[k]));
                }
                EditorUtility.SetDirty(so);
            }
        }
        #endregion

        #region Private - Directory
        private static void _EnsureDirectory(string unityPath) {
            if (AssetDatabase.IsValidFolder(unityPath)) return;

            int lastSlash = unityPath.LastIndexOf('/');
            if (lastSlash <= 0) return;

            string parent = unityPath.Substring(0, lastSlash);
            string folderName = unityPath.Substring(lastSlash + 1);

            _EnsureDirectory(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
        #endregion
#endif
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
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

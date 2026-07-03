#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 로컬리제이션 엑셀을 Unity 네이티브 Localization 패키지의
 * Locale + StringTableCollection 으로 변환하는 NPOI Loader.
 *
 * 특징 ::
 * - ExcelLoader<T> 상속 — 엑셀 규격은 HcupLocalizationTableLoader 와 동일
 * - Import: Locale 5개 + StringTableCollection "Localization" 자동 생성·갱신 (멱등)
 * - 엑셀에서 사라진 UID 는 컬렉션에서 제거 (stale 정리)
 *
 * 주의사항 ::
 * - dataOutputPath 설정 필수 — Locale 은 {dataOutputPath}/Locales/ 에 생성
 * - com.unity.localization 1.5 미만/미설치 환경에서는 어셈블리 자체가 컴파일되지 않음
 *
 * 사용 ::
 * - DataEditorWindow → "01. HUnityLocalization" → 엑셀 할당 → ImportData()
 * - 런타임 소비는 네이티브 API 직접 사용 (LocalizationSettings / LocalizeStringEvent)
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
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEditor.Localization;
using Newtonsoft.Json.Linq;
using HDiagnosis.Logger;
using HData.NPOI.Core;
using HData.NPOI.Localization;
using HcupLocalization;

namespace HUnityLocalization {
    [DataEditorEntry("01. HUnityLocalization")]
    public class HUnityLocalizationTableLoader : ExcelLoader<HUnityLocalizationTableLoader> {
#if UNITY_EDITOR
        #region Const
        const string TABLE_COLLECTION_NAME = "Localization";
        const string LOCALES_FOLDER_NAME = "Locales";
        #endregion

        #region Protected - Keys
        protected override string[] keys => LocalizationExcelParser.HEADER_KEYS;
        #endregion

        #region Public - Import
        public override void ImportData() {
            if (workBook == null) {
                HLogger.Error("[HUnityLocalizationTableLoader] 엑셀 파일을 먼저 로드하세요.");
                return;
            }
            if (string.IsNullOrEmpty(DataOutputPath)) {
                HLogger.Error("[HUnityLocalizationTableLoader] 데이터 출력 경로(dataOutputPath)를 설정하세요.");
                return;
            }

            var dataList = LocalizationExcelParser.Parse(ExcelToJsonAllSheets());
            if (dataList == null) return;

            var locales = _EnsureLocales();
            if (locales == null) return;

            var collection = _EnsureTableCollection(locales);
            if (collection == null) return;

            // 1) 모든 언어의 StringTable 을 먼저 확보 — 실패 시 데이터 변형 전에 중단 (테이블 신설은 추가적 부수효과라 무손실)
            var tables = new Dictionary<LocalizationLanguage, StringTable>(locales.Count);
            foreach (var pair in locales) {
                var identifier = pair.Value.Identifier;
                var table = collection.GetTable(identifier) as StringTable;
                if (table == null) table = collection.AddNewTable(identifier) as StringTable;
                if (table == null) {
                    HLogger.Error($"[HUnityLocalizationTableLoader] '{identifier.Code}' StringTable 생성 실패. Localization Tables 창에서 컬렉션 상태를 확인하세요.");
                    return;
                }
                tables[pair.Key] = table;
            }

            // 2) 전 테이블 확보 후에만 stale 제거 + 기록
            _RemoveStaleEntries(collection, dataList);

            foreach (var pair in tables) {
                for (int k = 0; k < dataList.Count; k++) {
                    pair.Value.AddEntry(dataList[k].uid, dataList[k].GetText(pair.Key));
                }
                EditorUtility.SetDirty(pair.Value);
            }

            EditorUtility.SetDirty(collection.SharedData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            HLogger.Log($"[HUnityLocalizationTableLoader] Import 완료 — UID {dataList.Count}개 / StringTable {locales.Count}개 갱신.");
        }
        #endregion

        #region Public - Export
        public override void ExportData() {
            var collection = LocalizationEditorSettings.GetStringTableCollection(TABLE_COLLECTION_NAME);
            if (collection == null) {
                HLogger.Error($"[HUnityLocalizationTableLoader] StringTableCollection '{TABLE_COLLECTION_NAME}' 를 찾을 수 없습니다. Import를 먼저 실행하세요.");
                return;
            }

            var langs = (LocalizationLanguage[])Enum.GetValues(typeof(LocalizationLanguage));
            var tableMap = new Dictionary<LocalizationLanguage, StringTable>(langs.Length);

            foreach (var lang in langs) {
                if (!LocaleCodeMap.TryGetSystemLanguage(lang, out SystemLanguage systemLanguage)) {
                    HLogger.Error($"[HUnityLocalizationTableLoader] '{lang}' 의 SystemLanguage 매핑이 없습니다. LocaleCodeMap 을 확인하세요.");
                    return;
                }
                var table = collection.GetTable(new LocaleIdentifier(systemLanguage)) as StringTable;
                if (table == null) {
                    HLogger.Error($"[HUnityLocalizationTableLoader] '{lang}' StringTable 이 없습니다. Import를 먼저 실행하세요.");
                    return;
                }
                tableMap[lang] = table;
            }

            var uids = new List<string>(collection.SharedData.Entries.Count);
            foreach (var entry in collection.SharedData.Entries) {
                uids.Add(entry.Key);
            }
            uids.Sort(StringComparer.Ordinal);

            var arr = new JArray();
            foreach (var uid in uids) {
                var json = new JObject();
                json["UID"] = uid;
                foreach (var lang in langs) {
                    json[lang.ToString()] = tableMap[lang].GetEntry(uid)?.Value ?? "";
                }
                arr.Add(json);
            }

            JsonToExcel(arr, "Localization");
        }
        #endregion

        #region Private - Locale
        private Dictionary<LocalizationLanguage, Locale> _EnsureLocales() {
            var langs = (LocalizationLanguage[])Enum.GetValues(typeof(LocalizationLanguage));

            // 1) 전 언어 매핑 검증 — 부분 생성 전에 전체 실패 확정
            var languageMap = new Dictionary<LocalizationLanguage, SystemLanguage>(langs.Length);
            foreach (var lang in langs) {
                if (!LocaleCodeMap.TryGetSystemLanguage(lang, out SystemLanguage systemLanguage)) {
                    HLogger.Error($"[HUnityLocalizationTableLoader] '{lang}' 의 SystemLanguage 매핑이 없습니다. LocaleCodeMap 을 확인하세요.");
                    return null;
                }
                languageMap[lang] = systemLanguage;
            }

            // 2) 매핑 검증 통과 후에만 폴더/Locale 생성
            string localesPath = $"{DataOutputPath}/{LOCALES_FOLDER_NAME}";
            AssetFolderUtility.EnsureFolder(localesPath);

            var result = new Dictionary<LocalizationLanguage, Locale>(langs.Length);
            var existingLocales = LocalizationEditorSettings.GetLocales();

            foreach (var lang in langs) {
                var systemLanguage = languageMap[lang];
                var identifier = new LocaleIdentifier(systemLanguage);
                Locale locale = null;
                foreach (var existing in existingLocales) {
                    if (existing.Identifier == identifier) {
                        locale = existing;
                        break;
                    }
                }

                if (locale == null) {
                    locale = Locale.CreateLocale(systemLanguage);
                    AssetDatabase.CreateAsset(locale, $"{localesPath}/{lang}.asset");
                    LocalizationEditorSettings.AddLocale(locale);
                }

                result[lang] = locale;
            }
            return result;
        }
        #endregion

        #region Private - Table Collection
        private StringTableCollection _EnsureTableCollection(Dictionary<LocalizationLanguage, Locale> locales) {
            var collection = LocalizationEditorSettings.GetStringTableCollection(TABLE_COLLECTION_NAME);
            if (collection != null) return collection;

            var localeList = new List<Locale>(locales.Values);
            collection = LocalizationEditorSettings.CreateStringTableCollection(TABLE_COLLECTION_NAME, DataOutputPath, localeList);
            if (collection == null) {
                HLogger.Error($"[HUnityLocalizationTableLoader] StringTableCollection '{TABLE_COLLECTION_NAME}' 생성 실패. dataOutputPath('{DataOutputPath}')가 유효한 Assets 하위 경로인지 확인하세요.");
            }
            return collection;
        }

        private void _RemoveStaleEntries(StringTableCollection collection, List<LocalizationData> dataList) {
            var importedUids = new HashSet<string>(StringComparer.Ordinal);
            for (int k = 0; k < dataList.Count; k++) {
                importedUids.Add(dataList[k].uid);
            }

            var sharedEntries = collection.SharedData.Entries;
            for (int k = sharedEntries.Count - 1; k >= 0; k--) {
                if (importedUids.Contains(sharedEntries[k].Key)) continue;
                collection.RemoveEntry(sharedEntries[k].Id);
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
 * @Jason - PKH 2026.07.04 Import 원자성 보강 + 매핑 선행 검증 + 클래스 본체 UNITY_EDITOR 가드
 *
 * # 변경
 * - ImportData(): 언어별 StringTable 확보 루프를 stale 제거 앞으로 이동 — 루프 중간 AddNewTable 실패 시 "stale 제거 + 일부 언어만 갱신" 중간 상태 제거
 * - _EnsureLocales(): SystemLanguage 매핑 검증 루프를 Locale 생성 루프 앞으로 분리 — enum 확장 시 매핑 누락으로 인한 고아 Locale 생성 방지
 * - keys: LocalizationExcelParser.HEADER_KEYS 공용 상수 참조로 교체 (HcupLocalizationTableLoader 와 헤더 규격 단일 소스화)
 * - 클래스 본체 #if UNITY_EDITOR 가드 추가 (ExcelLoader/AssetDatabaseInstance/HcupLocalizationTableLoader 디렉터리 관용 정합)
 *
 * =============================================================================
 * @Jason - PKH 2026.07.03 최초 작성
 *
 * # 목적
 * - HCUP-2.1.0 HUnityLocalization — 엑셀 → Unity 네이티브 Localization 데이터 파이프라인
 *
 * # 구조 결정
 * - 공용 파서(LocalizationExcelParser) 사용 — HcupLocalizationTableLoader 와 규격·검증 공유
 * - 멱등 Import: AddEntry(동일 키 갱신) + stale UID 는 collection.RemoveEntry 로 제거
 * - Locale 매칭은 LocaleIdentifier(SystemLanguage) == 비교 (코드 문자열 하드코딩 회피)
 * - API 시그니처는 com.unity.localization@1.5 공식 문서로 검증 (2026.07.03)
 *
 * =============================================================================
 */
#endif

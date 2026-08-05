#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 이 스크립트는 Excel 데이터를 게임 데이터로 변환하기 위한 NPOI 기반
 * Excel Import / Export 시스템의 베이스 클래스입니다.
 *
 * Excel 파일을 읽어 JSON으로 변환하고 이를 ScriptableObject 데이터로 생성하는
 * 역할을 담당합니다.
 *
 * 주요 기능 ::
 * 1. Excel 파일 로드
 * 2. 시트 선택
 * 3. Excel → JSON 변환
 * 4. JSON → Excel Export
 * 5. 데이터 Import / Export 인터페이스 제공
 *
 * 사용법 ::
 * 1. ExcelLoader를 상속하여 Loader 클래스를 생성합니다.
 * 예시 ::
 * public class EquipmentTableLoader : ExcelLoader<EquipmentTableLoader>
 *
 * 2. 엑셀 속성 배열을 keys에 정의합니다.
 * protected override string[] keys => new[] {
 *     "Id",
 *     "Name",
 *     "Level"
 * };
 *
 * 3. ImportData() 구현
 * Excel → ScriptableObject 데이터 생성
 *
 * 주요 흐름 ::
 * Excel 파일 선택
 *  → Workbook 생성
 *  → Sheet 선택
 *  → ExcelToJson
 *  → Loader ImportData
 *
 * 주의사항 ::
 * 1. Excel 첫 번째 Row는 반드시 Header(Row0)여야 합니다.
 * 2. keys 배열은 Excel Header와 정확히 일치해야 합니다.
 * 3. ExcelLoader는 UNITY_EDITOR에서만 동작합니다.
 * 4. Runtime에서 호출되면 동작하지 않습니다.
 *
 * 사용 목적 ::
 * Excel 기반 게임 데이터 관리
 * Game Designer와 협업을 위한 데이터 파이프라인
 * =========================================================
 */
#endif

using System;
#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Newtonsoft.Json.Linq;
using HDiagnosis.Logger;
#endif

namespace HExcel.Core {
    [Serializable]
    public abstract class ExcelLoader<Loader> :
        AssetDatabaseInstance<Loader>
        where Loader: ExcelLoader<Loader>, new() {
#if UNITY_EDITOR
        #region Fields
        [SerializeField]
        UnityEngine.Object excelFileAsset;

        // 직렬화 없음 — 테스트/프로그래밍 경로 직접 지정용 (SetDefaultExcelSettings 경유)
        string excelFilePath;

        [SerializeField]
        string sheetName;

        [SerializeField]
        string dataOutputPath;

        protected IWorkbook workBook;
        protected ISheet sheet;
        #endregion

        #region Protected - Abstract Keys
        protected abstract string[] keys { get; }
        #endregion

        #region Protected - Properties
        protected string DataOutputPath => dataOutputPath;
        #endregion

        #region Internal - Properties
        internal List<string> Sheets {
            get {
                if (workBook == null) return null;
                var sheets = new List<string>();
                for (int k = 0; k < workBook.NumberOfSheets; k++) {
                    sheets.Add(workBook.GetSheetName(k));
                }
                return sheets;
            }
        }

        internal bool IsAvailable {
            get {
                if (sheet == null) return false;

                var cols = new List<string>();
                foreach (var col in sheet.GetRow(0)) {
                    var value = col.ToString();
                    cols.Add(value);
                }

                foreach (var key in keys) {
                    if (!cols.Contains(key)) return false;
                }

                return true;
            }
        }
        #endregion

        #region Public - Excel Settings
        public void SetDefaultExcelSettings(string filePath) {
            this.excelFilePath = filePath;
            LoadExcelFile();
        }
        #endregion

        #region Public - Import / Export
        public abstract void ImportData();
        public abstract void ExportData();
        #endregion

        #region Protected - Excel to JSON / JSON to Excel
        protected JArray ExcelToJson() {
            var arr = new JArray();
            var cols = new Dictionary<string, int>();

            var firstRow = sheet.GetRow(0);
            Assert.IsNotNull(firstRow, "[ExcelLoader] Header row(0) is missing.");

            for (int k = 0; k < keys.Length; k++) {
                var headerCell = firstRow.GetCell(k, MissingCellPolicy.RETURN_BLANK_AS_NULL);
                Assert.IsNotNull(headerCell, $"[ExcelLoader] Header cell is blank. col={k}");
                var value = headerCell.ToString();
                cols.Add(value, k);
            }

            for (int k = 1; k <= sheet.LastRowNum; k++) {
                var row = sheet.GetRow(k);
                if (row == null) {
                    HLogger.Error($"[ExcelLoader] Row({k}) data is null.");
                    continue;
                }

                var json = new JObject();

                foreach (var col in cols) {
                    var cell = row.GetCell(col.Value, MissingCellPolicy.RETURN_BLANK_AS_NULL);

                    if (cell == null) continue;
                    if (cell.CellType == CellType.Blank) continue;

                    var value = cell.ToString();
                    json.Add(col.Key, value);
                }

                arr.Add(json);
            }

            return arr;
        }

        /// <summary>
        /// 워크북의 모든 유효 시트를 시트명 → JArray Dictionary로 반환.
        /// 헤더에 keys가 모두 포함된 시트만 대상. 시트별 구분이 필요한 Loader에서 사용.
        /// </summary>
        protected Dictionary<string, JArray> ExcelToJsonBySheet() {
            var result = new Dictionary<string, JArray>();
            if (workBook == null) {
                HLogger.Error("[ExcelLoader] Workbook is null.");
                return result;
            }

            var originalSheet = sheet;
            try {
                for (int k = 0; k < workBook.NumberOfSheets; k++) {
                    var s = workBook.GetSheetAt(k);
                    if (s == null) continue;

                    var header = s.GetRow(0);
                    if (header == null) {
                        HLogger.Log($"[ExcelLoader] Skip sheet '{s.SheetName}' (no header).");
                        continue;
                    }

                    var cols = new List<string>();
                    foreach (var c in header) cols.Add(c.ToString());

                    bool hasAllKeys = true;
                    foreach (var key in keys) {
                        if (!cols.Contains(key)) {
                            hasAllKeys = false;
                            break;
                        }
                    }
                    if (!hasAllKeys) {
                        HLogger.Log($"[ExcelLoader] Skip sheet '{s.SheetName}' (missing keys).");
                        continue;
                    }

                    sheet = s;
                    result[s.SheetName] = ExcelToJson();
                }
            }
            finally {
                sheet = originalSheet;
            }
            return result;
        }

        protected JArray ExcelToJsonAllSheets() {
            var merged = new JArray();
            if (workBook == null) {
                HLogger.Error("[ExcelLoader] Workbook is null.");
                return merged;
            }

            var originalSheet = sheet;
            try {
                for (int i = 0; i < workBook.NumberOfSheets; i++) {
                    var s = workBook.GetSheetAt(i);
                    if (s == null) continue;

                    // 헤더 row가 keys를 모두 포함하는 시트만 병합
                    var header = s.GetRow(0);
                    if (header == null) {
                        HLogger.Log($"[ExcelLoader] Skip sheet '{s.SheetName}' (no header).");
                        continue;
                    }

                    var cols = new List<string>();
                    foreach (var c in header) cols.Add(c.ToString());

                    bool hasAllKeys = true;
                    foreach (var k in keys) {
                        if (!cols.Contains(k)) {
                            hasAllKeys = false;
                            break;
                        }
                    }
                    if (!hasAllKeys) {
                        HLogger.Log($"[ExcelLoader] Skip sheet '{s.SheetName}' (missing keys).");
                        continue;
                    }

                    sheet = s;
                    var arr = ExcelToJson();
                    foreach (var item in arr) merged.Add(item);
                }
            }
            finally {
                sheet = originalSheet;
            }
            return merged;
        }

        protected void JsonToExcel(JArray jArray, string fileName = "") {
            bool isConfirmed = EditorUtility.DisplayDialog(
                "Export Confirmation",
                $"'{fileName}'을(를) 엑셀 파일로 내보내시겠습니까?",
                "확인",
                "취소"
            );

            if (!isConfirmed) {
                return;
            }

            IWorkbook book = new XSSFWorkbook();
            var sheet = book.CreateSheet("Sheet");
            IRow row = sheet.CreateRow(0);

            for (int k = 0; k < keys.Length; k++) {
                var key = keys[k];
                row.CreateCell(k).SetCellValue(key);
            }

            for (int k = 0; k < jArray.Count; k++) {
                var json = jArray[k] as JObject;
                row = sheet.CreateRow(k + 1);
                foreach (var property in json.Properties()) {
                    var key = property.Name;
                    var value = property.Value.Value<string>();
                    var index = Array.IndexOf(keys, key);
                    if (index < 0) {
                        HLogger.Error($"[ExcelLoader] '{fileName}' file's attribute({key}) value index({index}) is missing. Cannot save value({value})");
                        continue;
                    }
                    row.CreateCell(index).SetCellValue(value);
                }
            }

            try {
                string path = EditorUtility.SaveFilePanel("엑셀 파일로 저장하기", "", fileName, "xlsx");
                if (string.IsNullOrEmpty(path)) return;

                HLogger.Log(path);

                using (var fs = new FileStream(path, FileMode.Create)) {
                    book.Write(fs);
                }
            }
            finally {
                book.Close();
            }
        }
        #endregion

        #region Internal - Excel Loading
        internal void LoadExcelFile() {
            string path = excelFilePath;

            if (string.IsNullOrEmpty(path) && excelFileAsset != null) {
                string assetPath = AssetDatabase.GetAssetPath(excelFileAsset);
                if (!string.IsNullOrEmpty(assetPath))
                    path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            }

            if (string.IsNullOrEmpty(path)) return;

            // 이전 워크북을 놓아준다 — XSSF 는 OPCPackage + 전체 시트 인메모리 트리라 누수 비용이 크다.
            CloseWorkbook();

            try {
                HLogger.Log(path);
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read)) {
                    if (path.EndsWith("xls")) {
                        workBook = new HSSFWorkbook(fs);
                    } else if (path.EndsWith("xlsx")) {
                        workBook = new XSSFWorkbook(fs);
                    } else {
                        throw new NotSupportedException($"[ExcelLoader] Unsupported extension :: {path}");
                    }
                }
            }
            catch (Exception e) {
                // 실패를 삼키고 진행하면 사용자는 새 파일을 보고 있다고 믿으며 이전 데이터를
                // Import 하게 된다 — 워크북/시트를 비워 오염 경로를 차단한다.
                workBook = null;
                sheet = null;
                HLogger.Error($"[ExcelLoader] Load failed :: {path} — {e.Message}");
                return;
            }

            GetSheet();
        }
        #endregion

        #region Internal - Workbook Lifetime
        internal void CloseWorkbook() {
            if (workBook == null) return;
            try {
                workBook.Close();
            }
            catch (Exception e) {
                HLogger.Error($"[ExcelLoader] Workbook close failed :: {e.Message}");
            }
            workBook = null;
            sheet = null;
        }
        #endregion

        #region Internal - Sheet Selection
        internal void GetSheet() {
            if(workBook == null) return;
            if(sheetName == null || sheetName == "") {
                sheet = null;
                return;
            }
            sheet = workBook.GetSheet(sheetName);
        }
        #endregion

        #region Internal - Preview Data
        internal string[] GetPreviewHeaders() {
            if (sheet == null) return null;
            IRow headerRow = sheet.GetRow(0);
            if (headerRow == null) return null;

            var headers = new List<string>();
            for (int k = 0; k < headerRow.LastCellNum; k++) {
                ICell cell = headerRow.GetCell(k, MissingCellPolicy.RETURN_BLANK_AS_NULL);
                headers.Add((cell == null || cell.CellType == CellType.Blank) ? "" : cell.ToString());
            }
            return headers.Count > 0 ? headers.ToArray() : null;
        }

        internal string[][] GetPreviewRows(int maxRows = 200) {
            string[] headers = GetPreviewHeaders();
            if (headers == null) return null;

            var rows = new List<string[]>();
            int lastRow = Math.Min(sheet.LastRowNum, maxRows);

            for (int k = 1; k <= lastRow; k++) {
                IRow row = sheet.GetRow(k);
                if (row == null) continue;

                var cells = new string[headers.Length];
                for (int c = 0; c < headers.Length; c++) {
                    ICell cell = row.GetCell(c, MissingCellPolicy.RETURN_BLANK_AS_NULL);
                    cells[c] = (cell == null || cell.CellType == CellType.Blank) ? "" : cell.ToString();
                }
                rows.Add(cells);
            }
            return rows.ToArray();
        }
        #endregion
#endif
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.13 dataOutputPath 필드 + DataOutputPath 프로퍼티 추가
 *
 * # 추가
 * - [SerializeField] string dataOutputPath : Import 시 데이터 SO를 생성할 Unity 상대 경로 (예: "Assets/Data")
 * - protected string DataOutputPath : Loader 서브클래스에서 접근용 getter
 * - ExcelLoaderEditor 에서 경로 피커 IMGUI 통해 편집, ImportData() 에서 SO 자동 생성에 사용
 *
 * =============================================================================
 * @Jason - PKH 2026.05.13 excelFileAsset 타입 DefaultAsset → Object 수정
 *
 * # 변경
 * - DefaultAsset excelFileAsset → Object excelFileAsset
 * - DefaultAsset은 UnityEditor.dll 타입 — Unity 직렬화 시스템이 SerializedProperty 생성 불가
 *   → FindProperty("excelFileAsset") null 반환 → ObjectField NullReferenceException → 버튼 전체 미출력
 * - Object(UnityEngine.Object)는 직렬화 지원. ObjectField objType 파라미터로 DefaultAsset 필터 유지
 *
 * =============================================================================
 * @Jason - PKH 2026.05.13 excelFilePath(string) → excelFileAsset(DefaultAsset) 교체
 *
 * # 변경
 * - [SerializeField] string excelFilePath → [SerializeField] DefaultAsset excelFileAsset
 *   Project 창 드래그 앤 드롭으로 .xlsx 파일 할당 가능
 * - excelFilePath : 직렬화 제거, SetDefaultExcelSettings / 테스트 경로 직접 지정용으로만 유지
 * - LoadExcelFile() : excelFilePath 우선, 없으면 excelFileAsset → AssetDatabase.GetAssetPath → 절대 경로 변환
 * - LoadExcelFile() 기존 버그 수정 : path 계산 후 FileStream은 excelFilePath로 열던 문제 수정
 *
 * =============================================================================
 * @Jason - PKH 2026.05.13 데이터 미리보기 기능 추가 (GetPreviewHeaders / GetPreviewRows)
 *
 * # 추가
 * - GetPreviewHeaders() : 현재 시트의 헤더 행 전체를 string[] 반환
 * - GetPreviewRows(int maxRows) : 데이터 행을 string[][] 반환 (최대 200행 기본)
 * - ExcelLoaderEditor에서 Reflection 경유로 호출 — Import 없이 시트 로드 즉시 미리보기 표시
 *
 * =============================================================================
 * @Jason - PKH 2026.05.13 컨벤션 정리 — SerializeField 필드명 camelCase 유지 확인
 *
 * # 변경
 * - excelFilePath, sheetName : 언더바 접두 적용 오류 수정 → 원래 camelCase 복원
 *   (private 변수는 접근제어자 없이 camelCase, 언더바 접두 없음)
 *
 * =============================================================================
 * @Jason - PKH 2026.05.13 M3 — Odin 어트리뷰트 제거, internal 멤버 노출
 *
 * # 변경
 * - [FilePath], [ValueDropdown], [Button], [ShowIf] Odin 어트리뷰트 전면 제거
 * - _LoadExcelFile → LoadExcelFile (internal), _GetSheet → GetSheet (internal)
 * - _IsAvailable → IsAvailable (internal property), _Sheets → Sheets (internal property)
 *
 * =============================================================================
 */
#endif

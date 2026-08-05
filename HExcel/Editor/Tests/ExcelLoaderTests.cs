#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * ExcelLoader<T> 데이터 추출 파이프라인 EditMode 테스트
 *
 * 테스트 대상 ::
 * + Excel 로드 후 시트 목록 감지
 * + ImportData 행 수 / 필드 값 정확성
 * + null 행(빈 행 갭) 충돌 없이 건너뜀
 *
 * 실행 방법 ::
 * Unity Menu → Window → General → Test Runner → EditMode 탭
 *
 * 주의사항 ::
 * - sheetName(private), GetSheet(internal)은 Reflection 경유 접근
 * - 테스트용 xlsx는 temporaryCachePath에 생성 후 TearDown에서 삭제
 * =========================================================
 */
#endif

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Newtonsoft.Json.Linq;
using UnityEngine;
using HExcel.Core;

namespace HExcel.Tests {

    // AssetDatabase 의존 없이 ImportData 결과를 메모리에 저장하는 테스트 전용 Loader
    public class TestLoader : ExcelLoader<TestLoader> {
        protected override string[] keys => new[] { "id", "name", "value" };

        List<SampleData> importedRows = new();
        public IReadOnlyList<SampleData> ImportedRows => importedRows;

        public override void ImportData() {
            importedRows = new List<SampleData>();
            var arr = ExcelToJson();
            foreach (var token in arr) {
                if (token is not JObject json) continue;
                importedRows.Add(new SampleData {
                    Id    = json["id"]?.Value<int>()     ?? 0,
                    Name  = json["name"]?.Value<string>() ?? string.Empty,
                    Value = json["value"]?.Value<int>()   ?? 0,
                });
            }
        }

        public override void ExportData() { }
    }

    [TestFixture]
    public class ExcelLoaderTests {
        static readonly BindingFlags MEMBER_FLAGS =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        static readonly (int id, string name, int value)[] TEST_ROWS = {
            (1, "Alice", 100),
            (2, "Bob",   200),
            (3, "Carol", 300),
        };

        string tempPath;
        TestLoader loader;

        [SetUp]
        public void SetUp() {
            tempPath = Path.Combine(Application.temporaryCachePath, "NPOI_TestTable.xlsx");
            _CreateTestExcel(tempPath, TEST_ROWS);
            loader = ScriptableObject.CreateInstance<TestLoader>();
        }

        [TearDown]
        public void TearDown() {
            if (loader != null) Object.DestroyImmediate(loader);
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }

        // ── 테스트 케이스 ────────────────────────────────────────

        // 1. Excel 로드 후 시트 목록이 올바르게 감지되는지
        [Test]
        public void LoadExcelFile_SheetsDetected() {
            loader.SetDefaultExcelSettings(tempPath);

            var sheets = _GetSheets(loader);

            Assert.IsNotNull(sheets, "Sheets가 null — workBook 로드 실패");
            Assert.AreEqual(1, sheets.Count, "시트 수 불일치");
            Assert.AreEqual("Sheet1", sheets[0], "시트명 불일치");
        }

        // 2. ImportData 후 파싱된 행 수가 원본과 일치하는지
        [Test]
        public void ImportData_RowCountMatches() {
            loader.SetDefaultExcelSettings(tempPath);
            _SelectSheet(loader, "Sheet1");
            loader.ImportData();

            Assert.AreEqual(TEST_ROWS.Length, loader.ImportedRows.Count, "파싱 행 수 불일치");
        }

        // 3. 각 행의 id / name / value 값이 원본과 정확히 일치하는지
        [Test]
        public void ImportData_RowValues_Correct() {
            loader.SetDefaultExcelSettings(tempPath);
            _SelectSheet(loader, "Sheet1");
            loader.ImportData();

            for (int k = 0; k < TEST_ROWS.Length; k++) {
                var expected = TEST_ROWS[k];
                var actual   = loader.ImportedRows[k];
                Assert.AreEqual(expected.id,    actual.Id,    $"Row[{k}].Id 불일치");
                Assert.AreEqual(expected.name,  actual.Name,  $"Row[{k}].Name 불일치");
                Assert.AreEqual(expected.value, actual.Value, $"Row[{k}].Value 불일치");
            }
        }

        // 4. 중간에 null 행(빈 행 갭)이 있어도 예외 없이 건너뛰는지
        [Test]
        public void ImportData_NullRowGap_Skipped() {
            string gapPath  = Path.Combine(Application.temporaryCachePath, "NPOI_Gap.xlsx");
            var gapLoader   = ScriptableObject.CreateInstance<TestLoader>();
            try {
                _CreateTestExcelWithGap(gapPath);
                gapLoader.SetDefaultExcelSettings(gapPath);
                _SelectSheet(gapLoader, "Sheet1");

                Assert.DoesNotThrow(() => gapLoader.ImportData(), "null 행에서 예외 발생");
                Assert.AreEqual(2, gapLoader.ImportedRows.Count, "null 행 제외 후 2건 기대");
            } finally {
                Object.DestroyImmediate(gapLoader);
                if (File.Exists(gapPath)) File.Delete(gapPath);
            }
        }

        // ── Reflection 헬퍼 ──────────────────────────────────────

        // sheetName(private field) 설정 + GetSheet(internal method) 호출
        void _SelectSheet(object target, string sheetName) {
            target.GetType().GetField("sheetName", MEMBER_FLAGS)?.SetValue(target, sheetName);
            target.GetType().GetMethod("GetSheet",  MEMBER_FLAGS)?.Invoke(target, null);
        }

        List<string> _GetSheets(object target) =>
            target.GetType().GetProperty("Sheets", MEMBER_FLAGS)?.GetValue(target) as List<string>;

        // ── Excel 생성 헬퍼 ──────────────────────────────────────

        // 헤더(id/name/value) + 데이터 행을 가진 xlsx 생성
        // 셀은 전부 string으로 저장 — cell.ToString() 결과를 예측 가능하게 고정
        void _CreateTestExcel(string path, (int id, string name, int value)[] rows) {
            IWorkbook wb    = new XSSFWorkbook();
            ISheet    sheet = wb.CreateSheet("Sheet1");

            IRow header = sheet.CreateRow(0);
            header.CreateCell(0).SetCellValue("id");
            header.CreateCell(1).SetCellValue("name");
            header.CreateCell(2).SetCellValue("value");

            for (int k = 0; k < rows.Length; k++) {
                IRow row = sheet.CreateRow(k + 1);
                row.CreateCell(0).SetCellValue(rows[k].id.ToString());
                row.CreateCell(1).SetCellValue(rows[k].name);
                row.CreateCell(2).SetCellValue(rows[k].value.ToString());
            }

            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            wb.Write(fs);
        }

        // Row 2를 의도적으로 건너뛴(gap) xlsx 생성
        void _CreateTestExcelWithGap(string path) {
            IWorkbook wb    = new XSSFWorkbook();
            ISheet    sheet = wb.CreateSheet("Sheet1");

            IRow header = sheet.CreateRow(0);
            header.CreateCell(0).SetCellValue("id");
            header.CreateCell(1).SetCellValue("name");
            header.CreateCell(2).SetCellValue("value");

            IRow row1 = sheet.CreateRow(1);
            row1.CreateCell(0).SetCellValue("1");
            row1.CreateCell(1).SetCellValue("Alice");
            row1.CreateCell(2).SetCellValue("100");

            // Row 2 : 의도적으로 건너뜀 → sheet.GetRow(2) == null

            IRow row3 = sheet.CreateRow(3);
            row3.CreateCell(0).SetCellValue("3");
            row3.CreateCell(1).SetCellValue("Carol");
            row3.CreateCell(2).SetCellValue("300");

            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            wb.Write(fs);
        }
    }
}
#endif

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.13 최초 작성
 *
 * # 목적
 * - ExcelLoader<T> 데이터 추출 파이프라인 EditMode 테스트 케이스 제작
 *
 * # 테스트 케이스 4종
 * - LoadExcelFile_SheetsDetected : 시트 목록 감지
 * - ImportData_RowCountMatches : 파싱 행 수 일치
 * - ImportData_RowValues_Correct : 필드 값 정확성
 * - ImportData_NullRowGap_Skipped : null 행 건너뜀
 *
 * # 설계 결정
 * - TestLoader : ExcelToJson()(protected)를 서브클래스에서 직접 호출
 *   AssetDatabase 의존 없이 메모리 List에만 저장
 * - sheetName / GetSheet 접근 : Reflection (ExcelLoaderEditor 패턴 동일)
 * - 셀 값 string 저장 : NPOI 수치 셀 ToString() 편차 제거
 *
 * =============================================================================
 */
#endif

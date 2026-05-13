using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json.Linq;
using HData.NPOI.Core;
using HDiagnosis.Logger;

namespace HData.NPOI.Samples {
    // 900000_Goods.xlsx — Goods 시트 → GoodsTable SO 변환 Loader
    public class GoodsTableLoader : ExcelLoader<GoodsTableLoader> {
        protected override string[] keys => new[] { "id", "name", "local", "icon", "이름" };

        public override void ImportData() {
            var arr = ExcelToJson();
            var rows = new List<GoodsData>();
            foreach (var token in arr) {
                if (token is not JObject json) continue;
                rows.Add(new GoodsData {
                    Id      = json["id"]?.Value<int>()      ?? 0,
                    Name    = json["name"]?.Value<string>()  ?? string.Empty,
                    Local   = json["local"]?.Value<int>()    ?? 0,
                    Icon    = json["icon"]?.Value<string>()  ?? string.Empty,
                    Display = json["이름"]?.Value<string>()  ?? string.Empty,
                });
            }

            var table = GoodsTable.Instance;
            if (!table.IsLoadedFromAsset) {
                if (string.IsNullOrEmpty(DataOutputPath)) {
                    HLogger.Error("[GoodsTableLoader] 데이터 출력 경로가 설정되지 않았습니다. Inspector에서 경로를 지정하세요.");
                    return;
                }
                string dir     = DataOutputPath.TrimEnd('/').TrimEnd('\\');
                string fullDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", dir));
                if (!Directory.Exists(fullDir)) {
                    Directory.CreateDirectory(fullDir);
                    AssetDatabase.Refresh();
                }
                table.CreateAssetAt(dir + "/GoodsTable.asset");
            }

            table.SetRows(rows);
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
        }

        public override void ExportData() {
            var arr = new JArray();
            foreach (var row in GoodsTable.Instance.Rows) {
                arr.Add(new JObject {
                    ["id"]    = row.Id,
                    ["name"]  = row.Name,
                    ["local"] = row.Local,
                    ["icon"]  = row.Icon,
                    ["이름"]  = row.Display,
                });
            }
            JsonToExcel(arr, "GoodsTable");
        }
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.13 ImportData — GoodsTable SO 자동 생성 추가
 *
 * # 변경
 * - GoodsTable.Instance.IsLoadedFromAsset == false 일 때:
 *   DataOutputPath(ExcelLoader base 필드) + "GoodsTable.asset" 경로로 CreateAssetAt() 호출
 *   → 디렉토리 없으면 Directory.CreateDirectory + AssetDatabase.Refresh() 후 생성
 * - DataOutputPath 미설정 시 HLogger.Error 후 조기 반환 (SetDirty/SaveAssets 스킵)
 *
 * # 원인
 * - 기존: GoodsTable.Instance 가 메모리 인스턴스일 때 SetDirty/SaveAssets 는 디스크에 아무것도 쓰지 않음
 *         AssetDatabase.CreateAsset() 호출 전까지 .asset 파일이 존재하지 않으므로 저장 불가
 *
 * =============================================================================
 */
#endif

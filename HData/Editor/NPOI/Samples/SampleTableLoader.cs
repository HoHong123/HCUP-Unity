using System.Collections.Generic;
using UnityEditor;
using Newtonsoft.Json.Linq;
using HData.NPOI.Core;

namespace HData.NPOI.Samples {
    // ExcelLoader<T> 상속 예시 — Excel → SampleTable SO 변환 Loader
    public class SampleTableLoader : ExcelLoader<SampleTableLoader> {
        protected override string[] keys => new[] { "id", "name", "value" };

        public override void ImportData() {
            var arr = ExcelToJson();
            var rows = new List<SampleData>();
            foreach (var token in arr) {
                if (token is not JObject json)
                    continue;
                rows.Add(new SampleData {
                    Id = json["id"]?.Value<int>() ?? 0,
                    Name = json["name"]?.Value<string>() ?? string.Empty,
                    Value = json["value"]?.Value<int>() ?? 0,
                });
            }
            SampleTable.Instance.SetRows(rows);
            EditorUtility.SetDirty(SampleTable.Instance);
            AssetDatabase.SaveAssets();
        }

        public override void ExportData() {
            var arr = new JArray();
            foreach (var row in SampleTable.Instance.Rows) {
                arr.Add(new JObject {
                    ["id"] = row.Id,
                    ["name"] = row.Name,
                    ["value"] = row.Value,
                });
            }
            JsonToExcel(arr, "SampleTable");
        }
    }
}

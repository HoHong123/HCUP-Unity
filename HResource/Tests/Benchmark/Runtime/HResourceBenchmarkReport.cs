#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 벤치마크 결과를 모아 CSV 와 마크다운 요약으로 씁니다.
 *
 * 특징 / 지원기능 ::
 * CSV 는 모든 측정 행을 그대로, 마크다운은 (시나리오, 규모, 크기) 별 중앙값과 예산 판정을 담습니다.
 * + 시나리오와 크기별로 동기 비용이 예산을 처음 넘는 규모를 따로 적습니다
 *
 * 주의사항 ::
 * 파일은 persistentDataPath/HResourceBenchmark/ 에 씁니다. 에디터와 플레이어의 경로가 같으므로
 * 파일 이름에 실행 환경(editor / player)을 넣어 구분합니다.
 * =========================================================
 */
#endif

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using HDiagnosis.Logger;
using UnityEngine;

namespace HResource.Benchmark {
    public sealed class HResourceBenchmarkReport {
        #region 상수
        const string OUTPUT_FOLDER = "HResourceBenchmark";
        const string MISSING = "-";
        const string OVER = "OVER";
        const string WITHIN = "ok";
        #endregion

        #region Fields
        readonly List<HResourceBenchmarkSample> samples = new();
        readonly string environment;
        readonly string codeGeneration;
        readonly double frameBudgetMs;
        #endregion

        #region 생성자
        public HResourceBenchmarkReport(double frameBudgetMs) {
            this.frameBudgetMs = frameBudgetMs;
            environment = Application.isEditor ? "editor" : "player";
            codeGeneration = _DetectCodeGeneration();
        }
        #endregion

        #region Public - Collect
        public void Add(HResourceBenchmarkSample sample) {
            samples.Add(sample);
            HLogger.Log("[HResourceBenchmark] " + _FormatLogLine(sample));
        }
        #endregion

        #region Public - Write
        /// <summary> CSV 와 마크다운을 쓰고 마크다운 경로를 돌려준다 </summary>
        public string Write() {
            string folder = Path.Combine(Application.persistentDataPath, OUTPUT_FOLDER);
            Directory.CreateDirectory(folder);

            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            string basePath = Path.Combine(folder, stamp + "_" + environment);

            File.WriteAllText(basePath + ".csv", _BuildCsv(), Encoding.UTF8);
            File.WriteAllText(basePath + ".md", _BuildMarkdown(), Encoding.UTF8);
            return basePath + ".md";
        }
        #endregion

        #region Private - Csv
        string _BuildCsv() {
            var builder = new StringBuilder();
            builder.AppendLine("environment,scenario,count,payloadKB,repeat,issueMs,completeMs,frames,maxFrameMs,issueFrameGcKB,issueGcBytesPerUnit,teardownMs,managedBytesPerOwner,nativeBytesPerOwner");
            for (int k = 0; k < samples.Count; k++) {
                HResourceBenchmarkSample sample = samples[k];
                builder.Append(environment).Append(',')
                    .Append(sample.Scenario).Append(',')
                    .Append(sample.Count).Append(',')
                    .Append(sample.PayloadKB).Append(',')
                    .Append(sample.RepeatIndex).Append(',')
                    .Append(_Number(sample.IssueMs)).Append(',')
                    .Append(_Number(sample.CompleteMs)).Append(',')
                    .Append(sample.Frames).Append(',')
                    .Append(_Number(sample.MaxFrameMs)).Append(',')
                    .Append(_Number(sample.IssueFrameGcKB)).Append(',')
                    .Append(_Number(sample.IssueGcBytesPerUnit)).Append(',')
                    .Append(_Number(sample.TeardownMs)).Append(',')
                    .Append(_Number(sample.ManagedBytesPerOwner)).Append(',')
                    .Append(_Number(sample.NativeBytesPerOwner))
                    .AppendLine();
            }
            return builder.ToString();
        }
        #endregion

        #region Private - Markdown
        string _BuildMarkdown() {
            var builder = new StringBuilder();
            builder.AppendLine("# HResource Benchmark");
            builder.AppendLine();
            builder.AppendLine("- environment: " + environment);
            builder.AppendLine("- unity: " + Application.unityVersion);
            builder.AppendLine("- cpu: " + SystemInfo.processorType);
            builder.AppendLine("- HResource code generation: " + codeGeneration);
            builder.AppendLine("- frame budget: " + _Number(frameBudgetMs) + " ms");
            builder.AppendLine("- values: median of repeats. verdict compares issueMs and teardownMs with the frame budget");
            if (Application.isEditor) {
                builder.AppendLine("- editor note: with the Addressables 'Use Asset Database' play mode every load has an artificial delay (0.1s by default), so completeMs is not a load cost");
            }
            builder.AppendLine();
            builder.AppendLine("| scenario | count | payloadKB | issueMs | completeMs | frames | maxFrameMs | issueFrameGcKB | gcB/unit | teardownMs | managedB/owner | nativeB/owner | verdict |");
            builder.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|");

            List<List<HResourceBenchmarkSample>> groups = _GroupSamples();
            for (int k = 0; k < groups.Count; k++) {
                List<HResourceBenchmarkSample> group = groups[k];
                HResourceBenchmarkSample head = group[0];
                double issueMs = _Median(group, sample => sample.IssueMs);
                double teardownMs = _Median(group, sample => sample.TeardownMs);

                builder.Append("| ").Append(head.Scenario)
                    .Append(" | ").Append(head.Count)
                    .Append(" | ").Append(head.PayloadKB)
                    .Append(" | ").Append(_Number(issueMs))
                    .Append(" | ").Append(_Number(_Median(group, sample => sample.CompleteMs)))
                    .Append(" | ").Append(_Number(_Median(group, sample => sample.Frames)))
                    .Append(" | ").Append(_Number(_Median(group, sample => sample.MaxFrameMs)))
                    .Append(" | ").Append(_Number(_Median(group, sample => sample.IssueFrameGcKB)))
                    .Append(" | ").Append(_Number(_Median(group, sample => sample.IssueGcBytesPerUnit)))
                    .Append(" | ").Append(_Number(teardownMs))
                    .Append(" | ").Append(_Number(_Median(group, sample => sample.ManagedBytesPerOwner)))
                    .Append(" | ").Append(_Number(_Median(group, sample => sample.NativeBytesPerOwner)))
                    .Append(" | ").Append(_IsOverBudget(issueMs, teardownMs) ? OVER : WITHIN)
                    .AppendLine(" |");
            }

            builder.AppendLine();
            builder.AppendLine("## First count over budget");
            builder.AppendLine();
            _AppendFirstOverBudget(builder, groups);
            return builder.ToString();
        }

        void _AppendFirstOverBudget(StringBuilder builder, List<List<HResourceBenchmarkSample>> groups) {
            // (시나리오, 크기) 마다 규모 오름차순으로 처음 예산을 넘는 행을 찾는다.
            var firstOver = new Dictionary<string, int>();
            var seen = new List<string>();
            for (int k = 0; k < groups.Count; k++) {
                HResourceBenchmarkSample head = groups[k][0];
                string series = head.Scenario + " @ " + head.PayloadKB + "KB";
                if (!seen.Contains(series)) seen.Add(series);
                if (firstOver.ContainsKey(series)) continue;

                double issueMs = _Median(groups[k], sample => sample.IssueMs);
                double teardownMs = _Median(groups[k], sample => sample.TeardownMs);
                if (_IsOverBudget(issueMs, teardownMs)) firstOver[series] = head.Count;
            }

            for (int k = 0; k < seen.Count; k++) {
                string verdict = firstOver.TryGetValue(seen[k], out int count) ? count.ToString(CultureInfo.InvariantCulture) : "none within tested range";
                builder.Append("- ").Append(seen[k]).Append(" : ").AppendLine(verdict);
            }
        }

        List<List<HResourceBenchmarkSample>> _GroupSamples() {
            var groups = new List<List<HResourceBenchmarkSample>>();
            var indexByKey = new Dictionary<string, int>();
            for (int k = 0; k < samples.Count; k++) {
                HResourceBenchmarkSample sample = samples[k];
                string key = sample.Scenario + "|" + sample.Count + "|" + sample.PayloadKB;
                if (!indexByKey.TryGetValue(key, out int index)) {
                    index = groups.Count;
                    indexByKey[key] = index;
                    groups.Add(new List<HResourceBenchmarkSample>());
                }
                groups[index].Add(sample);
            }
            return groups;
        }

        bool _IsOverBudget(double issueMs, double teardownMs) {
            return (!double.IsNaN(issueMs) && issueMs > frameBudgetMs)
                || (!double.IsNaN(teardownMs) && teardownMs > frameBudgetMs);
        }
        #endregion

        #region Private - Environment
        // 디버그 코드 생성은 async 상태 기계를 class 로 만들어 동기 완료에도 할당한다. GC 수치 해석이 이것에 달려 있다.
        static string _DetectCodeGeneration() {
            Assembly assembly = typeof(HResource.Provider.AssetProviderFactory).Assembly;
            var debuggable = assembly.GetCustomAttribute<System.Diagnostics.DebuggableAttribute>();
            return debuggable != null && debuggable.IsJITOptimizerDisabled ? "debug (JIT optimizer disabled)" : "release";
        }
        #endregion

        #region Private - Format
        static double _Median(List<HResourceBenchmarkSample> group, Func<HResourceBenchmarkSample, double> selector) {
            var values = new List<double>(group.Count);
            for (int k = 0; k < group.Count; k++) {
                double value = selector(group[k]);
                if (!double.IsNaN(value)) values.Add(value);
            }
            if (values.Count < 1) return double.NaN;

            values.Sort();
            int middle = values.Count / 2;
            return values.Count % 2 == 1 ? values[middle] : (values[middle - 1] + values[middle]) * 0.5d;
        }

        static string _Number(double value) {
            return double.IsNaN(value) ? MISSING : value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        static string _FormatLogLine(HResourceBenchmarkSample sample) {
            return sample.Scenario + " count=" + sample.Count + " payloadKB=" + sample.PayloadKB + " repeat=" + sample.RepeatIndex
                + " issueMs=" + _Number(sample.IssueMs) + " completeMs=" + _Number(sample.CompleteMs)
                + " frames=" + sample.Frames + " maxFrameMs=" + _Number(sample.MaxFrameMs)
                + " issueFrameGcKB=" + _Number(sample.IssueFrameGcKB) + " gcB/unit=" + _Number(sample.IssueGcBytesPerUnit)
                + " teardownMs=" + _Number(sample.TeardownMs)
                + " managedB/owner=" + _Number(sample.ManagedBytesPerOwner) + " nativeB/owner=" + _Number(sample.NativeBytesPerOwner);
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * Dev Log
 * =========================================================
 * 2026-09-23 (수정) :: 코드 생성 모드 기록
 *
 * 변경 ::
 * 보고서 머리에 HResource 어셈블리의 코드 생성 모드(DebuggableAttribute.IsJITOptimizerDisabled)를 적는다.
 *
 * 이유 ::
 * 디버그 코드 생성은 async 상태 기계를 class 로 만들어 동기 완료에도 할당한다. 같은 코드의 GC 수치가 모드에 따라 약 4 배 달라진다.
 * 개발 빌드 플레이어도 디버그 코드 생성이었다(이 기록으로 처음 확인).
 *
 * =========================================================
 * 2026-09-23 (최초 설계) :: 결과 보고서
 *
 * 변경 ::
 * 측정 행을 모아 CSV(원자료)와 마크다운(중앙값 요약, 예산 판정, 처음 예산을 넘는 규모)으로 쓴다.
 *
 * 이유 ::
 * 에디터와 플레이어 결과를 같은 형식으로 비교해야 하고, 반복 측정의 튀는 값은 중앙값으로 누른다.
 *
 * 주의 ::
 * 판정은 IssueMs 와 TeardownMs 만 본다. 둘은 HResource 가 한 프레임에 더하는 동기 비용이다.
 * MaxFrameMs 는 렌더링과 테스트 러너 비용이 섞여 있어 참고값으로만 쓴다.
 * 이 파일은 측정 종료 후에만 할당하므로 측정 구간에는 영향이 없다.
 * =========================================================
 */
#endif

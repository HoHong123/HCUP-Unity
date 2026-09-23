#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * HResource 를 거치지 않고 Addressables 를 직접 부르는 기준선 측정입니다.
 *
 * 특징 / 지원기능 ::
 * + RawSameKey / RawReleaseAll : 같은 key 를 N 번 LoadAssetAsync 하고 N 개 핸들을 반납
 * + RawMultiKey / RawMultiKeyRelease : 서로 다른 key M 개를 LoadAssetAsync 하고 반납
 * HResource 시나리오(FirstRequest, DestroyAll, MultiKey, MultiKeyRelease)와 같은 규모로 재어 차이를 패키지 비용으로 읽습니다.
 *
 * 주의사항 ::
 * 기준선에는 소유권, 파괴 감지, 캐시가 없습니다. 차이는 그 기능들의 값입니다.
 * 반납은 핸들마다 Addressables.Release 를 부릅니다. HResource 의 파괴 연쇄와 달리 GameObject 파괴 비용은 없습니다.
 * =========================================================
 */
#endif

using System.Collections;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.TestTools;

namespace HResource.Benchmark {
    public sealed partial class HResourceBenchmarkTests {
        #region 상수 - Baseline
        const string SCENARIO_RAW_SAME_KEY = "RawSameKey";
        const string SCENARIO_RAW_RELEASE_ALL = "RawReleaseAll";
        const string SCENARIO_RAW_MULTI_KEY = "RawMultiKey";
        const string SCENARIO_RAW_MULTI_KEY_RELEASE = "RawMultiKeyRelease";
        #endregion

        #region Tests - Baseline
        [UnityTest]
        [Timeout(TIMEOUT_MS)]
        public IEnumerator RawAddressablesBaseline() => UniTask.ToCoroutine(_RunRawBaselineAsync);
        #endregion

        #region Private - Baseline
        async UniTask _RunRawBaselineAsync() {
            int payloadKB = config.PayloadSizesKB[0];
            string payloadAddress = HResourceBenchmarkConfig.PayloadAddress(payloadKB);

            // JIT 과 Addressables 초기화를 측정 밖으로 뺀다.
            await _MeasureRawAsync(_RepeatAddress(payloadAddress, WARMUP_OWNER_COUNT), SCENARIO_RAW_SAME_KEY, SCENARIO_RAW_RELEASE_ALL, payloadKB, 0);

            for (int n = 0; n < config.OwnerCounts.Length; n++) {
                string[] addresses = _RepeatAddress(payloadAddress, config.OwnerCounts[n]);
                for (int r = 0; r < config.Repeat; r++) {
                    HResourceBenchmarkSample[] samples = await _MeasureRawAsync(addresses, SCENARIO_RAW_SAME_KEY, SCENARIO_RAW_RELEASE_ALL, payloadKB, r);
                    for (int k = 0; k < samples.Length; k++) report.Add(samples[k]);
                }
            }

            for (int m = 0; m < config.KeyCounts.Length; m++) {
                string[] addresses = _KeyAddresses(config.KeyCounts[m]);
                for (int r = 0; r < config.Repeat; r++) {
                    HResourceBenchmarkSample[] samples = await _MeasureRawAsync(addresses, SCENARIO_RAW_MULTI_KEY, SCENARIO_RAW_MULTI_KEY_RELEASE, HResourceBenchmarkConfig.KEY_PAYLOAD_KB, r);
                    for (int k = 0; k < samples.Length; k++) report.Add(samples[k]);
                }
            }
        }

        async UniTask<HResourceBenchmarkSample[]> _MeasureRawAsync(string[] addresses, string issueScenario, string releaseScenario, int payloadKB, int repeatIndex) {
            var handles = new AsyncOperationHandle<TextAsset>[addresses.Length];
            await UniTask.NextFrame();
            using ProfilerRecorder gcRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, GC_ALLOCATED_IN_FRAME);

            long issueStart = Stopwatch.GetTimestamp();
            for (int k = 0; k < addresses.Length; k++) handles[k] = Addressables.LoadAssetAsync<TextAsset>(addresses[k]);

            var issue = new IssueResult {
                IssueMs = _ElapsedMs(issueStart),
                IssueFrameGcKB = double.NaN,
            };
            issue = await _CountFramesUntilAsync(() => _AreAllDone(handles), gcRecorder, issue);
            issue.CompleteMs = _ElapsedMs(issueStart);

            for (int k = 0; k < handles.Length; k++) {
                Assert.IsNotNull(handles[k].Result,
                    "[HResourceBenchmark] Raw load " + k + " of '" + addresses[k] + "' returned null. Check that the benchmark Addressables group was built.");
            }

            long releaseStart = Stopwatch.GetTimestamp();
            for (int k = 0; k < handles.Length; k++) Addressables.Release(handles[k]);
            double releaseMs = _ElapsedMs(releaseStart);

            var releaseSample = new HResourceBenchmarkSample {
                Scenario = releaseScenario,
                Count = addresses.Length,
                PayloadKB = payloadKB,
                RepeatIndex = repeatIndex,
                TeardownMs = releaseMs,
            };

            return new[] {
                _ToSample(issueScenario, addresses.Length, payloadKB, repeatIndex, issue),
                releaseSample,
            };
        }

        static bool _AreAllDone(AsyncOperationHandle<TextAsset>[] handles) {
            for (int k = 0; k < handles.Length; k++) {
                if (!handles[k].IsDone) return false;
            }
            return true;
        }

        static string[] _RepeatAddress(string address, int count) {
            var addresses = new string[count];
            for (int k = 0; k < count; k++) addresses[k] = address;
            return addresses;
        }

        static string[] _KeyAddresses(int count) {
            var addresses = new string[count];
            for (int k = 0; k < count; k++) addresses[k] = HResourceBenchmarkConfig.KeyAddress(k);
            return addresses;
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * Dev Log
 * =========================================================
 * 2026-09-23 (최초 설계) :: Addressables 직접 호출 기준선
 *
 * 변경 ::
 * HResource 없이 LoadAssetAsync / Release 를 직접 부르는 시나리오 두 벌을 같은 보고서에 넣었다.
 *
 * 이유 ::
 * 첫 벤치에서 새 key 첫 로드가 키당 100~130us 로 나왔지만, 그중 패키지 몫과 Addressables 몫을 나눌 수 없었다.
 *
 * 주의 ::
 * 주소 배열은 측정 구간 밖에서 만든다. 핸들 배열도 루프 전에 만든다.
 * 기준선 반납은 핸들 반납뿐이고 HResource DestroyAll 은 GameObject 파괴까지 포함한다. 둘을 직접 빼지 말고 참고로만 비교한다.
 * =========================================================
 */
#endif

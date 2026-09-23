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
 * 같은 key 시나리오의 페이로드는 PayloadSizesKB[0] 하나만 잽니다. 다른 크기와 비교하지 않습니다.
 * 반납은 핸들마다 Addressables.Release 를 부릅니다. HResource 의 파괴 연쇄와 달리 GameObject 파괴 비용은 없습니다.
 * =========================================================
 */
#endif

using System;
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

            // JIT 과 Addressables 초기화를 측정 밖으로 뺀다. HResource 시나리오처럼 두 경로를 각각 데운다.
            await _MeasureRawAsync(_RepeatAddress(payloadAddress, WARMUP_OWNER_COUNT), SCENARIO_RAW_SAME_KEY, SCENARIO_RAW_RELEASE_ALL, payloadKB, 0);
            await _MeasureRawAsync(_KeyAddresses(Math.Min(WARMUP_OWNER_COUNT, config.MaxKeyCount())), SCENARIO_RAW_MULTI_KEY, SCENARIO_RAW_MULTI_KEY_RELEASE, HResourceBenchmarkConfig.KEY_PAYLOAD_KB, 0);

            for (int k = 0; k < config.OwnerCounts.Length; k++) {
                string[] addresses = _RepeatAddress(payloadAddress, config.OwnerCounts[k]);
                await _RecordRepeatsAsync(repeat => _MeasureRawAsync(addresses, SCENARIO_RAW_SAME_KEY, SCENARIO_RAW_RELEASE_ALL, payloadKB, repeat));
            }

            for (int k = 0; k < config.KeyCounts.Length; k++) {
                string[] addresses = _KeyAddresses(config.KeyCounts[k]);
                await _RecordRepeatsAsync(repeat => _MeasureRawAsync(addresses, SCENARIO_RAW_MULTI_KEY, SCENARIO_RAW_MULTI_KEY_RELEASE, HResourceBenchmarkConfig.KEY_PAYLOAD_KB, repeat));
            }
        }

        async UniTask<HResourceBenchmarkSample[]> _MeasureRawAsync(string[] addresses, string issueScenario, string releaseScenario, int payloadKB, int repeatIndex) {
            var handles = new AsyncOperationHandle<TextAsset>[addresses.Length];
            bool released = false;

            try {
                // HResource 시나리오와 같은 조건으로 재기 위해 이슈 직전에 GC 를 정리한다.
                await _SettleMemoryAsync();
                // 강제 GC 가 끝난 프레임을 넘겨 HResource 쪽 _IssueAsync 처럼 프레임 경계에서 시작한다.
                await UniTask.NextFrame();
                using ProfilerRecorder gcRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, GC_ALLOCATED_IN_FRAME);

                long issueStart = Stopwatch.GetTimestamp();
                for (int k = 0; k < addresses.Length; k++) handles[k] = Addressables.LoadAssetAsync<TextAsset>(addresses[k]);

                var issue = new IssueResult {
                    IssueMs = _ElapsedMs(issueStart),
                    IssueFrameGcKB = double.NaN,
                };
                issue = await _CountFramesUntilAsync(handles, static pending => _AreAllDone(pending), gcRecorder, issue);
                issue.CompleteMs = _ElapsedMs(issueStart);

                for (int k = 0; k < handles.Length; k++) {
                    Assert.IsNotNull(handles[k].Result,
                        "[HResourceBenchmark] Raw load " + k + " of '" + addresses[k] + "' returned null. Check that the benchmark Addressables group was built.");
                }

                long releaseStart = Stopwatch.GetTimestamp();
                for (int k = 0; k < handles.Length; k++) Addressables.Release(handles[k]);
                double releaseMs = _ElapsedMs(releaseStart);
                released = true;

                return _ToRawSamples(issue, releaseMs, addresses.Length, issueScenario, releaseScenario, payloadKB, repeatIndex);
            }
            finally {
                // 단언이 실패해도 잡은 핸들은 돌려준다. 남기면 다음 측정이 이미 로드된 번들 위에서 돈다.
                if (!released) _ReleaseValid(handles);
            }
        }

        static HResourceBenchmarkSample[] _ToRawSamples(
            IssueResult issue, double releaseMs, int count, string issueScenario, string releaseScenario, int payloadKB, int repeatIndex) {

            var releaseSample = new HResourceBenchmarkSample {
                Scenario = releaseScenario,
                Count = count,
                PayloadKB = payloadKB,
                RepeatIndex = repeatIndex,
                TeardownMs = releaseMs,
            };

            return new[] {
                _ToSample(issueScenario, count, payloadKB, repeatIndex, issue),
                releaseSample,
            };
        }

        static bool _AreAllDone(AsyncOperationHandle<TextAsset>[] handles) {
            for (int k = 0; k < handles.Length; k++) {
                if (!handles[k].IsDone) return false;
            }
            return true;
        }

        // 반납된 핸들은 IsValid 가 false 라 두 번 반납하지 않는다. 발행되지 않은 칸은 기본값이라 역시 false 다.
        static void _ReleaseValid(AsyncOperationHandle<TextAsset>[] handles) {
            for (int k = 0; k < handles.Length; k++) {
                if (handles[k].IsValid()) Addressables.Release(handles[k]);
            }
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
 * 2026-09-23 (수정) :: 검수 반영 - HResource 시나리오와 조건 정렬
 *
 * 변경 ::
 * 다른 key 경로도 따로 데운다. 이슈 직전에 _SettleMemoryAsync 를 부른다.
 * 이슈 직전에 NextFrame 을 한 번 넘겨 프레임 경계에서 시작한다. 측정을 try / finally 로 감싸 단언이 실패해도 유효한 핸들을 반납한다(_ReleaseValid).
 * 대기를 상태 인자 형태의 _CountFramesUntilAsync 로 바꿨다. 반복 루프를 _RecordRepeatsAsync 로 묶고 인덱스를 k 로 바꿨다.
 *
 * 이유 ::
 * HResource 쪽은 두 경로를 데우고 GC 를 정리한 뒤 재는데 기준선은 그러지 않아, 차이를 패키지 몫으로 읽을 수 없었다.
 * 단언 실패 시 핸들이 남으면 다음 측정이 이미 로드된 번들 위에서 돈다.
 *
 * 결과 ::
 * 기준선과 HResource 시나리오가 같은 데우기, 같은 GC 정리, 같은 대기 루프로 잰다. 페이로드는 여전히 PayloadSizesKB[0] 하나다.
 *
 * 주의 ::
 * 반납된 핸들은 IsValid 가 false 라 finally 에서 두 번 반납하지 않는다.
 *
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

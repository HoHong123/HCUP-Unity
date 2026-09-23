#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * HResource 의 한계를 재는 PlayMode 벤치마크입니다. 에디터와 플레이어에서 같은 코드가 돕니다.
 *
 * 특징 / 지원기능 ::
 * + BurstLifecycle : N 개 오브젝트의 첫 요청 버스트 -> 같은 소유자의 반복 요청 -> 동시 파괴
 * + MultiKey : 오브젝트 하나가 서로 다른 key M 개를 한 프레임에 요청 -> 일괄 반납
 * + 소유자당 관리 · 네이티브 메모리 증가량
 *
 * 주의사항 ::
 * 측정 중에는 vSync 와 targetFrameRate 를 끄고 끝나면 되돌립니다.
 * 파괴는 DestroyImmediate 로 동기 비용을 잽니다. 프로브 OnDestroy 와 회수 연쇄가 그 안에서 돕니다.
 * 준비(더미 에셋, 임시 Addressables 그룹, 설정 JSON)는 에디터 어셈블리의 HResourceBenchmarkSetup 이 맡습니다.
 *
 * 사용 ::
 * Test Runner > PlayMode 에서 실행하거나 Run on player 로 개발 빌드에서 실행합니다.
 * 결과 : persistentDataPath/HResourceBenchmark/{시각}_{editor|player}.md / .csv
 * =========================================================
 */
#endif

using System;
using System.Collections;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using HDiagnosis.Logger;
using HResource.Data;
using HResource.Provider;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace HResource.Benchmark {
    [PrebuildSetup("HResource.Benchmark.Editor.HResourceBenchmarkSetup")]
    [PostBuildCleanup("HResource.Benchmark.Editor.HResourceBenchmarkSetup")]
    public sealed partial class HResourceBenchmarkTests {
        #region Nested Types
        struct IssueResult {
            public double IssueMs;
            public double CompleteMs;
            public int Frames;
            public double MaxFrameMs;
            public double IssueFrameGcKB;
        }
        #endregion

        #region 상수
        const int TIMEOUT_MS = 3600000;
        const int WARMUP_OWNER_COUNT = 10;
        const int NO_TARGET_FRAME_RATE = -1;
        const double BYTES_PER_KB = 1024d;
        const double MILLISECONDS_PER_SECOND = 1000d;
        const string GC_ALLOCATED_IN_FRAME = "GC Allocated In Frame";
        const string QUIT_WHEN_DONE_ARGUMENT = "-hresourceBenchmarkQuitWhenDone";
        const string SCENARIO_FIRST_REQUEST = "FirstRequest";
        const string SCENARIO_REPEAT_REQUEST = "RepeatRequest";
        const string SCENARIO_DESTROY_ALL = "DestroyAll";
        const string SCENARIO_MULTI_KEY = "MultiKey";
        const string SCENARIO_MULTI_KEY_RELEASE = "MultiKeyRelease";
        #endregion

        #region Fields
        HResourceBenchmarkConfig config;
        HResourceBenchmarkReport report;
        int savedVSyncCount;
        int savedTargetFrameRate;
        #endregion

        #region NUnit
        [OneTimeSetUp]
        public void OneTimeSetUp() {
            config = HResourceBenchmarkConfig.Load();
            report = new HResourceBenchmarkReport(config.FrameBudgetMs);

            savedVSyncCount = QualitySettings.vSyncCount;
            savedTargetFrameRate = Application.targetFrameRate;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = NO_TARGET_FRAME_RATE;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() {
            QualitySettings.vSyncCount = savedVSyncCount;
            Application.targetFrameRate = savedTargetFrameRate;

            if (report == null) return;
            string path = report.Write();
            HLogger.Log("[HResourceBenchmark] Report written to " + path);

            // 에디터와 연결되지 않은 플레이어는 테스트가 끝나도 스스로 종료하지 않는다.
            if (!Application.isEditor && _HasCommandLineArgument(QUIT_WHEN_DONE_ARGUMENT)) Application.Quit();
        }
        #endregion

        #region Tests
        [UnityTest]
        [Timeout(TIMEOUT_MS)]
        public IEnumerator BurstLifecycle() => UniTask.ToCoroutine(_RunBurstLifecycleAsync);

        [UnityTest]
        [Timeout(TIMEOUT_MS)]
        public IEnumerator MultiKey() => UniTask.ToCoroutine(_RunMultiKeyAsync);
        #endregion

        #region Private - Burst Lifecycle
        async UniTask _RunBurstLifecycleAsync() {
            // JIT 과 Addressables 초기화를 측정 밖으로 뺀다.
            await _MeasureBurstAsync(WARMUP_OWNER_COUNT, config.PayloadSizesKB[0], 0);

            for (int k = 0; k < config.PayloadSizesKB.Length; k++) {
                for (int n = 0; n < config.OwnerCounts.Length; n++) {
                    int payloadKB = config.PayloadSizesKB[k];
                    int ownerCount = config.OwnerCounts[n];
                    await _RecordRepeatsAsync(repeat => _MeasureBurstAsync(ownerCount, payloadKB, repeat));
                }
            }
        }

        /// <summary> 같은 조건을 설정의 반복 횟수만큼 재고 결과를 보고서에 담는다 </summary>
        async UniTask _RecordRepeatsAsync(Func<int, UniTask<HResourceBenchmarkSample[]>> measure) {
            for (int k = 0; k < config.Repeat; k++) {
                HResourceBenchmarkSample[] samples = await measure(k);
                for (int n = 0; n < samples.Length; n++) report.Add(samples[n]);
            }
        }

        async UniTask<HResourceBenchmarkSample[]> _MeasureBurstAsync(int ownerCount, int payloadKB, int repeatIndex) {
            string address = HResourceBenchmarkConfig.PayloadAddress(payloadKB);
            IAssetSource<string, TextAsset> source = AssetProviderFactory.CreateAddressable<TextAsset>();
            BenchmarkOwner[] owners = _CreateOwners(ownerCount);

            try {
                long managedBefore = await _SettleMemoryAsync();
                long nativeBefore = Profiler.GetTotalAllocatedMemoryLong();

                Func<int, UniTask<TextAsset>> request = k => source.GetAsync(owners[k], address, AssetLoadMode.Addressable);
                IssueResult first = await _IssueAsync(request, ownerCount);

                long managedAfter = await _SettleMemoryAsync();
                long nativeAfter = Profiler.GetTotalAllocatedMemoryLong();

                IssueResult repeat = await _IssueAsync(request, ownerCount);

                long destroyStart = Stopwatch.GetTimestamp();
                for (int k = 0; k < owners.Length; k++) Object.DestroyImmediate(owners[k].gameObject);
                double destroyMs = _ElapsedMs(destroyStart);

                Assert.IsFalse(source.TryGet(address, out _),
                    "[HResourceBenchmark] '" + address + "' is still cached after every owner was destroyed. The probe reclaim chain did not run.");

                HResourceBenchmarkSample firstSample = _ToSample(SCENARIO_FIRST_REQUEST, ownerCount, payloadKB, repeatIndex, first);
                firstSample.ManagedBytesPerOwner = (double)(managedAfter - managedBefore) / ownerCount;
                firstSample.NativeBytesPerOwner = (double)(nativeAfter - nativeBefore) / ownerCount;

                var destroySample = new HResourceBenchmarkSample {
                    Scenario = SCENARIO_DESTROY_ALL,
                    Count = ownerCount,
                    PayloadKB = payloadKB,
                    RepeatIndex = repeatIndex,
                    TeardownMs = destroyMs,
                };

                return new[] {
                    firstSample,
                    _ToSample(SCENARIO_REPEAT_REQUEST, ownerCount, payloadKB, repeatIndex, repeat),
                    destroySample,
                };
            }
            finally {
                _DestroyRemaining(owners);
                source.Dispose();
            }
        }
        #endregion

        #region Private - Multi Key
        async UniTask _RunMultiKeyAsync() {
            // 준비 단계는 설정의 최대 key 수만큼만 에셋을 만든다.
            await _MeasureMultiKeyAsync(Math.Min(WARMUP_OWNER_COUNT, config.MaxKeyCount()), 0);

            for (int k = 0; k < config.KeyCounts.Length; k++) {
                int keyCount = config.KeyCounts[k];
                await _RecordRepeatsAsync(repeat => _MeasureMultiKeyAsync(keyCount, repeat));
            }
        }

        async UniTask<HResourceBenchmarkSample[]> _MeasureMultiKeyAsync(int keyCount, int repeatIndex) {
            IAssetSource<string, TextAsset> source = AssetProviderFactory.CreateAddressable<TextAsset>();
            BenchmarkOwner[] owners = _CreateOwners(1);
            BenchmarkOwner owner = owners[0];

            try {
                // 주소 문자열은 측정 구간 밖에서 만든다. 안에서 만들면 그 할당과 시간이 요청 비용에 섞인다.
                var addresses = new string[keyCount];
                for (int k = 0; k < keyCount; k++) addresses[k] = HResourceBenchmarkConfig.KeyAddress(k);

                // 기준선과 같은 조건으로 재기 위해 이슈 직전에 GC 를 정리한다.
                await _SettleMemoryAsync();
                IssueResult issue = await _IssueAsync(k => source.GetAsync(owner, addresses[k], AssetLoadMode.Addressable), keyCount);

                long releaseStart = Stopwatch.GetTimestamp();
                int released = source.ReleaseOwner(owner);
                double releaseMs = _ElapsedMs(releaseStart);

                Assert.AreEqual(keyCount, released,
                    "[HResourceBenchmark] ReleaseOwner released " + released + " of " + keyCount + " keys. Check the owner table.");

                var releaseSample = new HResourceBenchmarkSample {
                    Scenario = SCENARIO_MULTI_KEY_RELEASE,
                    Count = keyCount,
                    PayloadKB = HResourceBenchmarkConfig.KEY_PAYLOAD_KB,
                    RepeatIndex = repeatIndex,
                    TeardownMs = releaseMs,
                };

                return new[] {
                    _ToSample(SCENARIO_MULTI_KEY, keyCount, HResourceBenchmarkConfig.KEY_PAYLOAD_KB, repeatIndex, issue),
                    releaseSample,
                };
            }
            finally {
                _DestroyRemaining(owners);
                source.Dispose();
            }
        }
        #endregion

        #region Private - Measure
        /// <summary> 요청 count 개를 한 프레임에 걸고, 모두 끝날 때까지 프레임을 세며 기다린다 </summary>
        async UniTask<IssueResult> _IssueAsync(Func<int, UniTask<TextAsset>> request, int count) {
            // 측정 도구 자신의 할당(결과 배열)은 이슈 프레임 밖에서 만든다. 기준선과 같은 조건이다.
            var tasks = new UniTask<TextAsset>[count];
            // 프레임 경계에서 시작해야 이슈 프레임의 GC 와 프레임 시간이 그 버스트만 담는다.
            await UniTask.NextFrame();
            using ProfilerRecorder gcRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, GC_ALLOCATED_IN_FRAME);

            long issueStart = Stopwatch.GetTimestamp();
            for (int k = 0; k < count; k++) tasks[k] = request(k);

            var result = new IssueResult {
                IssueMs = _ElapsedMs(issueStart),
                IssueFrameGcKB = double.NaN,
            };
            // WhenAll 은 이슈 프레임에 promise 와 결과 배열을 할당한다. 기준선처럼 상태만 확인한다.
            result = await _CountFramesUntilAsync(tasks, static pending => _AreAllCompleted(pending), gcRecorder, result);
            result.CompleteMs = _ElapsedMs(issueStart);

            for (int k = 0; k < tasks.Length; k++) {
                TextAsset asset = await tasks[k];
                Assert.IsNotNull(asset,
                    "[HResourceBenchmark] Request " + k + " returned null. Check that the benchmark Addressables group was built.");
            }
            return result;
        }

        /// <summary> 조건이 참이 될 때까지 프레임을 넘기며 프레임 수, 최대 프레임 시간, 이슈 프레임 GC 를 채운다 </summary>
        // 대기 대상을 인자로 받는다. 대상을 캡처하는 람다를 넘기면 측정 구간에 클로저가 할당된다.
        static async UniTask<IssueResult> _CountFramesUntilAsync<TState>(TState state, Func<TState, bool> isDone, ProfilerRecorder gcRecorder, IssueResult result) {
            // 모두 동기로 끝났어도 이슈 프레임의 값을 읽으려면 한 프레임은 넘겨야 한다.
            do {
                await UniTask.NextFrame();
                result.Frames++;
                result.MaxFrameMs = Math.Max(result.MaxFrameMs, Time.unscaledDeltaTime * MILLISECONDS_PER_SECOND);
                if (result.Frames == 1 && gcRecorder.Valid) result.IssueFrameGcKB = gcRecorder.LastValue / BYTES_PER_KB;
            } while (!isDone(state));
            return result;
        }

        static bool _AreAllCompleted(UniTask<TextAsset>[] tasks) {
            for (int k = 0; k < tasks.Length; k++) {
                if (!tasks[k].Status.IsCompleted()) return false;
            }
            return true;
        }

        static async UniTask<long> _SettleMemoryAsync() {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            await UniTask.NextFrame();
            return GC.GetTotalMemory(true);
        }

        static bool _HasCommandLineArgument(string argument) {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int k = 0; k < arguments.Length; k++) {
                if (string.Equals(arguments[k], argument, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        static double _ElapsedMs(long startTimestamp) {
            return (Stopwatch.GetTimestamp() - startTimestamp) * MILLISECONDS_PER_SECOND / Stopwatch.Frequency;
        }
        #endregion

        #region Private - Owners
        static BenchmarkOwner[] _CreateOwners(int count) {
            var owners = new BenchmarkOwner[count];
            for (int k = 0; k < count; k++) {
                var go = new GameObject("HResourceBenchmarkOwner");
                owners[k] = go.AddComponent<BenchmarkOwner>();
            }
            return owners;
        }

        static void _DestroyRemaining(BenchmarkOwner[] owners) {
            for (int k = 0; k < owners.Length; k++) {
                if (owners[k] != null) Object.DestroyImmediate(owners[k].gameObject);
            }
        }
        #endregion

        #region Private - Sample
        static HResourceBenchmarkSample _ToSample(string scenario, int count, int payloadKB, int repeatIndex, IssueResult issue) {
            return new HResourceBenchmarkSample {
                Scenario = scenario,
                Count = count,
                PayloadKB = payloadKB,
                RepeatIndex = repeatIndex,
                IssueMs = issue.IssueMs,
                CompleteMs = issue.CompleteMs,
                Frames = issue.Frames,
                MaxFrameMs = issue.MaxFrameMs,
                IssueFrameGcKB = issue.IssueFrameGcKB,
                IssueGcBytesPerUnit = issue.IssueFrameGcKB * BYTES_PER_KB / count,
            };
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * Dev Log
 * =========================================================
 * 2026-09-23 (수정 3) :: 검수 반영 - 측정 조건 정렬
 *
 * 변경 ::
 * _IssueAsync 가 결과 배열을 NextFrame 전에 만들고, WhenAll 대신 _AreAllCompleted 로 프레임을 센다.
 * _CountFramesUntilAsync 가 상태 인자(TState)를 받는다. 기준선과 같은 대기 루프를 쓴다.
 * MultiKey 이슈 직전에 _SettleMemoryAsync 를 부른다. 반복 루프를 _RecordRepeatsAsync 로 묶고 인덱스를 k / n 으로 바꿨다.
 *
 * 이유 ::
 * 결과 배열과 WhenAll 의 할당이 GC 레코더 구간에 들어가 패키지 몫을 부풀렸다.
 * MultiKey 만 GC 정리 없이 재어 앞 시나리오의 수거가 섞일 수 있었다.
 * for 인덱스 규칙(k 시작, 중첩 k / n)을 어겼다.
 *
 * 결과 ::
 * 이슈 프레임 GC 에서 요청 수에 비례하는 할당은 요청 자체의 것만 남는다. 대기 루프의 상태 기계와 NextFrame 등록은 요청 수와 무관한 상수로 양쪽에 같이 든다. 모든 이슈 시나리오가 GC 정리 뒤에 시작한다.
 *
 * 주의 ::
 * 대기 판정 람다는 static 이라 캡처가 없다. 상태는 인자로 넘긴다.
 *
 * =========================================================
 * 2026-09-23 (수정 2) :: 프레임 대기 공통화와 Addressables 기준선
 *
 * 변경 ::
 * 요청 뒤 프레임을 세는 루프를 _CountFramesUntilAsync 로 뺐다. 기준선(HResourceBenchmarkTests.Baseline.cs)이 같은 루프를 쓴다.
 * 클래스를 partial 로 바꿨다.
 *
 * 이유 ::
 * 새 key 첫 로드 비용 중 패키지 몫과 Addressables 몫을 나누려면 같은 방법으로 잰 기준선이 필요했다.
 *
 * =========================================================
 * 2026-09-23 (최초 설계) :: HResource 한계 측정 벤치마크
 *
 * 변경 ::
 * PlayMode 테스트 두 개(BurstLifecycle, MultiKey)로 첫 요청 버스트, 반복 요청, 동시 파괴,
 * 다중 key 요청과 일괄 반납, 소유자당 메모리를 잰다.
 *
 * 이유 ::
 * "몇 개의 오브젝트가 요청하면 프레임 예산을 넘는가" 를 에디터와 개발 빌드에서 같은 코드로 보기 위해서다.
 * 첫 요청은 프로브 부착(TryGetComponent, AddComponent)과 신원 발급이 겹치는 구간이라 따로 잰다.
 *
 * 주의 ::
 * 에디터에서는 AssetOwnerIdWatchRegistry 가 신원 발급마다 표 항목을 만든다. 에디터 수치에는 그 비용이 섞인다.
 * DestroyAll 의 TeardownMs 는 GameObject 파괴 자체 비용도 포함한다.
 * GC.GetAllocatedBytesForCurrentThread 는 Unity Mono 플레이어에서 항상 0 을 돌려준다(첫 측정에서 확인). 할당은 ProfilerRecorder 로 잰다.
 * GC 레코더는 이슈 루프보다 먼저 시작한다. 루프 뒤에 시작하면 그 프레임의 할당 대부분이 빠진다(첫 측정에서 1 만 개에 78KB 로 나옴).
 * 소유자당 관리 메모리는 GC.GetTotalMemory(true) 차이다. Profiler.GetMonoUsedSizeLong 은 구획 단위라 수백 바이트 차이를 못 잡았다.
 * 나눠 만든 플레이어는 테스트가 끝나도 종료하지 않았고, forceSingleInstance 때문에 다음 실행이 곧바로 종료 코드 1 로 끝났다.
 * 그래서 -hresourceBenchmarkQuitWhenDone 인자가 있을 때만 보고서를 쓴 뒤 스스로 종료한다.
 * 에디터의 Addressables "Use Asset Database" 모드는 로드마다 0.1s 인위 지연을 넣는다. 에디터 completeMs 는 로드 비용이 아니다.
 * 2026-09-22 의 테스트 전면 삭제 방침에 대한 사용자 지시 예외다(벤치마크 도구만).
 * =========================================================
 */
#endif

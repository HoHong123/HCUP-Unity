#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * SharedAssetLoadGate 의 합류 · 실패 · 취소 · 재진입 동작을 EditMode 로 고정하는 테스트.
 *
 * 주요 기능 ::
 * factory 를 UniTaskCompletionSource 로 대신해 완료 시점을 손으로 정한다. 게이트는 합류자를
 * 동기로 재개하므로 프레임을 기다리지 않고 Status 와 결과를 바로 단정한다.
 *
 * 사용법 ::
 * Test Runner 의 EditMode 탭. 또는 batchmode -runTests -testPlatform EditMode.
 *
 * 주의 ::
 * 미관찰 예외 검사는 UniTaskScheduler 전역 설정을 잠시 바꾸고 GC 를 강제한다. 탐지 수단이
 * 살아 있는지는 대조 테스트(AbandonedFaultedSourceIsReported)가 먼저 증명한다.
 * =========================================================
 */
#endif

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using HResource.Load;

namespace HResource.Tests {
    public sealed class SharedAssetLoadGateTests {
        #region Fields
        const string KEY = "key";
        const string ASSET = "asset";

        // 보수적 GC 가 스택의 옛 포인터로 하나쯤 살려 둘 수 있어 여러 개를 버린다.
        const int ABANDONED_SOURCE_COUNT = 8;

        SharedAssetLoadGate<string, string> gate;
        UniTaskCompletionSource<string> pendingLoad;
        int factoryCalls;
        #endregion

        #region Setup
        [SetUp]
        public void SetUp() {
            gate = new SharedAssetLoadGate<string, string>();
            pendingLoad = new UniTaskCompletionSource<string>();
            factoryCalls = 0;
        }
        #endregion

        #region Tests - Join
        [Test]
        public void JoinersShareOneFactoryCallAndItsResult() {
            UniTask<string> owner = gate.RunAsync(KEY, _Factory);
            UniTask<string> joinerA = gate.RunAsync(KEY, _Factory);
            UniTask<string> joinerB = gate.RunAsync(KEY, _Factory);
            Assert.AreEqual(UniTaskStatus.Pending, owner.Status);

            pendingLoad.TrySetResult(ASSET);

            Assert.AreEqual(1, factoryCalls);
            Assert.AreEqual(ASSET, owner.GetAwaiter().GetResult());
            Assert.AreEqual(ASSET, joinerA.GetAwaiter().GetResult());
            Assert.AreEqual(ASSET, joinerB.GetAwaiter().GetResult());
        }

        [Test]
        public void KeyIsFreedBeforeJoinersResume() {
            UniTask<string> owner = gate.RunAsync(KEY, _Factory);
            UniTask joiner = _JoinThenRequestAgainAsync();

            pendingLoad.TrySetResult(ASSET);

            // 재개된 합류자의 재요청이 끝난 항목에 합류했다면 factory 는 1 회였다.
            Assert.AreEqual(2, factoryCalls);
            Assert.AreEqual(UniTaskStatus.Succeeded, joiner.Status);
            Assert.AreEqual(ASSET, owner.GetAwaiter().GetResult());
        }
        #endregion

        #region Tests - Failure
        [Test]
        public void FailurePropagatesToEveryCaller() {
            UniTask<string> owner = gate.RunAsync(KEY, _Factory);
            UniTask<string> joiner = gate.RunAsync(KEY, _Factory);

            pendingLoad.TrySetException(new InvalidOperationException("load failed"));

            Assert.Throws<InvalidOperationException>(() => owner.GetAwaiter().GetResult());
            Assert.Throws<InvalidOperationException>(() => joiner.GetAwaiter().GetResult());
        }

        [Test]
        public void CancellationReachesJoinersAsCancellation() {
            UniTask<string> owner = gate.RunAsync(KEY, _Factory);
            UniTask<string> joiner = gate.RunAsync(KEY, _Factory);

            pendingLoad.TrySetCanceled();

            Assert.AreEqual(UniTaskStatus.Canceled, owner.Status);
            Assert.AreEqual(UniTaskStatus.Canceled, joiner.Status);
        }

        [Test]
        public void AbandonedFaultedSourceIsReported() {
            int reports = _CountUnobservedReports(_AbandonFaultedSources);

            Assert.GreaterOrEqual(reports, 1, "The unobserved exception probe did not fire, so the zero-report test below proves nothing.");
        }

        [Test]
        public void FailureWithoutJoinersIsReportedOnlyToTheCaller() {
            int reports = _CountUnobservedReports(_FailWithoutJoiners);

            Assert.AreEqual(0, reports);
        }
        #endregion

        #region Tests - Contract
        [Test]
        public void SameKeyReentryInsideFactoryDeadlocks() {
            // 계약으로 금지한 호출. 등록 순서가 바뀌면 이 테스트가 깨져 교착과 이중 로드 사이의 선택을 다시 하게 된다.
            UniTask<string> owner = gate.RunAsync(KEY, () => gate.RunAsync(KEY, _Factory));

            pendingLoad.TrySetResult(ASSET);

            Assert.AreEqual(0, factoryCalls);
            Assert.AreEqual(UniTaskStatus.Pending, owner.Status);
        }
        #endregion

        #region Private - Helpers
        UniTask<string> _Factory() {
            factoryCalls++;
            return pendingLoad.Task;
        }

        async UniTask _JoinThenRequestAgainAsync() {
            await gate.RunAsync(KEY, _Factory);
            await gate.RunAsync(KEY, _Factory);
        }

        void _FailWithoutJoiners() {
            UniTask<string> owner = gate.RunAsync(KEY, _Factory);
            pendingLoad.TrySetException(new InvalidOperationException("load failed"));
            Assert.Throws<InvalidOperationException>(() => owner.GetAwaiter().GetResult());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void _AbandonFaultedSources() {
            for (int k = 0; k < ABANDONED_SOURCE_COUNT; k++) {
                var source = new UniTaskCompletionSource<string>();
                source.TrySetException(new InvalidOperationException("abandoned"));
            }
        }

        // 소멸자 스레드에서 바로 부르도록 메인 스레드 디스패치를 잠시 끈다.
        static int _CountUnobservedReports(Action action) {
            int reports = 0;
            void OnUnobserved(Exception _) => Interlocked.Increment(ref reports);

            bool dispatch = UniTaskScheduler.DispatchUnityMainThread;
            UniTaskScheduler.DispatchUnityMainThread = false;
            UniTaskScheduler.UnobservedTaskException += OnUnobserved;
            try {
                action();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            finally {
                UniTaskScheduler.UnobservedTaskException -= OnUnobserved;
                UniTaskScheduler.DispatchUnityMainThread = dispatch;
            }

            return reports;
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * Dev Log
 * =========================================================
 * 2026-09-21 (최초 설계) :: SharedAssetLoadGate EditMode 테스트
 *
 * 변경 ::
 * HResource 첫 테스트 어셈블리 HCUP.HResource.Tests 와 게이트 테스트 7건 추가.
 *
 * 이유 ::
 * 같은 날 게이트를 지연 생성 UniTaskCompletionSource 로 바꾸며 리뷰가 권고한 세 케이스(다중 합류 동일 결과,
 * 합류자 없는 실패의 단일 보고, 취소 전달)에 더해 "먼저 뺀다" 와 재진입 교착 계약을 고정했다.
 *
 * 결과 ::
 * 재생 없이 EditMode 에서 돈다. 게이트의 동기 재개 덕분에 프레임 대기가 필요 없다.
 *
 * 주의 ::
 * 미관찰 예외 검사는 GC 에 기대므로 대조 테스트가 실패하면 0 회 단정 테스트의 통과도 믿지 않는다.
 * =========================================================
 */
#endif

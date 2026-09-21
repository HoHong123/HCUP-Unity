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
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HResource.Load;

namespace HResource.Tests {
    public sealed class SharedAssetLoadGateTests {
        #region Fields
        const string KEY = "key";
        const string ASSET = "asset";
        const string OTHER_KEY = "other-key";
        const string OTHER_ASSET = "other-asset";

        // 보수적 GC 가 스택의 옛 포인터로 하나쯤 살려 둘 수 있다. 대조 테스트와 회귀 테스트 모두
        // 이 횟수만큼 반복해, 하나가 살아남아도 나머지가 결과를 결정하게 한다.
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
            int reports = _CountUnobservedReports(_FailWithoutJoinersRepeatedly);

            Assert.AreEqual(0, reports);
        }

        [Test]
        public void SynchronousFactoryThrowFreesTheKey() {
            UniTask<string> owner = gate.RunAsync(KEY, _ThrowBeforeFirstAwait);
            Assert.Throws<InvalidOperationException>(() => owner.GetAwaiter().GetResult());

            // 표에 항목이 남았다면 재요청은 끝나지 않는 항목에 합류해 factory 를 부르지 않는다.
            UniTask<string> retry = gate.RunAsync(KEY, _Factory);
            pendingLoad.TrySetResult(ASSET);

            Assert.AreEqual(1, factoryCalls);
            Assert.AreEqual(ASSET, retry.GetAwaiter().GetResult());
        }

        [Test]
        public void NullFactoryThrowsArgumentNullException() {
            // HLogger.Throw 는 던지기 전에 에러 로그를 남긴다. 예상해 두지 않으면 Test Runner 가 실패로 본다.
            LogAssert.Expect(LogType.Error, new Regex("factory is null"));

            UniTask<string> call = gate.RunAsync(KEY, null);

            Assert.Throws<ArgumentNullException>(() => call.GetAwaiter().GetResult());
        }
        #endregion

        #region Tests - Keys
        [Test]
        public void DifferentKeysDoNotJoin() {
            var otherLoad = new UniTaskCompletionSource<string>();
            UniTask<string> first = gate.RunAsync(KEY, _Factory);
            UniTask<string> second = gate.RunAsync(OTHER_KEY, () => {
                factoryCalls++;
                return otherLoad.Task;
            });

            Assert.AreEqual(2, factoryCalls);

            otherLoad.TrySetResult(OTHER_ASSET);

            Assert.AreEqual(UniTaskStatus.Pending, first.Status);
            Assert.AreEqual(OTHER_ASSET, second.GetAwaiter().GetResult());

            pendingLoad.TrySetResult(ASSET);

            Assert.AreEqual(ASSET, first.GetAwaiter().GetResult());
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

        static UniTask<string> _ThrowBeforeFirstAwait() {
            throw new InvalidOperationException("thrown before the first await");
        }

        // 반복마다 게이트와 로드를 새로 만든다. 앞 회차의 객체가 스택에 남아도 다음 회차와 섞이지 않는다.
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void _FailWithoutJoinersRepeatedly() {
            for (int k = 0; k < ABANDONED_SOURCE_COUNT; k++) {
                var isolatedGate = new SharedAssetLoadGate<string, string>();
                var isolatedLoad = new UniTaskCompletionSource<string>();

                UniTask<string> owner = isolatedGate.RunAsync(KEY, () => isolatedLoad.Task);
                isolatedLoad.TrySetException(new InvalidOperationException("load failed"));

                Assert.Throws<InvalidOperationException>(() => owner.GetAwaiter().GetResult());
            }
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
 * 2026-09-21 (수정) :: 회귀 테스트 반복 구조와 누락 케이스 3건
 *
 * 변경 ::
 * FailureWithoutJoinersIsReportedOnlyToTheCaller 가 _FailWithoutJoinersRepeatedly(NoInlining) 로
 * ABANDONED_SOURCE_COUNT 회 실패시킨다. 회차마다 게이트와 로드를 새로 만든다.
 * SynchronousFactoryThrowFreesTheKey / NullFactoryThrowsArgumentNullException / DifferentKeysDoNotJoin 추가.
 *
 * 이유 ::
 * 리뷰 지적. 대조 테스트는 8 개를 버리는데 회귀 테스트는 1 회라, 옛 결함으로 되돌아가도 그 하나가 보수적 GC 로
 * 살아남으면 0 건이 되어 통과할 수 있었다. 동기 예외 · factory null · key 독립성은 검증이 없었다.
 *
 * 결과 ::
 * 11/11 통과. 돌연변이(항상 완료 소스 생성)에서 "Expected: 0 But was: 8". 기록은 SharedAssetLoadGate.cs
 * Dev Log 의 (검증) 항목.
 *
 * 주의 ::
 * factory null 은 HLogger.Throw 가 던지기 전에 Debug.LogError 를 남긴다. LogAssert.Expect 를 지우면
 * 올바른 동작인데도 Test Runner 가 실패로 본다.
 *
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

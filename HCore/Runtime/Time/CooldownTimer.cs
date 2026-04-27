#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 특정 시간 동안 동작하는 쿨타임 타이머 클래스입니다.
 *
 * 기능 ::
 * - UTC 기반 쿨타임 계산
 * - 일정 간격 Tick 이벤트 제공
 * - 완료 / 취소 이벤트 제공
 *
 * 특징 ::
 * Coroutine 기반으로 동작하며 남은 시간을 TimeSpan 형태로 제공합니다.
 * =========================================================
 */
#endif

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;

namespace HCore.HTime {
    public enum CancelBehavior {
        SkipAllEvents = 0,
        InvokeCanceled = 1,
        InvokeCompleted = 2,
    }

    public sealed class CooldownTimer : IDisposable {
        #region Fields
        readonly MonoBehaviour runner;
        readonly float tickIntervalSeconds;

        Coroutine coroutine;
        bool isDisposed;
        bool isRunning;
        bool isCanceled;

        long endUtcTicks; // 끝나는 시각 ticks(UTC)
        CancelBehavior cancelBehavior;
        #endregion

        #region Events
        public event Action OnCompleted;
        public event Action OnCanceled;
        public event Action<TimeSpan /* remaining */> OnTick;
        #endregion

        #region Properties
        public bool IsRunning => isRunning;
        public bool IsCanceled => isCanceled;
        public long EndUtcTicks => endUtcTicks;
        public TimeSpan Remaining => TimeUtil.GetRemaining(DateTime.UtcNow, endUtcTicks);
        #endregion

        #region Public - Constructors 
        public CooldownTimer(MonoBehaviour runner, float tickIntervalSeconds = 0.1f) {
            Assert.IsNotNull(runner);

            this.runner = runner;
            this.tickIntervalSeconds = Mathf.Max(0f, tickIntervalSeconds);
        }
        #endregion

        #region Public - Start Countdown
        public void Start(TimeSpan duration, CancelBehavior cancelBehavior = CancelBehavior.InvokeCanceled) {
            Assert.IsTrue(!isDisposed);

            if (duration < TimeSpan.Zero) duration = TimeSpan.Zero;

            endUtcTicks = TimeUtil.StartCooldownTicks(DateTime.UtcNow, duration);
            StartWithEndTicks(endUtcTicks, cancelBehavior);
        }

        public void StartWithEndTicks(long endUtcTicks, CancelBehavior cancelBehavior = CancelBehavior.InvokeCanceled) {
            Assert.IsTrue(!isDisposed);

            StopSilently();

            this.endUtcTicks = endUtcTicks;
            this.cancelBehavior = cancelBehavior;
            isRunning = true;
            isCanceled = false;

            coroutine = runner.StartCoroutine(_Run());
        }
        #endregion

        #region Public - Cancellation
        public void Cancel(CancelBehavior behavior) {
            Assert.IsTrue(!isDisposed);

            if (!isRunning) return;

            isCanceled = true;
            _StopInternal(behavior);
        }

        public void StopSilently() {
            if (!isRunning && coroutine == null) return;
            _StopInternal(CancelBehavior.SkipAllEvents);
        }
        #endregion

        #region Public - Dispose
        public void Dispose() {
            if (isDisposed) return;

            StopSilently();

            OnCompleted = null;
            OnCanceled = null;
            OnTick = null;

            isDisposed = true;
        }
        #endregion

        #region Public - Get Data
        public string GetRemainingStringAuto() {
            return TimeUtil.FormatRemainingAuto(Remaining);
        }
        #endregion

        #region Private - Coroutine
        private IEnumerator _Run() {
            float lastTickAt = 0f;

            while (isRunning) {
                TimeSpan remaining = Remaining;
                if (remaining <= TimeSpan.Zero) {
                    _StopInternal(CancelBehavior.SkipAllEvents);
                    OnCompleted?.Invoke();
                    yield break;
                }

                if (tickIntervalSeconds <= 0f) {
                    OnTick?.Invoke(remaining);
                }
                else {
                    float now = Time.unscaledTime;
                    if (now - lastTickAt >= tickIntervalSeconds) {
                        lastTickAt = now;
                        OnTick?.Invoke(remaining);
                    }
                }

                yield return null;
            }
        }
        #endregion

        #region Private - Internals
        private void _StopInternal(CancelBehavior behavior) {
            isRunning = false;

            if (coroutine != null) {
                runner.StopCoroutine(coroutine);
                coroutine = null;
            }

            if (behavior == CancelBehavior.InvokeCompleted) {
                OnCompleted?.Invoke();
                return;
            }

            if (behavior == CancelBehavior.InvokeCanceled) {
                OnCanceled?.Invoke();
                return;
            }
        }
        #endregion
    }
}
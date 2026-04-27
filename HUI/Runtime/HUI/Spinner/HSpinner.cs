#if UNITY_EDITOR
/* =========================================================
 * SpinnerManager를 보다 간단하게 사용하기 위한 Extension Utility 클래스입니다.
 * SpinnerManager.Instance 호출을 직접 사용하지 않고 Extension 메서드 형태로 Spinner를 제어하기 위함입니다.
 *
 * 지원 기능 ::
 * 1. Spinner 즉시 표시
 * 2. 일정 시간 Spinner 표시
 * 3. 비동기 작업 동안 Spinner 표시
 * 4. Spinner 수동 숨김
 *
 * 주의사항 ::
 * Spinner 호출자는 반드시 동일 객체로 Hide를 호출해야 합니다.
 * =========================================================
 */
#endif

using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace HUI.Spinner {
    public static class HSpinner {
        public static void ShowSpinner(this IDisposable caller) => SpinnerManager.Instance.Show(caller);
        public static UniTask ShowSpinner(this IDisposable owner, Func<UniTask> task) 
            => SpinnerManager.Instance.Show(owner, task);
        public static UniTask ShowSpinner(this IDisposable owner, int tick, bool ignorTimeScale = true, CancellationTokenSource cts = null)
            => SpinnerManager.Instance.Show(owner, tick, ignorTimeScale, cts);
        public static UniTask ShowSpinner(this IDisposable owner, float second, bool ignorTimeScale = true, CancellationTokenSource cts = null)
            => SpinnerManager.Instance.Show(owner, second, ignorTimeScale, cts);
        public static void HideSpinner(this IDisposable caller) => SpinnerManager.Instance.Hide(caller);
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH 2026.03.10
 *
 * 주요 기능 ::
 * ShowSpinner()
 *  + Spinner 표시
 * ShowSpinner(Func<UniTask>)
 *  + 비동기 작업 동안 Spinner 표시
 * ShowSpinner(int tick)
 *  + 지정 시간 동안 Spinner 표시
 * ShowSpinner(float second)
 *  + 초 단위 Spinner 표시
 * HideSpinner()
 *  + Spinner 숨김
 *
 * 사용법 ::
 * this.ShowSpinner();
 * await this.ShowSpinner(async () => { await LoadData(); });
 * await this.ShowSpinner(1.5f);
 *
 * 기타 ::
 * SpinnerManager.Instance를 직접 호출하지 않도록 작성된 Extension Wrapper 클래스입니다.
 * =========================================================
 */
#endif
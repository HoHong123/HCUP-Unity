#if UNITY_EDITOR
/* =========================================================
 * 프로젝트 전역에서 사용하는 Spinner UI 관리자입니다.
 *
 * 목적 ::
 * 비동기 작업이나 로딩 작업 동안 사용자에게 진행 상태를 표시하기 위한 Spinner UI를 관리합니다.
 *
 * 특징 ::
 * 1. Singleton 기반 전역 Spinner 관리
 * 2. 호출자 기반 Spinner 참조 카운팅
 * 3. 비동기 작업 자동 Spinner 처리
 * 4. Scene 전환 시 Caller 정리
 *
 * 동작 방식 ::
 * Spinner를 호출한 객체를 Dictionary로 관리하며 모든 호출자가 해제될 때 Spinner가 숨겨집니다.
 *
 * 주의사항 ::
 * Spinner를 호출한 객체는 반드시 Hide를 호출해야 합니다.
 * =========================================================
 */
#endif

using System;
using System.Threading;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TMPro;
using HCore;
using HCore.Scene;
using System.Text;
using HDiagnosis.Logger;
using HInspector;

namespace HUI.Spinner {
    public class SpinnerManager : SingletonBehaviour<SpinnerManager> {
        [HTitle("Spinner Object")]
        [SerializeField]
        GameObject spinner;

        [HTitle("UI")]
        [SerializeField]
        TMP_Text toolTipTxt;

        readonly Dictionary<object, int> callers = new();

        public bool IsVisible { get; private set; } = false;

        // 고착(Hide 누락)은 실제로 빌드에서 발생한다 — 진단 경로를 에디터 전용으로 두면 조사할 수 없다.
        public IReadOnlyDictionary<object, int> ActiveCallers => callers;
        public string GetCallerData() {
            if (callers.Count == 0) {
                return "[Spinner] No active callers.";
            }
            StringBuilder sb = new StringBuilder("[Spinner] Active Callers :: \n");
            foreach (var kvp in callers) {
                sb.AppendLine($"- Caller: {kvp.Key}, Count: {kvp.Value}");
            }
            return sb.ToString();
        }

        private void _ShowSpinner() {
            if (spinner == null) {
                HLogger.Error("[Spinner] 'spinner' GameObject is not assigned.");
                return;
            }
            spinner.SetActive(true);
        }

        private void _HideSpinner() {
            if (spinner == null) return;
            spinner.SetActive(false);
        }


        protected override void Awake() {
            base.Awake();
            // base 가 중복 인스턴스를 Destroy 한 경우 정적 이벤트를 구독하면 유령 구독자가 남는다.
            if (instance != this) return;
            SceneLoader.OnSceneLoaded += CleanUp;
            SceneLoader.OnSceneUnloaded += CleanUp;
        }

        // SceneLoader 의 이벤트는 정적이라 구독자를 강참조로 붙잡는다. 해제하지 않으면
        // 파괴된 인스턴스가 씬 전환마다 CleanUp 을 받아 spinner.SetActive 에서 MissingReferenceException 을 던진다.
        protected override void OnDestroy() {
            SceneLoader.OnSceneLoaded -= CleanUp;
            SceneLoader.OnSceneUnloaded -= CleanUp;
            base.OnDestroy();
        }

        #region Public - Show
        public void Show(object caller, string toolTip = null) {
            if (caller == null) {
                HLogger.Error("[Spinner] Show called with a null caller. Ignored.");
                return;
            }

            // 종전에는 후행 호출이 toolTip 을 지정하지 않으면 선행 호출자의 안내 문구를 지웠다.
            // 참조 카운팅되는 다른 상태와 규약을 맞춰, 명시된 경우에만 갱신한다.
            if (toolTip != null && toolTipTxt != null) toolTipTxt.text = toolTip;

            if (callers.ContainsKey(caller)) {
                callers[caller]++;
            }
            else {
                callers[caller] = 1;
            }

            if (!IsVisible) {
                IsVisible = true;
                _ShowSpinner();
            }
        }

        public async UniTask Show(
            object caller,
            int tick, bool ignorTimeScale = true,
            CancellationTokenSource cts = null,
            string toolTip = null) {
            Show(caller, toolTip);
            var ct = cts?.Token ?? default;
            try {
                await UniTask.Delay(
                    millisecondsDelay: tick,
                    ignoreTimeScale: ignorTimeScale,
                    cancellationToken: ct);
            }
            finally {
                _HideSafely(caller);   // 취소 시에도 스피너 고착 방지 — 다른 오버로드와 동일 규약
            }
        }

        public async UniTask Show(
            object caller,
            float second, bool ignorTimeScale = true,
            CancellationTokenSource cts = null,
            string toolTip = null) {
            Show(caller, toolTip);
            var ct = cts?.Token ?? default;
            try {
                await UniTask.WaitForSeconds(
                    duration: second,
                    ignoreTimeScale: ignorTimeScale,
                    cancellationToken: ct);
            }
            finally {
                _HideSafely(caller);   // 취소 시에도 스피너 고착 방지
            }
        }

        public async UniTask Show(object caller, Func<UniTask> taskFunc, string toolTip = null) {
            Show(caller, toolTip);
            try {
                await taskFunc();
            }
            finally {
                _HideSafely(caller);
            }
        }

        public async UniTask Show(object caller, UniTask task, string toolTip = null) {
            Show(caller, toolTip);
            try {
                await task;
            }
            finally {
                _HideSafely(caller);
            }
        }

        public async UniTask<T> Show<T>(object caller, UniTask<T> task, string toolTip = null) {
            Show(caller, toolTip);
            try {
                return await task;
            }
            finally {
                _HideSafely(caller);
            }
        }
        #endregion

        #region Public - Hide
        public void Hide(object caller) {
            if (!callers.ContainsKey(caller)) return;

            callers[caller]--;
            if (callers[caller] < 1) {
                callers.Remove(caller);
            }

            if (callers.Count == 0 && IsVisible) {
                IsVisible = false;
                _HideSpinner();
            }
        }
        #endregion

        #region Private - Safe Hide
        // finally 안에서 던져진 예외는 원본 예외를 대체한다 — 작업 실패 원인이 사라지는 것을 막는다.
        private void _HideSafely(object caller) {
            try {
                Hide(caller);
            }
            catch (System.Exception e) {
                HLogger.Error($"[Spinner] Hide failed while unwinding: {e}");
            }
        }
        #endregion

        #region Public - Clean
        public void CleanUp() {
            var keysToRemove = new List<object>();

            // Dictionary 는 null 키를 담을 수 없으므로 `key != null` 검사는 항상 no-op 이었다.
            // 수거 대상은 "파괴된 UnityEngine.Object 호출자" (fake-null) — 정적 타입이 object 라
            // Unity 의 == 오버로드가 걸리지 않아 명시 캐스트로 판정한다.
            foreach (var key in callers.Keys) {
                if (key is UnityEngine.Object unityCaller && unityCaller == null) {
                    keysToRemove.Add(key);
                }
            }

            foreach (var key in keysToRemove) {
                callers.Remove(key);
            }

            if (callers.Count == 0 && IsVisible) {
                IsVisible = false;
                _HideSpinner();
            }
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* Dev Log
 * =========================================================
 * @Jason - PKH 2026.03.10 [LOG-20260310-1]
 * - 설명 주석 추가 + 주요 기능/구조/사용법 정리.
 * ==================================
 * @Jason - PKH 09. 02. 26 [LOG-20260209-2]
 * - IDisposable 파기.
 * ==================================
 * @Jason - PKH 09. 02. 26 [LOG-20260209-1]
 * - 스피너 호출 오브젝트는 IDisposable 필수 + 호출자 파괴 안전장치.
 * ==================================
 * > 이전 엔트리는 docs/history/HUI/Runtime/HUI/Spinner/SpinnerManager.md 참조 (총 5 엔트리)
 * =========================================================
 */
#endif
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
using HInspector;
#if UNITY_EDITOR
using System.Text;
#endif

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

#if UNITY_EDITOR
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
#endif

        private void _ShowSpinner() => spinner.SetActive(true);
        private void _HideSpinner() => spinner.SetActive(false);


        protected override void Awake() {
            base.Awake();
            SceneLoader.OnSceneLoaded += CleanUp;
            SceneLoader.OnSceneUnloaded += CleanUp;
        }

        #region Public - Show
        public void Show(object caller, string toolTip = null) {
            toolTipTxt.text = toolTip ?? string.Empty;

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
            await UniTask.Delay(
                millisecondsDelay: tick,
                ignoreTimeScale: ignorTimeScale,
                cancellationToken: ct);
            Hide(caller);
        }

        public async UniTask Show(
            object caller,
            float second, bool ignorTimeScale = true,
            CancellationTokenSource cts = null,
            string toolTip = null) {
            Show(caller, toolTip);
            var ct = cts?.Token ?? default;
            await UniTask.WaitForSeconds(
                duration: second,
                ignoreTimeScale: ignorTimeScale,
                cancellationToken: ct);
            Hide(caller);
        }

        public async UniTask Show(object caller, Func<UniTask> taskFunc, string toolTip = null) {
            Show(caller, toolTip);
            try {
                await taskFunc();
            }
            finally {
                Hide(caller);
            }
        }

        public async UniTask Show(object caller, UniTask task, string toolTip = null) {
            Show(caller, toolTip);
            try {
                await task;
            }
            finally {
                Hide(caller);
            }
        }

        public async UniTask<T> Show<T>(object caller, UniTask<T> task, string toolTip = null) {
            Show(caller, toolTip);
            try {
                return await task;
            }
            finally {
                Hide(caller);
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

        #region Public - Clean
        public void CleanUp() {
            var keysToRemove = new List<object>();

            foreach (var key in callers.Keys) {
                if (key != null) continue;
                keysToRemove.Add(key);
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
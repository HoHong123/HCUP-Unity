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
 * @Jason - PKH 21. 07. 25
 * 1. 스피너를 전역으로 사용하기 위해 작성한 스크립트 입니다.
 * 1-1. 불필요한 싱글톤 접근을 제외하기 위해 작성했습니다.
 * 2. 스피너는 자신을 호출한 모든 오브젝트를 추적합니다.
 * 2-1. 스피너를 호출한 오브젝트가 비활성화(Hide)를 반드시 시켜주어야 합니다.
 * 3. 비동기 처리도 진행합니다.
 * 4. **팝업 매니저**가 반드시 필요합니다.
 * Ps. 사용법은 'SpinnerTester.cs'를 확인해주세요.
 * ===============================================
 * 1. This is a script written to use the spinner globally.
 * 1-1. It was written to exclude unnecessary singleton access.
 * 2. The spinner tracks all objects that called it.
 * 2-1. The object that called the spinner must deactivate(Hide) it.
 * 3. It also performs asynchronous processing.
 * 4. A popup manager is absolutely necessary.
 * Ps. Check 'SpinnerTester.cs' for tutorial.
 * 
 * @Jason - PKH 22. 07. 25
 * 1. 씬전환 및 콜러의 값이 의도치않게 제거되었을 경우, 스피너에서 이를 확인하여 해당 호출자 정보를 관리하는 기능 추가
 * 1-1. CleanUp함수
 * 2. CleanUp이 씬로드/씬언로드 프로세스가 진행되면 자동으로 활성화되도록 설정
 * 
 * ===============================================
 * @Jason - PKH 09. 02. 26
 * 1. 스피너 호출 오브젝트들은 반드시 IDisposable이 가능한 오브젝트로 선언.
 * + 예기치 못한 호출자 파괴와 같은 이벤트 대비 안전장치 추가.
 * ===============================================
 * @Jason - PKH 09. 02. 26
 * 1. IDisposable 파기
 * =========================================================
 * @Jason - PKH 2026.03.10
 * 설명 주석 추가
 * 주요 기능 ::
 *
 * 1. Spinner 표시
 * 2. 일정 시간 Spinner 표시
 * 3. 비동기 작업 Spinner 처리
 * 4. Spinner 숨김
 * 5. Scene 변경 정리
 *
 * 구조 ::
 * callers
 *  + Spinner 호출 객체 추적 Dictionary
 * IsVisible
 *  + 현재 Spinner 표시 상태
 *
 * 사용법 ::
 * SpinnerManager.Instance.Show(this);
 * await SpinnerManager.Instance.Show(
 *     this,
 *     async () => await LoadData()
 * );
 * =========================================================
 */
#endif
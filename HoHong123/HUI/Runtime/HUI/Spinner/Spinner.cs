using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using HUtil.Core;
using HUtil.Scene;
#if UNITY_EDITOR
using System.Text;
#endif

namespace HUI.Spinner {
    public class SpinnerManager : SingletonBehaviour<SpinnerManager> {
        [SerializeField]
        GameObject spinner;

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


        public void Show(object caller) {
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

        public void Hide(object caller) {
            if (!callers.ContainsKey(caller))
                return;

            callers[caller]--;
            if (callers[caller] < 1) {
                callers.Remove(caller);
            }

            if (callers.Count == 0 && IsVisible) {
                IsVisible = false;
                _HideSpinner();
            }
        }

        public async UniTask Show(int tick, object caller) {
            Show(caller);
            await UniTask.Delay(tick);
            Hide(caller);
        }

        public async UniTask Show(Func<UniTask> taskFunc, object caller) {
            Show(caller);
            await taskFunc();
            Hide(caller);
        }

        public void CleanUp() {
            var keysToRemove = new List<object>();

            foreach (var key in callers.Keys) {
                if (key == null) {
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
 */
#endif
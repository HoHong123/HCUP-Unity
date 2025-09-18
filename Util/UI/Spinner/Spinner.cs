#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 스피너(인디케이터) UI 컨트롤 스크립트 입니다.
 * 스피너는 현재 스피너를 호출한 모든 객체의 테스크가 끝나면 비활성화됩니다.
 * 
 * ** 사용법 **
 * 1. Spinner UI 프리팹을 생성합니다.
 * 2. 프리팹을 'Resources'폴더에 저장합니다.
 * 3. 해당 프리팹 리소스 경로를 'spinnerAddress' 값에 등록합니다.
 * 4. Show, Hide 함수를 호출하여 스피너를 사용합니다.
 * =========================================================
 */
#endif

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Util.Scene;
using UnityEngine;
using Util.Logger;
#if UNITY_EDITOR
using System.Text;
#endif

/// <summary>
/// Global spinner UI script.
/// ** MUST DO **
/// 1. Create Spinner UI.
/// 2. Save Spinner UI prefab in 'Resources' file.
/// 3. Input Spinner prefab address into 'spinnerAddress' string variable.
/// </summary>
public static class Spinner {
    static string spinnerAddress = "UI/Spinner";
    static GameObject spinner;
    static readonly Dictionary<object, int> callers = new();

    public static bool IsVisible { get; private set; } = false;

    public static IReadOnlyDictionary<object, int> ActiveCallers => callers;
    public static string GetCallerData() {
#if UNITY_EDITOR
        if (callers.Count == 0) {
            return "[Spinner] No active callers.";
        }
        StringBuilder sb = new StringBuilder("[Spinner] Active Callers :: \n");
        foreach (var kvp in callers) {
            sb.AppendLine($"- Caller: {kvp.Key}, Count: {kvp.Value}");
        }
        return sb.ToString();
#else
        return $"[Spinner] Active Caller Count :: {callers.Count}";
#endif
    }


    static Spinner() {
        spinner = Resources.Load<GameObject>(spinnerAddress);
        if (!spinner) HLogger.Error($"Cannot find 'Spinner' prefab in '{spinnerAddress}'");
        SceneLoader.OnSceneLoaded += CleanUp;
        SceneLoader.OnSceneUnloaded += CleanUp;
    }


    public static void Show(object caller) {
        if (callers.ContainsKey(caller)) {
            callers[caller]++;
        }
        else {
            callers[caller] = 1;
        }

        if (!IsVisible) {
            IsVisible = true;
            spinner.SetActive(true);
        }
    }

    public static async UniTask Show(float second, object caller) {
        Show(caller);
        await UniTask.Delay(TimeSpan.FromSeconds(second));
        Hide(caller);
    }

    public static async UniTask Show(int tick, object caller) {
        Show(caller);
        await UniTask.Delay(tick);
        Hide(caller);
    }

    public static async UniTask Show(Func<UniTask> taskFunc, object caller) {
        Show(caller);
        await taskFunc();
        Hide(caller);
    }

    public static void Hide(object caller) {
        if (!callers.ContainsKey(caller))
            return;

        callers[caller]--;
        if (callers[caller] < 1) {
            callers.Remove(caller);
        }

        if (callers.Count == 0 && IsVisible) {
            IsVisible = false;
            spinner?.SetActive(false);
        }
    }


    public static void CleanUp() {
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
            spinner?.SetActive(false);
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
 * ===============================================
 * @Jason - PKH 22. 07. 25
 * 1. 씬전환 및 콜러의 값이 의도치않게 제거되었을 경우, 스피너에서 이를 확인하여 해당 호출자 정보를 관리하는 기능 추가
 * 1-1. CleanUp함수
 * 2. CleanUp이 씬로드/씬언로드 프로세스가 진행되면 자동으로 활성화되도록 설정
 * ===============================================
 * @Jason - PKH 18. 09. 25
 * 1. 스피너 전체 수정했습니다.
 * 2. 스피너 프리팹을 생성자에서 호출하여 PopupManager와 종속관계를 제거했습니다.
 * + 패키지 사용시 종속관계로 인한 오류를 최소화 하기 위해 조치했습니다.
 */
#endif
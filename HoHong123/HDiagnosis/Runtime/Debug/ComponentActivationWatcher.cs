#if UNITY_EDITOR
/* =========================================================
 * 컴포넌트의 활성화 상태(OnEnable / OnDisable)를 추적하기 위한 디버깅용 유틸리티 컴포넌트입니다.
 *
 * 목적 ::
 * 특정 GameObject가 언제 활성화/비활성화 되는지 호출 스택과 함께 추적하기 위함입니다.
 *
 * 주의사항 ::
 * 디버깅 목적의 컴포넌트이므로 Production 환경에서는 사용하지 않는 것을 권장합니다.
 * =========================================================
 */
#endif

using UnityEngine;

namespace HUtil.Diagnosis {
    /// <summary>
    /// 컴포넌트의 활성화 상태 변화를 감지하여 로그를 출력하는 유틸리티 클래스입니다.
    /// </summary>
    public class ComponentActivationWatcher : MonoBehaviour {
#if UNITY_EDITOR
        #region Fields
        [SerializeField]
        int stackTraceDepth = 10;
        [SerializeField]
        bool trackEnable = true;
        [SerializeField]
        bool trackDisable = true;
        #endregion

        #region Private - Unity Life Cycle
        private void OnEnable() {
            if (!trackEnable) return;
            HDebug.StackTraceLog($"[ComponentActivationWatcher] {gameObject.name} enabled", stackTraceDepth);
        }

        private void OnDisable() {
            if (!trackDisable) return;
            HDebug.StackTraceLog($"[ComponentActivationWatcher] {gameObject.name} disabled", stackTraceDepth);
        }
        #endregion
#endif
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 *
 * 주요 기능 ::
 * OnEnable
 *  + Enable 호출 시 스택 로그 출력
 * OnDisable
 *  + Disable 호출 시 스택 로그 출력
 *
 * 옵션 ::
 * stackTraceDepth
 *  + 출력할 호출 스택 깊이
 * trackEnable / trackDisable
 *  + 각 이벤트 추적 여부
 *
 * 사용법 ::
 * 1. 디버깅할 GameObject에 컴포넌트 추가
 * 2. 활성화 이벤트 추적
 *
 * 기타 ::
 * 1. HDebug.StackTraceLog 기반 로그 출력
 * =========================================================
 */
#endif
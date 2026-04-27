#if UNITY_EDITOR
/* =========================================================
 * Animator StateMachineBehaviour 이벤트를 특정 컴포넌트로 전달하기 위한
 * Router 베이스 클래스입니다.
 *
 * Animator State 이벤트를 받아 동일 GameObject에 존재하는
 * Handler 컴포넌트로 전달하는 구조를 제공합니다.
 *
 * 주의사항 ::
 * 1. Handler 컴포넌트는 Animator가 붙어있는 동일 GameObject에 존재해야 합니다.
 * 2. useStateNameFilter가 활성화된 경우 targetStateName과 일치하는
 *    State에서만 이벤트가 전달됩니다.
 * =========================================================
 */
#endif

using UnityEngine;

namespace HUtil.Animation {
    public class BaseAnimatorStateRouter<AniHandler> : StateMachineBehaviour {
        [SerializeField]
        protected bool useStateNameFilter;
        [SerializeField]
        protected string targetStateName;

        protected AniHandler handler;

        protected void InitHandler(Animator animator) {
            if (handler != null) return;
            handler = animator.GetComponent<AniHandler>();
        }

        protected bool IsTargetState(AnimatorStateInfo stateInfo) {
            if (!useStateNameFilter || string.IsNullOrEmpty(targetStateName)) return true;
            return stateInfo.IsName(targetStateName);
        }
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH 2026.03.10
 *
 * 주요 기능 ::
 * 1. Animator StateMachineBehaviour 이벤트 라우팅
 *    + StateMachineBehaviour 이벤트를 Handler 컴포넌트로 전달
 * 2. Handler 자동 검색
 *    + InitHandler() 호출 시 Animator GameObject에서 Handler 탐색
 * 3. Animator State 필터링
 *    + 특정 State 이름에서만 이벤트를 실행하도록 제한
 *
 * 사용법 ::
 * 1. BaseAnimatorStateRouter<THandler>를 상속한 Router를 생성합니다.
 * 2. Animator State에 해당 Router Behaviour를 등록합니다.
 * 3. 동일 GameObject에 Handler 컴포넌트를 추가합니다.
 *
 * 기타 ::
 * 1. Router는 Animator StateMachineBehaviour 이벤트를
 *    게임 로직 컴포넌트로 전달하기 위한 Bridge 역할을 수행합니다.
 * =========================================================
 */
#endif
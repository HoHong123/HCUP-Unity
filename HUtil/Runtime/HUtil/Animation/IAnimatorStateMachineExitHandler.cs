#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Animator StateMachine Exit 이벤트를 처리하기 위한 인터페이스입니다.
 * AnimatorStateMachineExitRouter에서 전달되는 StateMachine Exit 이벤트를 처리하는
 * 컴포넌트가 구현해야 합니다.
 *
 * 이벤트 ::
 * OnAnimatorStateMachineExit
 *
 * 사용처 ::
 * Animator StateMachine 종료 시 로직 처리
 * =========================================================
 */
#endif

using UnityEngine;

namespace HUtil.Animation {
    /// <summary> Require 'AnimatorStateMachineExitRouter.cs' </summary>
    public interface IAnimatorStateMachineExitHandler {
        void OnAnimatorStateMachineExit(Animator animator, int stateMachinePathHash);
    }
}
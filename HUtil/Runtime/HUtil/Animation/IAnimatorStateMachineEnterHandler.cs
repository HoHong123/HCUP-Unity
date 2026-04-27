#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Animator StateMachine Enter 이벤트를 처리하기 위한 인터페이스입니다.
 * AnimatorStateMachineEnterRouter에서 전달되는 StateMachine Enter 이벤트를
 * 처리하는 컴포넌트가 구현해야 합니다.
 *
 * 사용처 ::
 * Animator StateMachine 진입 시 로직 처리
 * =========================================================
 */
#endif

using UnityEngine;

namespace HUtil.Animation {
    /// <summary> Require 'AnimatorStateMachineEnterRouter.cs' </summary>
    public interface IAnimatorStateMachineEnterHandler {
        void OnAnimatorStateMachineEnter(Animator animator, int stateMachinePathHash);
    }
}
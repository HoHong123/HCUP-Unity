#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Animator Root Motion 이벤트를 처리하기 위한 인터페이스입니다.
 * AnimatorStateMoveRouter에서 전달되는 OnStateMove 이벤트를 처리하는
 * 컴포넌트가 구현해야 합니다.
 *
 * 사용처 ::
 * Animator Root Motion 처리
 * =========================================================
 */
#endif

using UnityEngine;

namespace HUtil.Animation {
    /// <summary> Require 'AnimatorStateMoveRouter.cs' </summary>
    public interface IAnimatorStateMoveHandler {
        void OnAnimatorStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex);
    }
}
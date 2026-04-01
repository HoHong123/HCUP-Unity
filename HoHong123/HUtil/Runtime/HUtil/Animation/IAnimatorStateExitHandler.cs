#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Animator State Exit 이벤트를 처리하기 위한 인터페이스입니다.
 * AnimatorStateExitRouter에서 전달되는 State Exit 이벤트를 처리하는
 * 컴포넌트가 구현해야 합니다.
 *
 * 사용처 ::
 * Animator State 종료 시 게임 로직 처리
 * =========================================================
 */
#endif

using UnityEngine;

namespace HUtil.Animation {
    /// <summary> Require 'AnimatorStateExitRouter.cs' </summary>
    public interface IAnimatorStateExitHandler {
        void OnAnimatorStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex);
    }
}
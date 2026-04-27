#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Animator State Enter 이벤트를 처리하기 위한 인터페이스입니다.
 * AnimatorStateEnterRouter에서 전달되는 State Enter 이벤트를 처리하는
 * 컴포넌트가 구현해야 합니다.
 *
 * 사용처 ::
 * Animator State 진입 시 게임 로직 처리
 * =========================================================
 */
#endif

using UnityEngine;

namespace HUtil.Animation {
    /// <summary> Require 'AnimatorStateEnterRouter.cs' </summary>
    public interface IAnimatorStateEnterHandler {
        void OnAnimatorStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex);
    }
}
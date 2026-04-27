#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Animator IK 이벤트를 처리하기 위한 인터페이스입니다.
 * AnimatorStateIkRouter에서 전달되는 OnStateIK 이벤트를 처리하는
 * 컴포넌트가 구현해야 합니다.
 *
 * 사용처 ::
 * Animator IK 처리 로직
 * =========================================================
 */
#endif

using UnityEngine;

namespace HUtil.Animation {
    /// <summary> Require 'AnimatorStateIkRouter.cs' </summary>
    public interface IAnimatorStateIkHandler {
        void OnAnimatorStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex);
    }
}
#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Animator State Update 이벤트를 처리하기 위한 인터페이스입니다.
 * AnimatorStateUpdateRouter에서 전달되는 State Update 이벤트를 처리하는
 * 컴포넌트가 구현해야 합니다.
 *
 * 사용처 ::
 * Animator State 업데이트 로직 처리
 * =========================================================
 */
#endif

using UnityEngine;

namespace HUtil.Animation {
    /// <summary> Require 'AnimatorStateUpdateRouter.cs' </summary>
    public interface IAnimatorStateUpdateHandler {
        void OnAnimatorStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex);
    }
}
#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Animator Root Motion 처리 이벤트를 Handler로 전달하는 Router입니다.
 * Animator의 OnStateMove 이벤트가 발생하면 IAnimatorStateMoveHandler 인터페이스를
 * 구현한 컴포넌트로 전달합니다.
 *
 * 사용처 ::
 * Animator Root Motion 처리
 * =========================================================
 */
#endif

using UnityEngine;

namespace HUtil.Animation {
    public class AnimatorStateMoveRouter : BaseAnimatorStateRouter<IAnimatorStateMoveHandler> {
        #region State Handler
        public override void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            InitHandler(animator);
            if (!IsTargetState(stateInfo)) return;
            handler.OnAnimatorStateMove(animator, stateInfo, layerIndex);
        }
        #endregion
    }
}

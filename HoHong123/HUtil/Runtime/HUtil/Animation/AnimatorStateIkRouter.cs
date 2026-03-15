#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Animator IK 처리 이벤트를 Handler로 전달하는 Router입니다.
 * Animator의 OnStateIK 이벤트가 발생하면 IAnimatorStateIkHandler 인터페이스를
 * 구현한 컴포넌트로 전달합니다.
 *
 * 사용처 ::
 * Animator IK 처리 로직
 * =========================================================
 */
#endif

using UnityEngine;

namespace HUtil.Animation {
    public class AnimatorStateIkRouter : BaseAnimatorStateRouter<IAnimatorStateIkHandler> {
        #region State Handler
        public override void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            InitHandler(animator);
            if (!IsTargetState(stateInfo)) return;
            handler.OnAnimatorStateIK(animator, stateInfo, layerIndex);
        }
        #endregion
    }
}

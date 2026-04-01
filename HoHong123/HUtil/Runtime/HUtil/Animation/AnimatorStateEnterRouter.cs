#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Animator State Enter 이벤트를 Handler로 전달하는 Router입니다.
 * Animator의 OnStateEnter 이벤트가 발생하면 IAnimatorStateEnterHandler 인터페이스를
 * 구현한 컴포넌트로 전달합니다.
 *
 * 사용처 ::
 * Animator State 진입 시 로직 처리
 * =========================================================
 */
#endif

using UnityEngine;

namespace HUtil.Animation {
    public class AnimatorStateEnterRouter : BaseAnimatorStateRouter<IAnimatorStateEnterHandler> {
        #region State Handler
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            InitHandler(animator);
            if (!IsTargetState(stateInfo)) return;
            handler.OnAnimatorStateEnter(animator, stateInfo, layerIndex);
        }
        #endregion
    }
}

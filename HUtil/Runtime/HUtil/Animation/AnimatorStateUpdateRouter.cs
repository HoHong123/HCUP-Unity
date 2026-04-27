#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Animator State Update 이벤트를 Handler로 전달하는 Router입니다.
 * Animator의 OnStateUpdate 이벤트가 발생하면 IAnimatorStateUpdateHandler
 * 인터페이스를구현한 컴포넌트로 전달합니다.
 *
 * 사용처 ::
 * Animator State 업데이트 로직 처리
 * =========================================================
 */
#endif

using UnityEngine;

namespace HUtil.Animation {
    public class AnimatorStateUpdateRouter : BaseAnimatorStateRouter<IAnimatorStateUpdateHandler> {
        #region State Handler
        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            InitHandler(animator);
            if (!IsTargetState(stateInfo)) return;
            handler.OnAnimatorStateUpdate(animator, stateInfo, layerIndex);
        }
        #endregion
    }
}

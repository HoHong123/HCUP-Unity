#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Animator StateMachine 진입 이벤트를 Handler로 전달하는 Router입니다.
 * Animator의 OnStateMachineEnter 이벤트가 발생하면 IAnimatorStateMachineEnterHandler
 * 인터페이스를 구현한 컴포넌트로 전달합니다.
 *
 * 사용처 ::
 * Animator StateMachine 진입 시 로직 처리
 * =========================================================
 */
#endif

using UnityEngine;

namespace HUtil.Animation {
    public class AnimatorStateMachineEnterRouter : BaseAnimatorStateRouter<IAnimatorStateMachineEnterHandler> {
        #region State Machine Handler
        public override void OnStateMachineEnter(Animator animator, int stateMachinePathHash) {
            InitHandler(animator);
            handler.OnAnimatorStateMachineEnter(animator, stateMachinePathHash);
        }
        #endregion
    }
}

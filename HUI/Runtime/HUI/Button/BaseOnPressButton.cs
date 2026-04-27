#if UNITY_EDITOR
/* =========================================================
 * DelegateButton의 입력 이벤트를 기반으로 동작하는 버튼 동작 확장 베이스 클래스입니다.
 * 버튼 입력에 따른 다양한 UI 동작을 컴포넌트 단위로 확장하기 위해 사용됩니다.
 *
 * 주의사항 ::
 * 1. 이 스크립트는 DelegateButton 컴포넌트가 반드시 필요합니다.
 * 2. 실제 버튼 동작은 상속 클래스에서 OnPointDown / OnPointUp을 구현해야 합니다.
 * 3. Interaction 상태 변경 이벤트는 useInteractionChangeEvent가 활성화된 경우에만 연결됩니다.
 * =========================================================
 */
#endif

using UnityEngine;

namespace HUI.ButtonUI {
    [RequireComponent(typeof(DelegateButton))]
    public abstract class BaseOnPressButton : MonoBehaviour, IDelegateButton {
        #region Fields
        protected bool useInteractionChangeEvent;
        protected DelegateButton Button;
        #endregion

        #region Protected - Unity Life Cycle
        protected virtual void Awake() {
            Button = GetComponent<DelegateButton>();
            ConnectButton();
        }

        private void OnDestroy() {
            DisconnectButton();
        }
        #endregion

        #region Public - UI Events
        public abstract void OnPointDown();
        public abstract void OnPointUp();
        public virtual void OnButtonInteractive() { }
        public virtual void OnButtonNonInteractive() { }
        #endregion

        #region Protected - Button Connection
        protected void ConnectButton() {
            DisconnectButton();

            Button.OnPointDown += OnPointDown;
            Button.OnPointUp += OnPointUp;
            
            if (useInteractionChangeEvent) {
                Button.OnButtonInteractive += OnButtonInteractive;
                Button.OnButtonNonInteractive += OnButtonNonInteractive;
            }
        }

        protected void DisconnectButton() {
            Button.OnPointDown -= OnPointDown;
            Button.OnPointUp -= OnPointUp;

            if (useInteractionChangeEvent) {
                Button.OnButtonInteractive -= OnButtonInteractive;
                Button.OnButtonNonInteractive -= OnButtonNonInteractive;
            }
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 *
 * 주요 기능 ::
 * 1. DelegateButton의 Pointer Down / Up 이벤트를 수신합니다.
 * 2. 필요 시 버튼 Interaction 상태 변경 이벤트를 수신합니다.
 * 3. 자식 클래스에서 버튼 입력 반응 로직을 구현할 수 있도록 베이스 구조를 제공합니다.
 *
 * 사용법 ::
 * 1. BaseOnPressButton을 상속받는 버튼 확장 클래스를 작성합니다.
 * 2. OnPointDown(), OnPointUp()을 반드시 구현합니다.
 * 3. Interaction 상태 변경이 필요하면 useInteractionChangeEvent를 true로 설정하고
 *    OnButtonInteractive(), OnButtonNonInteractive()를 override하여 사용합니다.
 * 4. DelegateButton은 같은 GameObject에 반드시 존재해야 합니다.
 *
 * 기타 ::
 * 1. Awake에서 DelegateButton을 가져오고 자동으로 이벤트를 연결합니다.
 * 2. OnDestroy에서 이벤트를 해제하여 중복 구독 및 잔여 참조를 방지합니다.
 * =========================================================
 */
#endif
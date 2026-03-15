#if UNITY_EDITOR
/* =========================================================
 * Unity Toggle 기반 커스텀 UI 토글 시스템의 베이스 클래스입니다.
 *
 * 목적 ::
 * Unity Toggle 이벤트를 확장하여 UI 애니메이션 및 시각 효과를 쉽게 연결할 수 있도록 설계되었습니다.
 *
 * 특징 ::
 * 1. Toggle 상태 기반 콜백 제공
 * 2. PointerDown / PointerUp 이벤트 지원
 * 3. 외부 관리자에서 강제 초기화 및 동기화 지원
 *
 * 주의사항 ::
 * Toggle 컴포넌트가 반드시 동일 GameObject에 존재해야 합니다.
 * =========================================================
 */
#endif

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;

namespace HUI.ToggleUI {
    [RequireComponent(typeof(Toggle))]
    public abstract class BaseCustomToggle : MonoBehaviour, IDelegateToggle, IPointerDownHandler, IPointerUpHandler {
        #region Fields
        [Title("Event Timing")]
        [SerializeField]
        protected bool activateOnSelect = true;
        [SerializeField]
        protected bool activateOnPointerDown = false;
        [SerializeField]
        protected bool activateOnPointerUp = false;

        [Title("References")]
        [SerializeField]
        Toggle toggle;

        bool isInitialized;
        #endregion

        #region Properties
        public Toggle Toggle => toggle;
        #endregion

        #region Unity Lifecycle
        private void Awake() {
            _EnsureInitialized();
        }

        private void OnEnable() {
            _EnsureInitialized();
            SyncToToggleState(immediate: false);
        }
        #endregion

        #region Public - Init / Sync
        /// <summary>
        /// 비활성 상태에서도 외부(관리자)가 호출해서 리스너 등록 + 현재 상태 반영까지 강제 수행.
        /// </summary>
        public void ForceInitializeAndSync() {
            _EnsureInitialized();
            SyncToToggleState(immediate: !gameObject.activeInHierarchy);
        }

        public void SyncToToggleState(bool immediate) {
#if UNITY_ASSERTIONS
            UnityEngine.Assertions.Assert.IsNotNull(toggle, "[BaseCustomToggle] Toggle is null.");
#endif
            OnToggleActive(toggle.isOn, immediate);
        }
        #endregion

        #region Protected - Callbacks
        public virtual void OnToggleActive(bool isOn) => OnToggleActive(isOn, immediate: false);
        public abstract void OnToggleActive(bool isOn, bool immediate);
        public abstract void OnPointerDown(PointerEventData eventData);
        public abstract void OnPointerUp(PointerEventData eventData);
        #endregion

        #region Private
        private void _EnsureInitialized() {
            if (isInitialized) return;
            if (toggle == null) toggle = GetComponent<Toggle>();
#if UNITY_ASSERTIONS
            UnityEngine.Assertions.Assert.IsNotNull(toggle, "[BaseCustomToggle] Toggle component missing.");
#endif
            toggle.onValueChanged.AddListener(_OnToggleValueChanged);
            isInitialized = true;
        }

        private void _OnToggleValueChanged(bool isOn) {
            OnToggleActive(isOn, immediate: false);
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 *
 * 주요 기능 ::
 * 1. Toggle 상태 변경 이벤트 처리
 * 2. PointerDown / PointerUp 이벤트 처리
 * 3. 외부 강제 초기화
 *  + ForceInitializeAndSync
 * 4. Toggle 상태 동기화
 *  + SyncToToggleState
 *
 * 상속 구조 ::
 * BaseCustomToggle
 *  ├ ColorOnSelectToggle
 *  ├ MoveOnSelectToggle
 *  ├ ScaleOnSelectToggle
 *  └ OnOffDelegatorToggle
 *
 * 동작 흐름 ::
 * Awake
 *  → _EnsureInitialized
 * Toggle 변경
 *  → _OnToggleValueChanged
 *  → OnToggleActive
 *
 * 사용법 ::
 * BaseCustomToggle을 상속하여
 * OnToggleActive / Pointer 이벤트를 구현합니다.
 *
 * 기타 ::
 * Toggle 이벤트 등록은 최초 초기화 시 1회만 수행됩니다.
 * =========================================================
 */
#endif
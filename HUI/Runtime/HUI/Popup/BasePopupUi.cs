using System;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

namespace HUI.Popup {
    public class BasePopupUi : SerializedMonoBehaviour, IBasicPanel {
        [Title("Panel")]
        [SerializeField]
        protected GameObject panel;

        [Title("UI")]
        [SerializeField]
        protected Button closeBtn;

        public event Action OnClickCancel;

        public bool IsActive => panel.activeSelf;


        protected virtual void Start() {
            closeBtn.onClick.AddListener(_HandleCloseClicked);
        }

        // 등록과 대칭되는 해제를 위해 명명 메서드로 승격.
        protected virtual void OnDestroy() {
            if (closeBtn != null) {
                closeBtn.onClick.RemoveListener(_HandleCloseClicked);
            }
            OnClickCancel = null;
        }

        private void _HandleCloseClicked() {
            OnClickCancel?.Invoke();
        }


        public virtual void Open() => panel.SetActive(true);
        public virtual void Close() => panel.SetActive(false);
    }
}
using System;
using UnityEngine;
using UnityEngine.UI;
using HInspector;

namespace HUI.Popup {
    public class BasePopupUi : MonoBehaviour, IBasicPanel {
        [HTitle("Panel")]
        [SerializeField]
        protected GameObject panel;

        [HTitle("UI")]
        [SerializeField]
        protected Button closeBtn;

        public event Action OnClickCancel;

        public bool IsActive => panel.activeSelf;


        protected virtual void Start() {
            OnClickCancel += Close;

            closeBtn.onClick.AddListener(_HandleCloseClicked);
        }

        protected virtual void OnDestroy() {
            OnClickCancel = null;
            if (closeBtn != null) {
                closeBtn.onClick.RemoveAllListeners();
            }
        }

        private void _HandleCloseClicked() {
            OnClickCancel?.Invoke();
        }


        public virtual void Open() => panel.SetActive(true);
        public virtual void Close() => panel.SetActive(false);
    }
}
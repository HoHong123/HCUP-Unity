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

        // 팝업이 스스로 닫혔음을 매니저에 알리는 유일한 경로. 이것이 없어서
        // PopupManager 는 이미지/비디오 팝업이 닫힌 사실을 영원히 알지 못했다 (배경 잔존).
        public event Action<BasePopupUi> OnClosed;

        public bool IsActive => panel != null && panel.activeSelf;


        protected virtual void Start() {
            OnClickCancel += Close;

            closeBtn.onClick.AddListener(_HandleCloseClicked);
        }

        protected virtual void OnDestroy() {
            OnClickCancel = null;
            OnClosed = null;
            if (closeBtn != null) {
                closeBtn.onClick.RemoveAllListeners();
            }
        }

        private void _HandleCloseClicked() {
            OnClickCancel?.Invoke();
        }


        public virtual void Open() {
            if (panel != null) panel.SetActive(true);
        }

        public virtual void Close() {
            bool wasActive = IsActive;
            if (panel != null) panel.SetActive(false);
            if (wasActive) OnClosed?.Invoke(this);
        }
    }
}
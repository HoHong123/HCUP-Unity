using System;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

namespace Util.UI.Popup {
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
            closeBtn.onClick.AddListener(() => OnClickCancel?.Invoke());
        }


        public virtual void Open() => panel.SetActive(true);
        public virtual void Close() => panel.SetActive(false);
    }
}
using System;
using UnityEngine;
using UnityEngine.UI;
using Util.OdinCompat;
using Util.UI.Panel;

namespace Util.UI.Popup {
    public class BasePopupUi : MonoBehaviour, IBasicPanel {
        [HeaderOrTitle("Panel")]
        [SerializeField]
        protected GameObject panel;

        [HeaderOrTitle("UI")]
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
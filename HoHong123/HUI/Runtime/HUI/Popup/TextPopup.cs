using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;

namespace HUI.Popup {
    public class TextPopup : BasePopupUi {
        [Title("Texts")]
        [SerializeField]
        TMP_Text titleTxt;
        [SerializeField]
        TMP_Text bodyTxt;

        [Title("UI")]
        [SerializeField]
        Image titleBgImg;
        [SerializeField]
        Image bodyBgImg;
        [SerializeField]
        Button okBtn;

        Action lastCancelEvent = null;

        public event Action OnClickOk;

        public Color TitleColor { set => titleBgImg.color = value; }
        public Color TitleTextColor { set => titleTxt.color = value; }
        public Color BodyColor { set => bodyBgImg.color = value; }
        public Color BodyTextColor { set => bodyTxt.color = value; }


        protected override void Start() {
            base.Start();
            OnClickCancel += _OnCancelEvent;
            okBtn.onClick.AddListener(() => OnClickOk?.Invoke());
        }

        public void SetText(string title, string message, Action okEvent = null, Action cancelEvent = null) {
            titleTxt.text = title;
            bodyTxt.text = message;

            OnClickOk = null;
            OnClickOk = okEvent;
            okBtn.gameObject.SetActive((okEvent != null));

            lastCancelEvent = cancelEvent;
        }

        private void _OnCancelEvent() {
            if (lastCancelEvent != null) {
                var action = lastCancelEvent;
                lastCancelEvent = null;
                action.Invoke();
            }
        }
    }
}
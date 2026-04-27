using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HInspector;

namespace HUI.Popup {
    public class TextPopup : BasePopupUi {
        [HTitle("Texts")]
        [SerializeField]
        TMP_Text titleTxt;
        [SerializeField]
        TMP_Text bodyTxt;

        [HTitle("UI")]
        [SerializeField]
        Image titleBgImg;
        [SerializeField]
        Image bodyBgImg;
        [SerializeField]
        Button okBtn;
        [SerializeField]
        TMP_Text okTxt;
        [SerializeField]
        TMP_Text cancelTxt;

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

        public void SetText(
            string title, string message,
            Action okEvent = null, Action cancelEvent = null,
            string okBtnTxt = null, string cancelBtnTxt = null) {
            titleTxt.text = title;
            bodyTxt.text = message;

            OnClickOk = null;
            OnClickOk = okEvent;

            var isOkActive = (okEvent != null);
            okBtn.gameObject.SetActive(isOkActive);
            if (isOkActive) okTxt.text = okBtnTxt ?? "확인";

            cancelTxt.text = cancelBtnTxt ?? "닫기";

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
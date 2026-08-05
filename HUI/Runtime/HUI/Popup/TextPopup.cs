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
        Button okBtn;
        [SerializeField]
        TMP_Text okTxt;
        [SerializeField]
        TMP_Text cancelTxt;

        Action lastCancelEvent = null;

        public event Action OnClickOk;

        public Color TitleTextColor { set => titleTxt.color = value; }
        public Color BodyTextColor { set => bodyTxt.color = value; }


        protected override void Start() {
            base.Start();
            OnClickCancel += _OnCancelEvent;
            okBtn.onClick.AddListener(() => OnClickOk?.Invoke());
        }

        // showOk 를 별도로 받는다. 호출자가 okEvent 에 내부 큐 진행 콜백을 합성해 넘기면
        // okEvent != null 판정이 항상 참이 되어 "취소 전용 팝업" 을 만들 수 없었다.
        public void SetText(
            string title, string message,
            Action okEvent = null, Action cancelEvent = null,
            string okBtnTxt = null, string cancelBtnTxt = null,
            bool showOk = true) {
            titleTxt.text = title;
            bodyTxt.text = message;

            OnClickOk = null;
            OnClickOk = okEvent;

            var isOkActive = showOk && (okEvent != null);
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

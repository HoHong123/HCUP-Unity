using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;
using PoolDispose = HUtil.Pooling.IPoolDispose<HUtil.UI.Popup.TextPopup>;
using PoolReturn = HUtil.Pooling.IPoolReturn<HUtil.UI.Popup.TextPopup>;

namespace HUtil.UI.Popup {
    public class TextPopup : BasePopupUi, PoolReturn, PoolDispose {
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

        public event Action OnClickOk;

        public Color TitleColor { set => titleBgImg.color = value; }
        public Color TitleTextColor { set => titleTxt.color = value; }
        public Color BodyColor { set => bodyBgImg.color = value; }
        public Color BodyTextColor { set => bodyTxt.color = value; }


        protected override void Start() {
            base.Start();
            okBtn.onClick.AddListener(() => OnClickOk?.Invoke());
        }

        public void SetText(string title, string message, Action triggerEvent = null) {
            titleTxt.text = title;
            bodyTxt.text = message;
            OnClickOk = triggerEvent;
            okBtn.gameObject.SetActive((triggerEvent != null));
        }


        public void OnReturn(TextPopup mono) {
            mono.titleTxt.text = string.Empty;
            mono.bodyTxt.text = string.Empty;
        }

        public void OnDispose(TextPopup mono) {
            Destroy(mono.panel);
        }
    }
}
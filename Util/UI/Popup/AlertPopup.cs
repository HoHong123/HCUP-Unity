using UnityEngine;
using TMPro;
using PoolReturn = Util.Pooling.IPoolReturn<Util.UI.Popup.AlertPopup>;
using PoolDispose = Util.Pooling.IPoolDispose<Util.UI.Popup.AlertPopup>;
using Util.OdinCompat;

namespace Util.UI.Popup {
    public class AlertPopup : BasePopupUi, PoolReturn, PoolDispose {
        [HeaderOrTitle("Texts")]
        [SerializeField]
        TMP_Text titleTxt;
        [SerializeField]
        TMP_Text descriptionTxt;


        public void SetUi(string title, string message) {
            titleTxt.text = title;
            descriptionTxt.text = message;
        }


        public void OnReturn(AlertPopup mono) {
            mono.titleTxt.text = string.Empty;
            mono.descriptionTxt.text = string.Empty;
        }

        public void OnDispose(AlertPopup mono) {
            Destroy(mono.panel);
        }
    }
}
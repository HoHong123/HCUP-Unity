using UnityEngine;
using TMPro;
using Sirenix.OdinInspector;
using PoolDispose = IPoolDispose<Util.UI.Popup.AlertPopup>;
using PoolReturn = IPoolReturn<Util.UI.Popup.AlertPopup>;

namespace Util.UI.Popup {
    public class AlertPopup : BasePopupUi, PoolReturn, PoolDispose {
        [Title("Texts")]
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
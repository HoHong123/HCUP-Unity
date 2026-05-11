using UnityEngine;
using TMPro;
using HInspector;

namespace HUI.Popup {
    public class AlertPopup : BasePopupUi {
        [HTitle("Texts")]
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
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using HUI.ScrollView;

namespace HUI.DebugConsole {
    public sealed class HLogCellView : BaseRecycleCellView<HLogCellData> {
        [SerializeField]
        TextMeshProUGUI messageText;
        [SerializeField]
        Button selectButton;

        HLogCellData cellData;
        public System.Action<HLogCellData> OnClick { get; set; }

        public override void Bind(HLogCellData data) {
            cellData = data;
            if (messageText != null) messageText.text = data.DisplayText;
            if (selectButton != null) {
                selectButton.onClick.RemoveListener(_OnClick);
                selectButton.onClick.AddListener(_OnClick);
            }
        }

        public override void Dispose() {
            if (selectButton != null) selectButton.onClick.RemoveListener(_OnClick);
            cellData = null;
        }

        private void _OnClick() {
            if (cellData == null) return;
            OnClick?.Invoke(cellData);
        }
    }
}

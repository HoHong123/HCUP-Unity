using TMPro;
using UnityEngine;
using UnityEngine.UI;
using HUI.ScrollView;
using HUtil.Logger;

namespace HUI.DebugConsole {
    public sealed class HLogCellView : BaseRecycleCellView<HLogCellData> {
        #region Static Colors
        static readonly Color LOG_COLOR = new(0.45f, 0.45f, 0.45f, 1f);
        static readonly Color WARN_COLOR = new(0.95f, 0.70f, 0.15f, 1f);
        static readonly Color ERROR_COLOR = new(0.90f, 0.25f, 0.25f, 1f);
        #endregion

        #region Fields
        [SerializeField]
        Image levelBar;
        [SerializeField]
        TMP_Text messageTxt;
        [SerializeField]
        Button selectBtn;

        HLogCellData cellData;
        #endregion

        #region Properties
        public System.Action<HLogCellData> OnClick { get; set; }
        #endregion

        #region Public - Bind/Dispose
        public override void Bind(HLogCellData data) {
            cellData = data;

            messageTxt.text = data.DisplayText;

            levelBar.color = _GetLevelColor(data.Level);

            selectBtn.onClick.RemoveListener(_OnClick);
            selectBtn.onClick.AddListener(_OnClick);
        }

        public override void Dispose() {
            selectBtn.onClick.RemoveListener(_OnClick);

            cellData = null;
        }
        #endregion

        #region Private Functions
        private void _OnClick() {
            if (cellData == null)
                return;
            OnClick?.Invoke(cellData);
        }

        private static Color _GetLevelColor(LogLevel level) {
            switch (level) {
            case LogLevel.Warn:
                return WARN_COLOR;

            case LogLevel.Error:
            case LogLevel.Fatal:
            case LogLevel.Assert:
                return ERROR_COLOR;

            default:
                return LOG_COLOR;
            }
        }
        #endregion
    }
}

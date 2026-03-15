using HUI.ScrollView;

namespace HUI.DebugConsole {
    public sealed class HLogRecycleView : VerticalRecycleView<HLogCellView, HLogCellData> {
        public System.Action<HLogCellData> OnCellClicked { get; set; }

        protected override void OnCellCreated(HLogCellView cell, int index, HLogCellData data) {
            cell.OnClick = OnCellClicked;
        }
    }
}

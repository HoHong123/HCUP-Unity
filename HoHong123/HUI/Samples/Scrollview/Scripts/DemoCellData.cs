using HUI.ScrollView;

public class DemoCellData : BaseRecycleCellData, IGridSpanData {
    public string tester;
    public int SpanX { get; private set; } = 1;
    public int SpanY { get; private set; } = 1;

    public DemoCellData(string tester) {
        this.tester = tester;
    }

    public DemoCellData(string tester, int spanX, int spanY) : this(tester) {
        SpanX = spanX;
        SpanY = spanY;
    }
}

namespace HUI.ScrollView {
    /// <summary>
    /// 데이터가 그리드에서 차지할 스팬(열/행) 정보를 제공.
    /// - SpanX: 가로(열) 칸 수
    /// - SpanY: 세로(행) 칸 수
    /// </summary>
    public interface IGridSpanData {
        int SpanX { get; }
        int SpanY { get; }
    }
}

using System.Collections.Generic;
using Util.UI.ScrollView;

public class VlgTesterScroller : VlgRecycleView<DemoCellView, DemoCellData> {
    public int dataSize = 500;
    
    List<DemoCellData> data = new();

    private void Start() {
        _Test();
    }

    private void _Test() {
        for (int k = 0; k < dataSize; k++) {
            data.Add(new($"Object No.{k + 1}"));
        }
        SetData(data);
    }
}

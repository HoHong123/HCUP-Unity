using TMPro;
using Util.UI.ScrollView;

public class DemoCellView : BaseRecycleCellView<DemoCellData> {
    public TMP_Text Text;

    public DemoCellView(int key) : base(key) {}

    public override void Bind(DemoCellData data) {
       Text.text = data.tester;
    }

    public override void Dispose() {
        Destroy(gameObject);
    }
}

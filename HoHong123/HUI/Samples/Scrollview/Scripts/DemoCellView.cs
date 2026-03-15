using HUI.ScrollView;
using TMPro;

public class DemoCellView : BaseRecycleCellView<DemoCellData> {
    public TMP_Text Text;

    public override void Bind(DemoCellData data) {
       Text.text = data.tester;
    }

    public override void Dispose() {
        Destroy(gameObject);
    }
}

using UnityEngine;

namespace HUI.ScrollView {
    public abstract class BaseRecycleCellView<TCellData> : MonoBehaviour
        where TCellData : BaseRecycleCellData {
        public abstract void Bind(TCellData data);
        public abstract void Dispose();
    }
}

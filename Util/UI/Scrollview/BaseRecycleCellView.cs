using UnityEngine;

namespace Util.UI.ScrollView {
    public abstract class BaseRecycleCellView<TCellData> : MonoBehaviour
        where TCellData : BaseRecycleCellData {
        [SerializeField]
        Vector2 itemSize = Vector2.zero;

        /// <summary> Object UID </summary>
        public int Key { get; private set; }
        public Vector2 ItemSize => itemSize;


        public BaseRecycleCellView(int key) {
            Key = key;
        }

        public abstract void Bind(TCellData data);
        public abstract void Dispose();
    }
}

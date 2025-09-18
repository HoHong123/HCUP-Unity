using System;
using UnityEngine;
using Sirenix.OdinInspector;

namespace Util.UI.ScrollView {
    [Serializable]
    public class VerticalRecycleView<TCellView, TCellData> : BaseRecycleView<TCellView, TCellData>, IRecycleView
        where TCellData : BaseRecycleCellData
        where TCellView : BaseRecycleCellView<TCellData> {
        [Title("Cell View")]
        [SerializeField]
        float spacing = 10f;
        [SerializeField]
        float itemHeight = 100f;

        public float TotalContentSize => (itemHeight + spacing) * Count - spacing;


        protected override void Awake() {
            base.Awake();
            scrollRect.vertical = true;
            scrollRect.horizontal = false;
            scrollRect.onValueChanged.AddListener(_OnScrollValueChanged);
        }


        public override void ScrollToIndex(int index, bool center = true) {
            if (dataList == null || Count == 0 || index < 0 || index >= Count) return;

            float itemSpace = itemHeight + spacing;
            float centerOffset = center ? (viewport.rect.height - itemSpace) / 2f : 0f;
            float targetY = index * itemSpace - centerOffset;
            float maxScrollY = Mathf.Max(0f, content.sizeDelta.y - viewport.rect.height);
            targetY = Mathf.Clamp(targetY, 0f, maxScrollY);

            var pos = content.anchoredPosition;
            content.anchoredPosition = new Vector2(pos.x, targetY);

            UpdateVisibleItems();
        }


        protected override void UpdateVisibleCount() {
            VisibleCount = Mathf.CeilToInt(viewport.rect.height / (itemHeight + spacing));
        }
        protected override void UpdateContentSize() {
            float totalHeight = (itemHeight + spacing) * Count - spacing;
            float contentHeight = Mathf.Max(totalHeight, viewport.rect.height);
            content.sizeDelta = new Vector2(content.sizeDelta.x, contentHeight);
        }
        protected override void UpdateVisibleItems() {
            if (Count == 0) return;

            float totalHeight = (itemHeight + spacing) * Count - spacing;
            bool isVirtualizing = totalHeight > viewport.rect.height;

            if (!isVirtualizing) {
                RecycleInvisibleItems(0, Count - 1);
                for (int k = 0; k < Count; k++) {
                    if (!activeItems.ContainsKey(k)) {
                        _CreateCell(k);
                    }
                }
                return;
            }

            float scrollY = content.anchoredPosition.y;
            int start = Mathf.Max(0, Mathf.FloorToInt(scrollY / (itemHeight + spacing)));
            int end = Mathf.Min(Count - 1, start + VisibleCount);

            RecycleInvisibleItems(start, end);
            
            if (start == lastStartIndex && end == lastEndIndex) return;
            lastStartIndex = start;
            lastEndIndex = end;

            for (int k = start; k <= end; k++) {
                if (k >= Count) continue;
                if (!activeItems.ContainsKey(k)) {
                    _CreateCell(k);
                }
            }
        }


        private void _OnScrollValueChanged(Vector2 pos) {
            UpdateVisibleItems();
        }

        private void _CreateCell(int index) {
            var cell = itemPool.Get();
            var rect = cell.GetComponent<RectTransform>();
            cell.Bind(dataList[index]);
            cell.gameObject.SetActive(true);
            rect.SetParent(content, false);
            rect.sizeDelta = new Vector2(content.sizeDelta.x, itemHeight);
            rect.anchoredPosition = new Vector2(0, -index * (itemHeight + spacing));
            activeItems[index] = cell;
        }
    }
}

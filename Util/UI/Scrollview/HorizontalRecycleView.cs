using System;
using UnityEngine;
using Sirenix.OdinInspector;

namespace Util.UI.ScrollView {
    [Serializable]
    public class HorizontalRecycleView<TCellView, TCellData> : BaseRecycleView<TCellView, TCellData>, IRecycleView
        where TCellData : BaseRecycleCellData
        where TCellView : BaseRecycleCellView<TCellData> {
        [Title("Cell View")]
        [SerializeField]
        float spacing = 10f;
        [SerializeField]
        float itemWidth = 100f;

        public float TotalContentSize => (itemWidth + spacing) * Count - spacing;


        protected override void Awake() {
            base.Awake();
            scrollRect.vertical = false;
            scrollRect.horizontal = true;
            scrollRect.onValueChanged.AddListener(_OnScrollValueChanged);
        }

        public override void ScrollToIndex(int index, bool center = true) {
            if (dataList == null || Count == 0 || index < 0 || index >= Count) return;

            float itemSpace = itemWidth + spacing;
            float centerOffset = center ? (viewport.rect.width - itemSpace) / 2f : 0f;
            float targetX = index * itemSpace - centerOffset;
            float maxScrollX = Mathf.Max(0f, content.sizeDelta.x - viewport.rect.width);
            targetX = Mathf.Clamp(targetX, 0f, maxScrollX);

            var pos = content.anchoredPosition;
            content.anchoredPosition = new Vector2(targetX, pos.y);

            UpdateVisibleItems();
        }


        protected override void UpdateVisibleCount() {
            VisibleCount = Mathf.CeilToInt(viewport.rect.width / (itemWidth + spacing));
        }
        protected override void UpdateContentSize() {
            float totalWidth = (itemWidth + spacing) * Count - spacing;
            float contentWidth = Mathf.Max(totalWidth, viewport.rect.width);
            content.sizeDelta = new Vector2(contentWidth, content.sizeDelta.y);
        }
        protected override void UpdateVisibleItems() {
            if (Count == 0) return;

            float totalWidth = (itemWidth + spacing) * Count - spacing;
            bool isVirtualizing = totalWidth > viewport.rect.width;

            if (!isVirtualizing) {
                RecycleInvisibleItems(0, Count - 1);
                for (int k = 0; k < Count; k++) {
                    if (!activeItems.ContainsKey(k)) {
                        _CreateCell(k);
                    }
                }
                return;
            }

            float scrollX = -content.anchoredPosition.x;
            int start = Mathf.Max(0, Mathf.FloorToInt(scrollX / (itemWidth + spacing)));
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
            rect.sizeDelta = new Vector2(itemWidth, content.sizeDelta.y);
            rect.anchoredPosition = new Vector2(index * (itemWidth + spacing), 0);
            activeItems[index] = cell;
        }
    }
}

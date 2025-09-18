using System;
using UnityEngine;
using Sirenix.OdinInspector;

namespace Util.UI.ScrollView {
    [Serializable]
    public class GridRecycleView<TCellView, TCellData> : BaseRecycleView<TCellView, TCellData>, IRecycleView
        where TCellData : BaseRecycleCellData
        where TCellView : BaseRecycleCellView<TCellData> {

        [Title("Grid Settings")]
        [SerializeField]
        bool isHorizontal = true;
        [SerializeField]
        Vector2 spacing = new Vector2(10f, 10f);
        [SerializeField]
        Vector2 cellSize = new Vector2(100f, 100f);

        [Title("Grid Line Control")]
        [SerializeField]
        bool useFixedCount = false;
        [SerializeField, ShowIf("useFixedCount")]
        int fixedCount = 1;

        int rowCount;
        int columnCount;


        public float TotalContentSize {
            get {
                int primaryCount = Mathf.CeilToInt((float)Count / secondaryCount);
                return (primarySize + primarySpacing) * primaryCount - primarySpacing;
            }
        }

        float primarySize => isHorizontal ? cellSize.x : cellSize.y;
        float primarySpacing => isHorizontal ? spacing.x : spacing.y;
        float secondarySize => isHorizontal ? cellSize.y : cellSize.x;
        float secondarySpacing => isHorizontal ? spacing.y : spacing.x;
        int secondaryCount => isHorizontal ? rowCount : columnCount;


        protected override void Awake() {
            base.Awake();
            scrollRect.vertical = !isHorizontal;
            scrollRect.horizontal = isHorizontal;
            scrollRect.onValueChanged.AddListener(_OnScrollValueChanged);
        }

        public override void ScrollToIndex(int index, bool center = true) {
            if (dataList == null || Count == 0 || index < 0 || index >= Count) return;

            int primaryIndex = index / secondaryCount;
            float targetPos = primaryIndex * (primarySize + primarySpacing);
            float centerOffset = center ? ((isHorizontal ? viewport.rect.width : viewport.rect.height) - primarySize) / 2f : 0f;
            float maxScroll = Mathf.Max(0f, TotalContentSize - (isHorizontal ? viewport.rect.width : viewport.rect.height));
            float scrollTarget = Mathf.Clamp(targetPos - centerOffset, 0f, maxScroll);

            var pos = content.anchoredPosition;
            if (isHorizontal)
                pos.x = scrollTarget;
            else
                pos.y = scrollTarget;

            content.anchoredPosition = pos;

            UpdateVisibleItems();
        }


        protected override void UpdateVisibleCount() {
            float viewportPrimary = isHorizontal ? viewport.rect.width : viewport.rect.height;
            float viewportSecondary = isHorizontal ? viewport.rect.height : viewport.rect.width;

            int visiblePrimary = Mathf.CeilToInt(viewportPrimary / (primarySize + primarySpacing)) + 1;
            int visibleSecondary 
                = useFixedCount ? 
                fixedCount : 
                Mathf.FloorToInt(viewportSecondary / (secondarySize + secondarySpacing));

            if (isHorizontal) {
                columnCount = visiblePrimary;
                rowCount = visibleSecondary;
            }
            else {
                columnCount = visibleSecondary;
                rowCount = visiblePrimary;
            }

            VisibleCount = columnCount * rowCount;
        }

        protected override void UpdateContentSize() {
            int primaryCount = Mathf.CeilToInt((float)Count / secondaryCount);
            float contentLength = (primarySize + primarySpacing) * primaryCount - primarySpacing;
            var size = content.sizeDelta;

            if (isHorizontal)
                content.sizeDelta = new Vector2(Mathf.Max(contentLength, viewport.rect.width), size.y);
            else
                content.sizeDelta = new Vector2(size.x, Mathf.Max(contentLength, viewport.rect.height));
        }

        protected override void UpdateVisibleItems() {
            if (Count == 0) return;

            float scrollPos = isHorizontal ? content.anchoredPosition.x : content.anchoredPosition.y;
            scrollPos = Mathf.Abs(scrollPos);

            int startPrimary = Mathf.Max(0, Mathf.FloorToInt(scrollPos / (primarySize + primarySpacing)));
            int endPrimary = startPrimary + (isHorizontal ? columnCount : rowCount);

            int startIndex = startPrimary * secondaryCount;
            int endIndex = Mathf.Min(Count - 1, (endPrimary * secondaryCount) - 1);

            RecycleInvisibleItems(startIndex, endIndex);

            if (startIndex == lastStartIndex && endIndex == lastEndIndex) return;
            lastStartIndex = startIndex;
            lastEndIndex = endIndex;

            for (int i = startIndex; i <= endIndex; i++) {
                if (!activeItems.ContainsKey(i)) {
                    _CreateCell(i);
                }
            }
        }


        private void _OnScrollValueChanged(Vector2 _) {
            UpdateVisibleItems();
        }

        private void _CreateCell(int index) {
            var cell = itemPool.Get();
            var rect = cell.GetComponent<RectTransform>();
            cell.Bind(dataList[index]);
            cell.gameObject.SetActive(true);
            rect.SetParent(content, false);
            rect.sizeDelta = cellSize;

            int primary = index / secondaryCount;
            int secondary = index % secondaryCount;

            Vector2 anchored = isHorizontal
                ? new Vector2(primary * (cellSize.x + spacing.x), -secondary * (cellSize.y + spacing.y))
                : new Vector2(secondary * (cellSize.x + spacing.x), -primary * (cellSize.y + spacing.y));

            rect.anchoredPosition = anchored;
            activeItems[index] = cell;
        }
    }
}

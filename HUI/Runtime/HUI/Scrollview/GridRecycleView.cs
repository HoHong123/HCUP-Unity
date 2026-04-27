#if UNITY_EDITOR
/* =========================================================
 * Grid 형태의 Recycle ScrollView를 구현하는 베이스 클래스입니다.
 * 대량의 데이터를 Grid 배치 방식으로 효율적으로 표시하기 위해 Cell 재사용 기반 구조를 제공합니다.
 * 현재 Scroll 위치를 기준으로 Primary Axis 범위를 계산하고 해당 범위에 필요한 Cell만 생성 및 재사용합니다.
 *
 * 특징 ::
 * 1. 가로/세로 스크롤 전환 지원
 * 2. Grid 형태 Visible Cell 계산
 * 3. ComponentPool 기반 Cell 재사용
 * 4. Padding / Spacing / CellSize 설정 지원
 * 5. 고정 줄 수(Fixed Count) 또는 Viewport 기반 자동 계산 지원
 *
 * 주의사항 ::
 * secondaryCount(Row/Column 수)는 1 이상이어야 하며,
 * Cell은 재사용되므로 Bind 시 상태 초기화가 보장되어야 합니다.
 * =========================================================
 */
#endif

using System;
using UnityEngine;
using HInspector;

namespace HUI.ScrollView {
    [Serializable]
    public class GridRecycleView<TCellView, TCellData> :
        BaseRecycleView<TCellView, TCellData>, IRecycleView
        where TCellData : class
        where TCellView : BaseRecycleCellView<TCellData> {

        [HTitle("Grid Settings")]
        [SerializeField]
        bool isHorizontal = true;
        [SerializeField]
        Vector2 spacing = new Vector2(10f, 10f);
        [SerializeField]
        Vector2 cellSize = new Vector2(100f, 100f);

        [HTitle("Grid Line Control")]
        [SerializeField]
        bool useFixedCount = false;
        [SerializeField, HShowIf("useFixedCount")]
        int fixedCount = 1;

        [HTitle("Padding (Primary Axis)")]
        [SerializeField]
        float startPadding = 0f;
        [SerializeField]
        float endPadding = 0f;

        int rowCount;
        int columnCount;


        public float TotalContentSize {
            get {
                int primaryCount = Mathf.CeilToInt((float)Count / secondaryCount);
                float itemsLength = Mathf.Max(0f, (primarySize + primarySpacing) * primaryCount - primarySpacing);
                return startPadding + itemsLength + endPadding;
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
            float itemSpace = primarySize + primarySpacing;
            float targetPos = startPadding + primaryIndex * itemSpace;
            float viewportPrimary = isHorizontal ? viewport.rect.width : viewport.rect.height;
            float centerOffset = center ? (viewportPrimary - primarySize) / 2f : 0f;
            float maxScroll = Mathf.Max(0f, TotalContentSize - viewportPrimary);
            float scrollTarget = Mathf.Clamp(targetPos - centerOffset, 0f, maxScroll);

            var pos = content.anchoredPosition;
            if (isHorizontal) {
                pos.x = scrollTarget;
            }
            else {
                pos.y = scrollTarget;
            }

            content.anchoredPosition = pos;

            UpdateVisibleItems();
        }


        protected override void UpdateVisibleCount() {
            float viewportPrimary = isHorizontal ? viewport.rect.width : viewport.rect.height;
            float viewportSecondary = isHorizontal ? viewport.rect.height : viewport.rect.width;

            int visiblePrimary = Mathf.CeilToInt(viewportPrimary / (primarySize + primarySpacing)) + 1;
            int visibleSecondary = useFixedCount
                 ? Mathf.Max(1, fixedCount)
                 : Mathf.Max(1, Mathf.FloorToInt(viewportSecondary / (secondarySize + secondarySpacing)));

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
            float itemsLength = Mathf.Max(0f, (primarySize + primarySpacing) * primaryCount - primarySpacing);
            float contentLength = startPadding + itemsLength + endPadding;
            var size = content.sizeDelta;

            content.sizeDelta = isHorizontal ? 
                new Vector2(Mathf.Max(contentLength, viewport.rect.width), size.y) :
                new Vector2(size.x, Mathf.Max(contentLength, viewport.rect.height));
        }

        protected override void UpdateVisibleItems() {
            if (Count == 0) return;
            if (secondaryCount <= 0) {
                UpdateVisibleCount();
                return;
            }

            float scrollPos = isHorizontal ? content.anchoredPosition.x : content.anchoredPosition.y;
            scrollPos = Mathf.Abs(scrollPos);
            float offset = scrollPos - startPadding;
            if (offset < 0f) offset = 0f;

            float itemSpace = primarySize + primarySpacing;
            int startPrimary = Mathf.Max(0, Mathf.FloorToInt(offset / itemSpace));
            int endPrimary = startPrimary + (isHorizontal ? columnCount : rowCount);
            int startIndex = startPrimary * secondaryCount;
            int endIndex = Mathf.Min(Count - 1, (endPrimary * secondaryCount) - 1);

            RecycleInvisibleItems(startIndex, endIndex);

            if (startIndex == lastStartIndex && endIndex == lastEndIndex) return;
            lastStartIndex = startIndex;
            lastEndIndex = endIndex;

            for (int k = startIndex; k <= endIndex; k++) {
                if (!activeItems.ContainsKey(k)) {
                    CreateCell(k);
                }
            }
        }

        protected override void CreateCell(int index) {
            var cell = itemPool.Get();
            var rect = cell.GetComponent<RectTransform>();
            var data = dataList[index];

            cell.Bind(data);
            cell.gameObject.SetActive(true);
            rect.sizeDelta = cellSize;
            rect.SetParent(content, false);

            int primary = index / secondaryCount;
            int secondary = index % secondaryCount;

            Vector2 anchored = isHorizontal
                ? new Vector2(startPadding + primary * (cellSize.x + spacing.x), -secondary * (cellSize.y + spacing.y))
                : new Vector2(secondary * (cellSize.x + spacing.x), -(startPadding + primary * (cellSize.y + spacing.y)));

            rect.anchoredPosition = anchored;
            activeItems[index] = cell;

            OnCellCreated(cell, index, data);
        }


        private void _OnScrollValueChanged(Vector2 _) {
            UpdateVisibleItems();
        }
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH 2026.03.10
 *
 * 구조 ::
 * GridRecycleView<TCellView, TCellData>
 *     └ 실제 Grid ScrollView 구현 클래스
 *
 * 주요 기능 ::
 * 1. Grid Scroll 지원
 *  + Horizontal / Vertical 방향 전환
 * 2. Visible Count 계산
 * UpdateVisibleCount()
 *  + Viewport 크기 기준으로 현재 필요한 Cell 수 계산
 * 3. Content 크기 갱신
 * UpdateContentSize()
 *  + 전체 데이터 개수에 맞춰 Content 길이 계산
 * 4. Visible Cell 재생성
 * UpdateVisibleItems()
 *  + 현재 화면에 필요한 Cell만 생성 및 유지
 * 5. 특정 Index 이동
 * ScrollToIndex()
 *  + 특정 데이터 위치로 이동
 * 6. Grid Cell 생성
 * CreateCell()
 *  + Index 기반 Grid 좌표 계산 후 Cell 생성
 *
 * Grid 기준 ::
 * isHorizontal = true
 *  + X축 진행, Y축 줄 배치
 * isHorizontal = false
 *  + Y축 진행, X축 줄 배치
 *
 * 주요 필드 ::
 * cellSize
 *  + 개별 Cell 크기
 * spacing
 *  + Cell 간격
 * startPadding / endPadding
 *  + Primary Axis 시작/끝 여백
 * useFixedCount / fixedCount
 *  + 줄 수 고정 여부 및 고정 개수
 *
 * 사용법 ::
 * class ShopGridView : GridRecycleView<ShopItemUI, ShopData> {}
 * 이후 SetData(dataList)를 호출하여 Grid ScrollView를 구성합니다.
 *
 * 기타 ::
 * Grid 가상화 구조를 통해 전체 데이터를 한 번에 생성하지 않고 화면에 필요한 범위만
 * 생성하도록 설계되었습니다.
 * =========================================================
 */
#endif
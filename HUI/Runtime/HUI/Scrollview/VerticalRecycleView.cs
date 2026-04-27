#if UNITY_EDITOR
/* =========================================================
 * 이 스크립트는 세로 스크롤 전용 RecycleView를 담당하는 스크립트입니다.
 * Vertical 방향의 ScrollRect 환경에서 셀을 재사용하며, 현재 보이는 범위의 셀만 생성 및 유지합니다.
 *
 * 주의사항 ::
 * 1. 셀의 세로 크기(itemHeight), 간격(spacing), 상하 패딩 값은 실제 UI 레이아웃과 일치해야 합니다.
 * 2. virtualization 동작은 content 크기와 viewport 크기 비교를 기준으로 판단됩니다.
 * 3. ScrollToIndex 호출 시 index 범위가 유효하지 않으면 동작하지 않습니다.
 * 4. 셀 위치 계산은 상단부터 순차 배치되는 구조를 전제로 합니다.
 * 5. BaseRecycleView 내부 필드(scrollRect, content, viewport, itemPool, activeItems, dataList)에 정상 접근 가능해야 합니다.
 * ********** 중요 **********
 *  6. 사용되는 Item UI와 Context 모두 좌상 Anchore와 Pivot (0, 1)로 사용하셔야 합니다. 
 * =========================================================
 */
#endif

using System;
using UnityEngine;
using HInspector;

namespace HUI.ScrollView {
    [Serializable]
    public class VerticalRecycleView<TCellView, TCellData> : BaseRecycleView<TCellView, TCellData>, IRecycleView
        where TCellData : class
        where TCellView : BaseRecycleCellView<TCellData> {
        [HTitle("Cell View")]
        [SerializeField]
        float spacing = 10f;
        [SerializeField]
        float itemHeight = 100f;

        [HTitle("Padding")]
        [SerializeField]
        float topPadding = 0f;
        [SerializeField]
        float bottomPadding = 0f;

        public float TotalContentSize {
            get {
                float itemsHeight = Mathf.Max(0f, (itemHeight + spacing) * Count - spacing);
                return topPadding + itemsHeight + bottomPadding;
            }
        }


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
            float targetY = topPadding + index * itemSpace - centerOffset;
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
            float itemsHeight = Mathf.Max(0f, (itemHeight + spacing) * Count - spacing);
            float totalHeight = topPadding + itemsHeight + bottomPadding;
            float contentHeight = Mathf.Max(totalHeight, viewport.rect.height);
            content.sizeDelta = new Vector2(content.sizeDelta.x, contentHeight);
        }

        protected override void UpdateVisibleItems() {
            if (Count == 0) return;

            float itemsHeight = Mathf.Max(0f, (itemHeight + spacing) * Count - spacing);
            float totalHeight = topPadding + itemsHeight + bottomPadding;
            bool isVirtualizing = totalHeight > viewport.rect.height;

            if (!isVirtualizing) {
                RecycleInvisibleItems(0, Count - 1);
                for (int k = 0; k < Count; k++) {
                    if (!activeItems.ContainsKey(k)) {
                        CreateCell(k);
                    }
                }
                return;
            }

            float scrollY = content.anchoredPosition.y;
            float offset = scrollY - topPadding;
            if (offset < 0f) offset = 0f;

            int start = Mathf.Max(0, Mathf.FloorToInt(offset / (itemHeight + spacing)));
            int end = Mathf.Min(Count - 1, start + VisibleCount);

            RecycleInvisibleItems(start, end);
            
            if (start == lastStartIndex && end == lastEndIndex) return;
            lastStartIndex = start;
            lastEndIndex = end;

            for (int k = start; k <= end; k++) {
                if (k >= Count) continue;
                if (!activeItems.ContainsKey(k)) {
                    CreateCell(k);
                }
            }
        }


        private void _OnScrollValueChanged(Vector2 pos) {
            UpdateVisibleItems();
        }

        protected override void CreateCell(int index) {
            var cell = itemPool.Get();
            var rect = cell.GetComponent<RectTransform>();
            cell.Bind(dataList[index]);
            cell.gameObject.SetActive(true);
            rect.SetParent(content, false);
            rect.sizeDelta = new Vector2(content.sizeDelta.x, itemHeight);
            rect.anchoredPosition = new Vector2(0, -topPadding - index * (itemHeight + spacing));
            activeItems[index] = cell;
            OnCellCreated(cell, index, dataList[index]);
        }
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH 2026.03.10
 *
 * 주요 기능 ::
 * 1. Vertical ScrollRect 전용 RecycleView 동작을 제공합니다.
 * 2. TotalContentSize를 통해 전체 컨텐츠 세로 길이를 계산합니다.
 * 3. ScrollToIndex로 특정 인덱스 위치로 즉시 이동할 수 있습니다.
 * 4. viewport 범위를 기준으로 현재 표시해야 할 셀 범위를 계산합니다.
 * 5. 가시 범위를 벗어난 셀은 회수하고, 필요한 셀만 새로 생성합니다.
 * 6. 전체 컨텐츠가 viewport보다 작을 경우 모든 셀을 생성하여 표시합니다.
 *
 * 사용법 ::
 * 1. itemHeight, spacing, topPadding, bottomPadding을 Inspector에서 설정합니다.
 * 2. BaseRecycleView를 통해 데이터가 세팅되면 UpdateVisibleItems가 셀 표시 범위를 갱신합니다.
 * 3. 스크롤 이동 시 ScrollRect.onValueChanged를 통해 자동으로 가시 셀이 갱신됩니다.
 * 4. 특정 셀 위치로 이동이 필요할 경우 ScrollToIndex(index, center)를 호출합니다.
 *
 * 기타 ::
 * 1. ScrollToIndex의 center=true인 경우 해당 셀이 viewport 중앙에 가깝도록 이동합니다.
 * 2. end 계산은 start + VisibleCount 기준이라, 구현 의도상 한 칸의 여유 셀이 포함될 수 있습니다.
 * 3. CreateCell은 content 하위에 셀을 배치하고 anchoredPosition으로 직접 좌표를 설정합니다.
 * =========================================================
 */
#endif
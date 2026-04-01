#if UNITY_EDITOR
/* =========================================================
 * 이 스크립트는 Span 정보를 가지는 셀 데이터를 Grid 형태로 재배치하여 표시하는 RecycleView 스크립트입니다.
 * 셀마다 서로 다른 SpanX, SpanY를 가질 수 있으며, primary/secondary 축 기준으로 배치 정보를 계산하여
 * 현재 가시 범위의 셀만 생성 및 유지합니다.
 *
 * 주의사항 ::
 * 1. 이 스크립트는 IGridSpanData를 구현한 데이터에 대해서만 Span 정보를 사용하며, 미구현 데이터는 1x1 셀로 처리합니다.
 * 2. secondary 라인 점유 계산은 비트 마스크 기반이므로 최대 32라인까지만 지원합니다.
 * 3. scrollRect, viewport, content, itemPrefab은 반드시 유효하게 연결되어 있어야 합니다.
 * 4. ScrollToIndex, 가시 범위 계산, 컨텐츠 길이 계산은 내부 LayoutInfo 캐시가 정상적으로 빌드되어야만 올바르게 동작합니다.
 * 5. 셀 크기(cellSize), 간격(spacing), 패딩(startPadding/endPadding), 방향(isHorizontal)이 실제 UI 구성과 일치해야 합니다.
 * ********** 중요 **********
 *  6. 사용되는 Item UI와 Context 모두 좌상 Anchore와 Pivot (0, 1)로 사용하셔야 합니다. 
 * =========================================================
 */
#endif


using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using HUtil.Inspector;

namespace HUI.ScrollView {
    [Serializable]
    public class SpanningGridRecycleView<TCellView, TCellData> :
        BaseRecycleView<TCellView, TCellData>, IRecycleView
        where TCellData : class
        where TCellView : BaseRecycleCellView<TCellData> {
        #region Nested Class
        struct LayoutInfo {
            public int Primary;
            public int Secondary;
            public int PrimarySpan;
            public int SecondarySpan;

            public Vector2 SizePx;
            public Vector2 AnchoredPos;

            public float StartPrimaryPx;
            public float EndPrimaryPx;
        }
        #endregion

        [HTitle("Grid Settings")]
        [SerializeField]
        bool isHorizontal = true;
        [SerializeField]
        Vector2 spacing = new Vector2(10f, 10f);
        [SerializeField]
        Vector2 cellSize = new Vector2(100f, 100f);

        [HTitle("Grid Line Control")]
        [SerializeField]
        bool useFixedCount = true;
        [SerializeField, HShowIf("useFixedCount")]
        int fixedCount = 5;

        [HTitle("Padding (Primary Axis)")]
        [SerializeField]
        float startPadding = 0f;
        [SerializeField]
        float endPadding = 0f;

        int rowCount;
        int columnCount;
        int totalPrimaryUnits;

        bool layoutDirty = true;
        LayoutInfo[] layout;
        float[] startPrimaryPxList;
        float[] endPrimaryPxList;

        float _PrimarySize => isHorizontal ? cellSize.x : cellSize.y;
        float _PrimarySpacing => isHorizontal ? spacing.x : spacing.y;
        float _SecondarySize => isHorizontal ? cellSize.y : cellSize.x;
        float _SecondarySpacing => isHorizontal ? spacing.y : spacing.x;
        int _SecondaryCount => isHorizontal ? rowCount : columnCount;

        float _PrimaryItemSpace => _PrimarySize + _PrimarySpacing;
        float _SecondaryItemSpace => _SecondarySize + _SecondarySpacing;

        public float TotalContentSize {
            get {
                int units = Mathf.Max(0, totalPrimaryUnits);
                if (units <= 0)
                    return startPadding + endPadding;

                float itemsLength = units * _PrimarySize + (units - 1) * _PrimarySpacing;
                return startPadding + itemsLength + endPadding;
            }
        }

        protected override void Awake() {
            base.Awake();

            Assert.IsTrue(scrollRect != null, "[SpanningGridRecycleView] scrollRect is null.");
            Assert.IsTrue(viewport != null, "[SpanningGridRecycleView] viewport is null.");
            Assert.IsTrue(content != null, "[SpanningGridRecycleView] content is null.");
            Assert.IsTrue(itemPrefab != null, "[SpanningGridRecycleView] itemPrefab is null.");

            scrollRect.vertical = !isHorizontal;
            scrollRect.horizontal = isHorizontal;
            scrollRect.onValueChanged.AddListener(_OnScrollValueChanged);
        }

        protected override void OnRectTransformDimensionsChange() {
            base.OnRectTransformDimensionsChange();
            layoutDirty = true;
        }

        public override void SetData(
            IEnumerable<TCellData> data,
            int initSize = 0,
            Action<TCellView> onCreate = null, Action<TCellView> onGet = null,
            Action<TCellView> onReturn = null, Action<TCellView> onDispose = null) {
            base.SetData(data, initSize, onCreate, onGet, onReturn, onDispose);
            layoutDirty = true;
            UpdateVisibleItems();
        }

        public override void ScrollToIndex(int index, bool center = true) {
            if (dataList == null || Count == 0 || index < 0 || index >= Count)
                return;

            _EnsureLayoutBuilt();

            float viewportPrimary = isHorizontal ? viewport.rect.width : viewport.rect.height;

            float targetStart = startPrimaryPxList[index];
            float targetEnd = endPrimaryPxList[index];
            float targetCenter = (targetStart + targetEnd) * 0.5f;

            float desired = center ? (targetCenter - viewportPrimary * 0.5f) : targetStart;
            float maxScroll = Mathf.Max(0f, TotalContentSize - viewportPrimary);
            float scrollTarget = Mathf.Clamp(desired, 0f, maxScroll);

            var pos = content.anchoredPosition;
            if (isHorizontal) pos.x = scrollTarget;
            else pos.y = scrollTarget;

            content.anchoredPosition = pos;
            UpdateVisibleItems();
        }

        protected override void UpdateVisibleCount() {
            float viewportPrimary = isHorizontal ? viewport.rect.width : viewport.rect.height;
            float viewportSecondary = isHorizontal ? viewport.rect.height : viewport.rect.width;

            int visiblePrimary = Mathf.CeilToInt(viewportPrimary / _PrimaryItemSpace) + 2; // buffer
            int visibleSecondary = useFixedCount
                ? Mathf.Max(1, fixedCount)
                : Mathf.Max(1, Mathf.FloorToInt(viewportSecondary / _SecondaryItemSpace));

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
            _EnsureLayoutBuilt();

            float contentLength = TotalContentSize;
            var size = content.sizeDelta;

            content.sizeDelta = isHorizontal
                ? new Vector2(Mathf.Max(contentLength, viewport.rect.width), size.y)
                : new Vector2(size.x, Mathf.Max(contentLength, viewport.rect.height));
        }

        protected override void UpdateVisibleItems() {
            if (Count == 0) return;

            UpdateVisibleCount();
            _EnsureLayoutBuilt();
            UpdateContentSize();

            float scrollPos = isHorizontal ? content.anchoredPosition.x : content.anchoredPosition.y;
            scrollPos = Mathf.Abs(scrollPos);

            float viewportPrimary = isHorizontal ? viewport.rect.width : viewport.rect.height;
            float buffer = _PrimaryItemSpace * 1.5f;

            float minPos = Mathf.Max(0f, scrollPos - buffer);
            float maxPos = scrollPos + viewportPrimary + buffer;

            int startIndex = _LowerBoundEndPrimary(minPos);
            int endIndex = _UpperBoundStartPrimary(maxPos);

            startIndex = Mathf.Clamp(startIndex, 0, Count - 1);
            endIndex = Mathf.Clamp(endIndex, 0, Count - 1);

            if (startIndex > endIndex) {
                RecycleInvisibleItems(0, -1);
                return;
            }

            RecycleInvisibleItems(startIndex, endIndex);

            if (startIndex == lastStartIndex && endIndex == lastEndIndex) return;
            lastStartIndex = startIndex;
            lastEndIndex = endIndex;

            for (int k = startIndex; k <= endIndex; k++) {
                if (!activeItems.ContainsKey(k)) CreateCell(k);
            }
        }


        private void _OnScrollValueChanged(Vector2 _) {
            UpdateVisibleItems();
        }

        protected override void CreateCell(int index) {
            var cell = itemPool.Get();
            var rect = cell.GetComponent<RectTransform>();
            var data = dataList[index];

            var info = layout[index];

            cell.Bind(data);
            cell.gameObject.SetActive(true);
            rect.SetParent(content, false);

            rect.sizeDelta = info.SizePx;
            rect.anchoredPosition = info.AnchoredPos;

            activeItems[index] = cell;
            OnCellCreated(cell, index, data);
        }

        private void _EnsureLayoutBuilt() {
            if (!layoutDirty && layout != null && layout.Length == Count) return;
            if (Count <= 0) return;

            if (_SecondaryCount <= 0) {
                UpdateVisibleCount();
                if (_SecondaryCount <= 0) _SecondaryCount.ToString();
            }

            int lines = Mathf.Max(1, _SecondaryCount);

            layout = new LayoutInfo[Count];
            startPrimaryPxList = new float[Count];
            endPrimaryPxList = new float[Count];

            // primary 단위별로 secondary 점유 마스크(최대 32 라인 지원)
            Assert.IsFalse(lines > 32, "[SpanningGridRecycleView] secondaryCount > 32 is not supported in mask mode.");

            var primaryMasks = new List<int>(64);
            totalPrimaryUnits = 0;

            for (int k = 0; k < Count; k++) {
                var (spanX, spanY) = _GetSpan(dataList[k]);

                // 스팬을 방향성(primary/secondary)으로 변환
                int primarySpan = isHorizontal ? spanX : spanY;
                int secondarySpan = isHorizontal ? spanY : spanX;

                primarySpan = Mathf.Max(1, primarySpan);
                secondarySpan = Mathf.Clamp(secondarySpan, 1, lines);

                var pos = _FindPlacement(primaryMasks, lines, primarySpan, secondarySpan, out int primary, out int secondary);

                var sizePx = _CalcSizePx(spanX, spanY);
                var anchoredPos = _CalcAnchoredPos(primary, secondary);

                float startPrimaryPx = startPadding + primary * _PrimaryItemSpace;
                float endPrimaryPx = startPrimaryPx + primarySpan * _PrimarySize + (primarySpan - 1) * _PrimarySpacing;

                layout[k] = new LayoutInfo {
                    Primary = primary,
                    Secondary = secondary,
                    PrimarySpan = primarySpan,
                    SecondarySpan = secondarySpan,
                    SizePx = sizePx,
                    AnchoredPos = anchoredPos,
                    StartPrimaryPx = startPrimaryPx,
                    EndPrimaryPx = endPrimaryPx
                };

                startPrimaryPxList[k] = startPrimaryPx;
                endPrimaryPxList[k] = endPrimaryPx;

                totalPrimaryUnits = Mathf.Max(totalPrimaryUnits, primary + primarySpan);
            }

            layoutDirty = false;
        }

        private (int spanX, int spanY) _GetSpan(TCellData data) {
            if (data is IGridSpanData span) return (span.SpanX, span.SpanY);
            return (1, 1);
        }

        private Vector2 _CalcSizePx(int spanX, int spanY) {
            float w = cellSize.x * spanX + spacing.x * (spanX - 1);
            float h = cellSize.y * spanY + spacing.y * (spanY - 1);
            return new Vector2(w, h);
        }

        private Vector2 _CalcAnchoredPos(int primary, int secondary) {
            // isHorizontal:
            //  x: primary 축 (오른쪽 +)
            //  y: secondary 축 (아래 -)
            //
            // !isHorizontal:
            //  x: secondary 축 (오른쪽 +)
            //  y: primary 축 (아래 -)
            if (isHorizontal) {
                return new Vector2(
                    startPadding + primary * (cellSize.x + spacing.x),
                    -secondary * (cellSize.y + spacing.y)
                );
            }

            return new Vector2(
                secondary * (cellSize.x + spacing.x),
                -(startPadding + primary * (cellSize.y + spacing.y))
            );
        }

        private bool _FindPlacement(List<int> primaryMasks, int lines, int primarySpan, int secondarySpan, out int primary, out int secondary) {
            int maxSecondaryStart = lines - secondarySpan;
            int spanMaskBase = (1 << secondarySpan) - 1;

            for (int p = 0; ; p++) {
                // 필요한 primary 칼럼이 없으면 확장
                while (primaryMasks.Count < p + primarySpan) {
                    primaryMasks.Add(0);
                }

                for (int k = 0; k <= maxSecondaryStart; k++) {
                    int mask = spanMaskBase << k;
                    bool canPlace = true;

                    for (int dp = 0; dp < primarySpan; dp++) {
                        if ((primaryMasks[p + dp] & mask) != 0) {
                            canPlace = false;
                            break;
                        }
                    }

                    if (!canPlace) continue;

                    for (int dp = 0; dp < primarySpan; dp++) {
                        primaryMasks[p + dp] |= mask;
                    }

                    primary = p;
                    secondary = k;
                    return true;
                }
            }
        }

        private int _LowerBoundEndPrimary(float value) {
            // endPrimaryPx >= value 인 첫 인덱스
            int lo = 0;
            int hi = Count;
            while (lo < hi) {
                int mid = (lo + hi) >> 1;
                if (endPrimaryPxList[mid] < value) lo = mid + 1;
                else hi = mid;
            }
            return lo;
        }

        private int _UpperBoundStartPrimary(float value) {
            // startPrimaryPx <= value 인 마지막 인덱스
            int lo = 0;
            int hi = Count;
            while (lo < hi) {
                int mid = (lo + hi) >> 1;
                if (startPrimaryPxList[mid] <= value) lo = mid + 1;
                else hi = mid;
            }
            return lo - 1;
        }
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 *
 * 주요 기능 ::
 * 1. SpanX, SpanY를 가지는 셀 데이터를 Grid 레이아웃으로 배치합니다.
 * 2. primary/secondary 축 개념을 사용하여 가로/세로 방향을 공통 로직으로 처리합니다.
 * 3. 각 셀의 SizePx, AnchoredPos, StartPrimaryPx, EndPrimaryPx를 사전 계산하여 레이아웃 캐시를 구성합니다.
 * 4. 가시 범위를 벗어난 셀은 회수하고, 현재 보이는 범위의 셀만 생성합니다.
 * 5. ScrollToIndex를 통해 특정 인덱스 셀 위치로 스크롤 이동할 수 있습니다.
 * 6. fixedCount 또는 viewport 크기를 기준으로 secondary 라인 수를 계산합니다.
 *
 * 사용법 ::
 * 1. TCellData가 IGridSpanData를 구현하면 SpanX, SpanY 값이 레이아웃 계산에 반영됩니다.
 * 2. isHorizontal로 스크롤 방향을 결정합니다.
 * 3. useFixedCount가 true면 fixedCount를 secondary 라인 수로 사용합니다.
 * 4. useFixedCount가 false면 viewport 크기를 기준으로 secondary 라인 수를 자동 계산합니다.
 * 5. SetData 호출 시 레이아웃을 Dirty 처리한 뒤 가시 셀을 다시 계산합니다.
 * 6. 스크롤 이동 또는 RectTransform 크기 변경 시 레이아웃과 가시 범위를 갱신합니다.
 *
 * 기타 ::
 * 1. LayoutInfo는 각 셀의 배치 결과를 캐싱하기 위한 내부 구조체입니다.
 * 2. _FindPlacement는 secondary 라인 점유 상태를 비트 마스크로 관리하며, 배치 가능한 첫 위치를 탐색합니다.
 * 3. _LowerBoundEndPrimary / _UpperBoundStartPrimary는 현재 스크롤 범위와 겹치는 셀 인덱스를 빠르게 찾기 위한 이진 탐색 함수입니다.
 * 4. _EnsureLayoutBuilt 내부의 `var pos = _FindPlacement(...)` 반환값은 실제로 사용되지 않으므로 제거 대상입니다.
 * 5. `_SecondaryCount <= 0`일 때 `_SecondaryCount.ToString();`을 호출하는 코드는 의미 없는 잔여 코드입니다.
 * 6. ScrollToIndex와 UpdateVisibleItems는 anchoredPosition 부호 기준이 실제 ScrollRect 설정과 일치해야 정상 동작합니다.
 * =========================================================
 */
#endif

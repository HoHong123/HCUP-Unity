#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Span 정보 (SpanX/SpanY) 를 가지는 셀 데이터를 Grid 형태로 재배치하는 RecycleView 변종.
 * 셀마다 서로 다른 점유 영역을 가질 수 있으며, primary/secondary 축 기준으로 배치 정보를
 * 사전 계산 (LayoutInfo) 후 가시 범위 셀만 생성/유지.
 *
 * 주요 기능 ::
 * Span 셀 자동 배치 (가변 크기 셀이 그리드 빈 공간에 패킹) + primary/secondary 축 추상화로
 * 가로/세로 방향 공통 로직 + LayoutInfo 사전 계산 캐시 + 비트 마스크 기반 라인 점유 추적 +
 * 이진 탐색 기반 가시 범위 계산.
 *
 * 사용법 ::
 * 1. TCellData 가 IGridSpanData 를 구현하면 SpanX/SpanY 값이 레이아웃 계산에 반영.
 *    미구현 데이터는 1x1 셀로 자동 처리.
 * 2. isHorizontal 로 스크롤 방향 결정 (가로/세로).
 * 3. useFixedCount=true 면 fixedCount 를 secondary 라인 수로 사용. false 면 viewport 크기
 *    기준으로 자동 계산.
 * 4. SetData 호출 시 레이아웃 Dirty 처리 후 가시 셀 재계산.
 *
 * 주의 ::
 * 1. IGridSpanData 미구현 데이터는 1x1 셀로 처리.
 * 2. secondary 라인 점유 계산이 비트 마스크 기반 — 최대 32 라인까지만 지원.
 * 3. scrollRect / viewport / content / itemPrefab 모두 유효 연결 필수.
 * 4. ScrollToIndex / 가시 범위 / 컨텐츠 길이 계산은 LayoutInfo 캐시가 정상 빌드되어야만 작동.
 * 5. cellSize / spacing / startPadding / endPadding / isHorizontal 가 실제 UI 구성과 일치해야 함.
 * 6. Item UI 와 Context 모두 좌상 Anchor + Pivot (0, 1) 필수 (전제 조건).
 *
 * 내부 구조 ::
 * LayoutInfo (셀별 Primary/Secondary/Span/SizePx/AnchoredPos 캐시) +
 * startPrimaryPxList / endPrimaryPxList (이진 탐색용 정렬 배열) +
 * _FindPlacement (비트 마스크로 secondary 라인 점유 추적, 첫 빈 위치 반환) +
 * _LowerBoundEndPrimary / _UpperBoundStartPrimary (가시 범위 이진 탐색).
 * =========================================================
 */
#endif


using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using HInspector;

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
 * Dev Log
 * =========================================================
 *
 * =========================================================
 * 2026-04-26 (수정) :: 헤더 형틀 통합 + Dev Log 형식 도입
 * =========================================================
 * 변경 ::
 * 기존 헤더 (상단 도입+주의사항 + 하단 주요기능/사용법/기타) 를 한 곳에 통합하여 §11 형틀
 * 통일. 하단 Dev Log 영역 추가. 헤더와 Dev Log 모두 #if UNITY_EDITOR 가드.
 * 헤더의 "주의" 6 항목 + "내부 구조" 섹션 모두 통합 헤더에 흡수.
 *
 * 이유 ::
 * 글로벌 CLAUDE.md §11 룰 일괄 적용. SpanningGridRecycleView 가 RecycleView 시스템의 가장
 * 복잡한 변종이라 헤더에서 의도/주의/내부 구조를 한 화면에 모두 노출.
 *
 * =========================================================
 * 2026-04-25 (최초 설계, @Jason - PKH) :: SpanningGridRecycleView 초기 구현
 * =========================================================
 * RecycleView 시스템에서 가장 복잡한 변종. 핵심 결정 4 가지:
 *
 * (1) primary/secondary 축 추상화 ::
 * 가로/세로 두 방향을 공통 로직으로 처리. isHorizontal 플래그 한 개로 layout 코드의 두 갈래를
 * 통합. primary = 스크롤 방향, secondary = 스크롤 직교 방향.
 *
 * (2) LayoutInfo 사전 계산 캐시 ::
 * 각 셀의 (Primary / Secondary / PrimarySpan / SecondarySpan / SizePx / AnchoredPos +
 * startPrimaryPx / endPrimaryPx) 를 SetData 시 일괄 계산하여 메모리에 보유. 매 프레임
 * 가시 범위 계산이 이 캐시 위에서 이진 탐색만 수행 — O(log n).
 *
 * (3) 비트 마스크 기반 라인 점유 추적 ::
 * _FindPlacement 가 secondary 라인의 빈 위치를 비트 마스크로 추적. SpanX 칸을 점유 가능한
 * 첫 위치를 한 번의 비트 연산으로 탐색. 32 라인 한계는 int 비트 폭에서 옴 — 더 많은 라인이
 * 필요하면 long 또는 BitArray 로 확장 가능.
 *
 * (4) 이진 탐색 기반 가시 범위 ::
 * _LowerBoundEndPrimary / _UpperBoundStartPrimary 가 정렬된 startPrimaryPxList /
 * endPrimaryPxList 위에서 가시 범위 시작/끝 인덱스를 O(log n) 으로 찾음. 1만 개 셀에서도
 * 가시 범위 계산이 ~14 비교만에 끝남.
 *
 * 알려진 정리 후보 ::
 * - _EnsureLayoutBuilt 내부의 `var pos = _FindPlacement(...)` 반환값 미사용 → 제거 가능.
 * - `_SecondaryCount <= 0` 분기의 `_SecondaryCount.ToString();` 잔여 코드.
 *
 * 전제 조건 ::
 * Item UI / Context 모두 좌상 Anchor + Pivot (0, 1). ScrollRect 의 anchoredPosition 부호
 * 기준이 본 클래스의 layout 계산과 일치해야 함.
 * =========================================================
 */
#endif

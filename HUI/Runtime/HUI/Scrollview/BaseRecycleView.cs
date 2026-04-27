#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Recycle ScrollView 시스템의 베이스 추상 클래스. 대량 리스트 데이터를 Cell 재사용 기반으로
 * 효율적 표시 — 가시 영역만 활성화하여 메모리/렌더링 비용을 데이터 크기와 분리.
 *
 * 특징 ::
 * Generic 기반 CellView / CellData 구조 + ComponentPool 기반 UI Cell 재사용 + Visible 영역
 * 기준 동적 Cell 생성/회수 + ScrollRect 기반 가상 리스트 (Virtual List) 구현.
 *
 * 주요 기능 ::
 * SetData(...) — 데이터 설정 + Cell Pool 초기화 + 가시 영역 갱신.
 * ScrollTo(normalizedY) / ScrollToItem(target) / ScrollToIndex(index) — 위치 이동 3 갈래.
 * RecycleInvisibleItems(start, end) — 가시 범위 밖 Cell 풀 반환.
 * 추상 메서드 (UpdateVisibleCount / UpdateContentSize / UpdateVisibleItems / CreateCell /
 * ScrollToIndex) — 변종이 layout 정책에 맞게 구현 (Template Method).
 *
 * 사용법 ::
 *   class RankScrollView : BaseRecycleView<RankItemUI, RankData> {
 *       protected override void UpdateVisibleCount() { ... }
 *       protected override void UpdateContentSize() { ... }
 *       protected override void UpdateVisibleItems() { ... }
 *       protected override void CreateCell(int index) { ... }
 *       public override void ScrollToIndex(int index, bool center) { ... }
 *   }
 *
 * 내부 구조 ::
 * dataList (전체 데이터) / activeItems (현재 가시 셀) / recycleKeys (재활용 대상) /
 * itemPool (Cell 풀) / lastStartIndex / lastEndIndex (이전 가시 영역).
 *
 * 주의사항 ::
 * Cell 은 풀에서 재사용되므로 BaseRecycleCellView.Bind 시 모든 UI 상태를 반드시 초기화.
 * 직전 셀의 잔존 상태가 시각적 버그로 이어지는 함정 회피.
 * =========================================================
 */
#endif

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HCore;
using HInspector;
using HDiagnosis.Logger;
using HUtil.Pooling;

namespace HUI.ScrollView {
    [Serializable]
    [RequireComponent(typeof(ScrollRect))]
    public abstract class BaseRecycleView<TCellView, TCellData> :
#if ODIN_INSPECTOR
        Sirenix.OdinInspector.SerializedMonoBehaviour
#else
        MonoBehaviour
#endif
        where TCellData : class
        where TCellView : BaseRecycleCellView<TCellData> {
        #region Fields
        [HTitle("Require")]
        [SerializeField]
        protected ScrollRect scrollRect;
        [SerializeField]
        protected RectTransform viewport;
        [SerializeField]
        protected RectTransform content;

        [HTitle("Item Prefab")]
        [SerializeField]
        protected TCellView itemPrefab;

        [SerializeField]
        protected List<TCellData> dataList = new();

        protected ComponentPool<TCellView> itemPool;
        protected readonly List<int> recycleKeys = new();
        protected readonly Dictionary<int, TCellView> activeItems = new();

        protected int lastStartIndex = -1;
        protected int lastEndIndex = -1;

        bool isInitialized = false;
        #endregion

        #region Properties
        public int VisibleCount { get; protected set; } = 0;
        public int Count => dataList.Count;
        public IReadOnlyList<TCellData> Datas => dataList ?? new();
        #endregion

        #region Unity Callbacks
        protected virtual void Awake() {
            if (scrollRect == null) scrollRect = GetComponent<ScrollRect>();
        }

        protected virtual void OnRectTransformDimensionsChange() {
            if (!isInitialized && viewport.rect.height > 0f) {
                isInitialized = true;
                InitializeScrollView();
            }
        }
        #endregion

        #region Init
        protected virtual void InitializeScrollView() {
            if (dataList != null) {
                SetData(dataList);
            }
        }
        #endregion

        #region Public - Item Control
        public void DestroyAll() {
            itemPool?.Dispose();
            content?.DestroyAllChildren();
        }
        #endregion

        #region Public - Data Control
        public virtual void SetData(
            IEnumerable<TCellData> data,
            int initSize = 0,
            Action<TCellView> onCreate = null, Action<TCellView> onGet = null,
            Action<TCellView> onReturn = null, Action<TCellView> onDispose = null) {
            if (data == null) {
                HLogger.Error("[BaseRecycleView] Data param is null.");
                return;
            }

            dataList = data.ToList();

            if (itemPool == null) {
                onCreate += (item) => { item.gameObject.SetActive(false); };
                onGet += (item) => { item.gameObject.SetActive(true); };
                onReturn += (item) => { item.gameObject.SetActive(false); };
                itemPool = new(
                    itemPrefab, initSize, content,
                    onCreate, onGet, onReturn, onDispose
                );
            }
            else {
                foreach (var kvp in activeItems) {
                    itemPool.Return(kvp.Value);
                }
            }

            activeItems.Clear();
            recycleKeys.Clear();

            lastStartIndex = -1;
            lastEndIndex = -1;

            UpdateVisibleCount();
            UpdateContentSize();

            if (Count == 0) return;

            UpdateVisibleItems();
        }
        #endregion

        #region Public - Scroll Control
        public virtual void ScrollTo(float normalizedY) {
            normalizedY = Mathf.Clamp01(normalizedY);
            scrollRect.verticalNormalizedPosition = normalizedY;

            UpdateVisibleItems();
        }

        public virtual void ScrollToItem(TCellData target, bool center = true) {
            int index = dataList.IndexOf(target);
            if (index > -1) ScrollToIndex(index, center);
        }


        protected virtual void RecycleInvisibleItems(int start, int end) {
            recycleKeys.Clear();

            foreach (var kvp in activeItems) {
                if (kvp.Key < start || kvp.Key > end) {
                    itemPool.Return(kvp.Value);
                    recycleKeys.Add(kvp.Key);
                }
            }

            foreach (var key in recycleKeys) {
                activeItems.Remove(key);
            }
        }
        #endregion

        #region Protected - Cell Control
        protected virtual void OnCellCreated(TCellView cell, int index, TCellData data) { }

        public abstract void ScrollToIndex(int index, bool center = true);
        protected abstract void UpdateVisibleCount();
        protected abstract void UpdateContentSize();
        protected abstract void UpdateVisibleItems();
        protected abstract void CreateCell(int index);
        #endregion
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
 * 기존 헤더 (상단 도입+주의사항 + 하단 특징/구조/주요기능/내부구조/사용법/기타) 를 한 곳에
 * 통합하여 §11 형틀 통일. 하단 Dev Log 영역 추가. 헤더와 Dev Log 모두 #if UNITY_EDITOR 가드.
 *
 * 이유 ::
 * 글로벌 CLAUDE.md §11 룰 일괄 적용.
 *
 * =========================================================
 * 2026-04-25 (최초 설계, @Jason - PKH 2026.03.10) :: BaseRecycleView 초기 구현
 * =========================================================
 * Unity ScrollRect 기반 가상 리스트 (Virtualized List) 구현을 위한 베이스. 핵심 통찰은
 * "데이터 크기와 활성 셀 수의 분리" — 1만 개 데이터를 가져도 화면에 보이는 ~10 개 셀만
 * 활성화. 메모리/렌더링 비용이 데이터 크기에 비례하지 않음.
 *
 * 자료구조 ::
 * - dataList: 전체 데이터 (모든 도메인 모델 보유)
 * - activeItems: 현재 가시 셀의 [index → CellView] 매핑
 * - recycleKeys: 한 프레임 내 재활용 대상 index 임시 list (heap allocation 회피)
 * - itemPool: ComponentPool<TCellView> 풀 (Object Pool 패턴)
 * - lastStartIndex / lastEndIndex: 이전 가시 영역 - 변경 감지용
 *
 * Template Method 5 갈래 ::
 * 변종 (Vertical / Horizontal / Grid / Vlg / SpanningGrid) 이 layout 정책에 맞게 구현:
 * UpdateVisibleCount / UpdateContentSize / UpdateVisibleItems / CreateCell / ScrollToIndex.
 *
 * 가상 리스트 패턴이 핵심 가치 — 게임 UI (랭킹 / 인벤토리 / 상점 / 채팅 로그 등) 에서
 * 1k+ 항목을 부드러운 60fps 로 스크롤 가능하게 만드는 표준 기법.
 * =========================================================
 */
#endif

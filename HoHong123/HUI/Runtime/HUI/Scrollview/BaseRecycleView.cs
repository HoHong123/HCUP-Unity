#if UNITY_EDITOR
/* =========================================================
 * Recycle ScrollView 시스템의 베이스 클래스입니다.
 * 대량의 리스트 데이터를 효율적으로 표시하기 위해 Cell 재사용(Recycling) 기반 ScrollView 구조를 제공합니다.
 * Scroll 위치를 기준으로 Visible Index를 계산하고 필요한 Cell만 생성 및 재사용하여 성능을 최적화합니다.
 *
 * 주의사항 ::
 * Cell은 재사용되므로 Bind 시 UI 상태를 반드시 초기화해야 합니다.
 * =========================================================
 */
#endif

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HUtil.Core;
using HUtil.Inspector;
using HUtil.Logger;
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
 * @Jason - PKH 2026.03.10
 *
 * 특징 ::
 * 1. Generic 기반 CellView / CellData 구조
 * 2. ComponentPool 기반 UI Cell 재사용
 * 3. Visible 영역 기준 동적 Cell 생성
 * 4. ScrollRect 기반 가상 리스트(Virtual List) 구현
 *
 * 구조 ::
 * BaseRecycleView<TCellView, TCellData>
 *     └ ScrollView 구현 클래스
 *            └ Item Cell View
 *
 * 주요 기능 ::
 * 1. 데이터 설정
 * SetData()
 *  + ScrollView 데이터 설정
 *  + Cell Pool 초기화
 * 2. Scroll 위치 이동
 * ScrollTo()
 *  + normalizedPosition 기반 이동
 * ScrollToItem()
 *  + 특정 데이터 위치로 이동
 * ScrollToIndex()
 *  + 특정 Index 위치로 이동
 * 3. Cell 재사용
 * ComponentPool 기반 UI Cell 재활용
 * 4. Visible Cell 관리
 * Visible 영역 기준 Cell 생성 및 제거
 * 5. ScrollView 초기화
 * InitializeScrollView()
 *
 * 내부 구조 ::
 * dataList
 *  + ScrollView 데이터 리스트
 * activeItems
 *  + 현재 화면에 표시중인 Cell
 * recycleKeys
 *  + 재활용 대상 Cell Index
 * itemPool
 *  + Cell Pool
 * lastStartIndex
 * lastEndIndex
 *  + 현재 Visible 영역 Index
 *
 * 사용법 ::
 * class RankScrollView : BaseRecycleView<RankItemUI, RankData> {
 *     protected override void UpdateVisibleCount() { }
 *     protected override void UpdateContentSize() { }
 *     protected override void UpdateVisibleItems() { }
 *     protected override void CreateCell(int index) { }
 *     public override void ScrollToIndex(int index, bool center) { }
 * }
 *
 * 기타 ::
 * Unity ScrollRect 기반 가상 리스트(Virtualized List) 구현을 위한 베이스 클래스입니다.
 * =========================================================
 */
#endif

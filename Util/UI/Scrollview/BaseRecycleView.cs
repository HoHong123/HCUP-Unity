using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using Util.Pooling;

namespace Util.UI.ScrollView {
    [Serializable]
    [RequireComponent(typeof(ScrollRect))]
    public abstract class BaseRecycleView<TCellView, TCellData> : MonoBehaviour
        where TCellData : BaseRecycleCellData
        where TCellView : BaseRecycleCellView<TCellData> {
        [Title("Require")]
        [SerializeField]
        protected ScrollRect scrollRect;
        [SerializeField]
        protected RectTransform viewport;
        [SerializeField]
        protected RectTransform content;

        [Title("Item Prefab")]
        [SerializeField]
        protected TCellView itemPrefab;

        protected List<TCellData> dataList = new();
        protected ComponentPool<TCellView> itemPool;
        protected readonly List<int> recycleKeys = new();
        protected readonly Dictionary<int, TCellView> activeItems = new();

        protected int lastStartIndex = -1;
        protected int lastEndIndex = -1;

        bool isInitialized = false;

        public int VisibleCount { get; protected set; } = 0;
        public int Count => dataList.Count;


        protected virtual void Awake() {
            if (scrollRect == null) scrollRect = GetComponent<ScrollRect>();
        }

        protected virtual void OnRectTransformDimensionsChange() {
            if (!isInitialized && viewport.rect.height > 0f) {
                isInitialized = true;
                InitializeScrollView();
            }
        }

        protected virtual void InitializeScrollView() {
            if (dataList != null) {
                UpdateVisibleCount();
                SetData(dataList);
            }
        }


        public virtual void SetData(List<TCellData> data) {
            if (!isInitialized || data == null || data.Count == 0) return;
            
            dataList = data;
            recycleKeys.Clear();
            activeItems.Clear();

            UpdateContentSize();

            if (itemPool == null) {
                itemPool = new(itemPrefab, dataList.Count, content);
            }
            itemPool.Init(VisibleCount + 2);

            UpdateVisibleItems();
        }


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


        public abstract void ScrollToIndex(int index, bool center = true);
        protected abstract void UpdateVisibleCount();
        protected abstract void UpdateContentSize();
        protected abstract void UpdateVisibleItems();
    }
}
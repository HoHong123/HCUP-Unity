using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using Unity.VisualScripting;

namespace Util.UI.ScrollView {
    [Serializable]
    public class VlgRecycleView<TCellView, TCellData> : BaseRecycleView<TCellView, TCellData>, IRecycleView
        where TCellData : BaseRecycleCellData
        where TCellView : BaseRecycleCellView<TCellData> {
        [Title("Cell View")]
        [SerializeField]
        VerticalLayoutGroup layoutGroup;
        [SerializeField]
        float spacing;
        [SerializeField]
        float itemHeight;

        [Title("Cell Tube")]
        [SerializeField, ReadOnly]
        LayoutElement headerTube;
        [SerializeField, ReadOnly]
        LayoutElement footerTube;

        public float TotalContentSize => (itemHeight + spacing) * dataList.Count - spacing;


        protected override void Awake() {
            _InitTubesIfNeeded();
            if (layoutGroup != null) layoutGroup.spacing = spacing;
            
            base.Awake();
            scrollRect.vertical = true;
            scrollRect.horizontal = false;
            scrollRect.onValueChanged.AddListener(_OnScrollValueChanged);
        }


        public override void SetData(List<TCellData> data) {
            _InitTubesIfNeeded();
            base.SetData(data);
            ScrollToIndex(0, false);
        }

        public override void ScrollToIndex(int index, bool center = true) {
            if (dataList == null || Count == 0 || index < 0 || index >= Count) return;

            float itemSpace = itemHeight + spacing;
            float centerOffset = center ? (viewport.rect.height - itemSpace) / 2f : 0f;
            float targetY = index * itemSpace - centerOffset;
            float maxScrollY = Mathf.Max(0, TotalContentSize - viewport.rect.height);
            float normalizedY = Mathf.Clamp01(1f - (targetY / maxScrollY));

            scrollRect.verticalNormalizedPosition = normalizedY;
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
                for (int i = 0; i < Count; i++) {
                    if (!activeItems.ContainsKey(i)) {
                        _CreateCell(i);
                    }
                }

                _UpdateTubes(0f, 0f);
                return;
            }

            float scrollY = content.anchoredPosition.y;
            int start = Mathf.Max(0, Mathf.FloorToInt(scrollY / (itemHeight + spacing)));
            int end = start + VisibleCount - 1;
            start = Mathf.Clamp(start, 0, Mathf.Max(0, Count - 1));
            end = Mathf.Clamp(end, 0, Mathf.Max(0, Count - 1));

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

            float topHeight = start * (itemHeight + spacing);
            float bottomHeight = (Count - end - 1) * (itemHeight + spacing);
            _UpdateTubes(topHeight, bottomHeight);
        }


        private void _OnScrollValueChanged(Vector2 pos) {
            UpdateVisibleItems();
        }

        private void _InitTubesIfNeeded() {
            if (headerTube == null) {
                headerTube = _CreateTube("HeaderTube");
                headerTube.transform.SetParent(content, false);
                headerTube.transform.SetAsFirstSibling();
            }
            if (footerTube == null) {
                footerTube = _CreateTube("FooterTube");
                footerTube.transform.SetParent(content, false);
                footerTube.transform.SetAsLastSibling();
            }
        }

        private void _CreateCell(int index) {
            var cell = itemPool.Get();
            var layout = cell.GetComponent<LayoutElement>();
            if (layout == null) layout = cell.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = layout.preferredHeight = itemHeight;

            cell.Bind(dataList[index]);
            cell.gameObject.SetActive(true);
            cell.transform.SetParent(content, false);
            cell.transform.SetSiblingIndex(index + 1);
            activeItems[index] = cell;
        }

        private LayoutElement _CreateTube(string name) {
            var obj = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
            var layout = obj.GetComponent<LayoutElement>();
            layout.minHeight = layout.preferredHeight = 0;
            return layout;
        }

        private void _UpdateTubes(float headerHeight, float footerHeight) {
            headerTube.minHeight = headerTube.preferredHeight = headerHeight;
            footerTube.minHeight = footerTube.preferredHeight = footerHeight;
            headerTube.transform.SetAsFirstSibling();
            footerTube.transform.SetAsLastSibling();
        }
    }
}
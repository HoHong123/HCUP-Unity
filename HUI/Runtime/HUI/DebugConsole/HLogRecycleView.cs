using System;
using UnityEngine;
using HUI.ScrollView;

namespace HUI.DebugConsole {
    public sealed class HLogRecycleView : VerticalRecycleView<HLogCellView, HLogCellData> {
        public Action<HLogCellData> OnCellClicked { get; set; }
        public Action<bool> OnLatestFollowStateChanged { get; set; }

        bool isProgrammaticScroll;

        public bool IsAtLatest(float tolerance = 0.001f) {
            if (scrollRect == null) return true;
            if (Count <= VisibleCount) return true;
            return scrollRect.verticalNormalizedPosition <= tolerance;
        }

        public void ScrollToLatest() {
            if (scrollRect == null) return;
            isProgrammaticScroll = true;
            ScrollTo(0f);
            isProgrammaticScroll = false;
        }

        protected override void Awake() {
            base.Awake();
            scrollRect.onValueChanged.AddListener(_OnScrollChanged);
        }

        protected override void OnCellCreated(HLogCellView cell, int index, HLogCellData data) {
            cell.OnClick = OnCellClicked;
        }

        void _OnScrollChanged(Vector2 _) {
            if (isProgrammaticScroll) return;
            OnLatestFollowStateChanged?.Invoke(IsAtLatest());
        }
    }
}

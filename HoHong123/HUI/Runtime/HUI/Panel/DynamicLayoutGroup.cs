using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HUtil.UI.Panel {
    // On Progress
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("Layout/Proportional Vertical Layout Group")]
    public class DynamicLayoutGroup : LayoutGroup {
        public override void CalculateLayoutInputVertical() {
        }

        public override void SetLayoutHorizontal() {
        }

        public override void SetLayoutVertical() {
        }
    }
}
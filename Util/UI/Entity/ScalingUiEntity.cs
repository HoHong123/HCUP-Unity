using System;
using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;


namespace Util.UI.Entity {
    [Serializable]
    public class ScalingUiEntity {
        [Title("Target")]
        [SerializeField]
        [OnValueChanged(nameof(_Init))]
        Transform target;

        [Title("Option")]
        [SerializeField]
        bool useAnimation = false;
        [ShowIf(nameof(useAnimation)), SerializeField]
        float animationDuration = 0.2f;

        [Title("Scales")]
        [InfoBox("MUST consider the pivot relation with parent.")]
        public bool UseAbsoluteScale = false;
        [SerializeField]
        Vector2 originalScale;
        [ShowIf("UseAbsoluteScale")]
        [SerializeField]
        Vector2 absoluteScale = Vector2.zero;
        [HideIf("UseAbsoluteScale")]
        [SerializeField]
        float scaleFactor = 1f;



        private void _Init() {
            originalScale = target.localScale;
        }

        public void Reset() => _Scale(originalScale);
        public void ChangeScale() => _Scale(UseAbsoluteScale ? absoluteScale : target.localScale * scaleFactor);


        private void _Scale(Vector2 scale) {
            if (useAnimation) {
                target.DOKill();
                target.DOScale(scale, animationDuration);
            }
            else {
                target.localScale = scale;
            }
        }
    }
}
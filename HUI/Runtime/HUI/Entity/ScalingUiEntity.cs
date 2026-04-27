using System;
using UnityEngine;
using DG.Tweening;
using HInspector;

namespace HUI.Entity {
    [Serializable]
    public class ScalingUiEntity {
        [HTitle("Target")]
        [SerializeField]
        [HOnValueChanged(nameof(_Init))]
        Transform target;

        [HTitle("Option")]
        [SerializeField]
        bool useAnimation = false;
        [HShowIf(nameof(useAnimation)), SerializeField]
        float animationDuration = 0.2f;

        [HTitle("Scales")]
        [Tooltip("MUST consider the pivot relation with parent.")]
        public bool UseAbsoluteScale = false;
        [SerializeField]
        Vector2 originalScale;
        [HShowIf("UseAbsoluteScale")]
        [SerializeField]
        Vector2 absoluteScale = Vector2.zero;
        [HHideIf("UseAbsoluteScale")]
        [SerializeField]
        float scaleFactor = 1f;


        private void _Init() {
            originalScale = target.localScale;
        }


        public void Reset(bool immediate = false) => _ApplyScale(originalScale, immediate);
        public void Scale(bool immediate = false) => _ApplyScale(UseAbsoluteScale ? absoluteScale : originalScale * scaleFactor, immediate);


        private bool _CanAnimate() {
            if (!useAnimation) return false;
            if (target == null) return false;
            return target.gameObject.activeInHierarchy;
        }

        private void _ApplyScale(Vector3 scale, bool immediate = false) {
            target.DOKill();

            if (_CanAnimate()) {
                target.DOScale(scale, animationDuration).SetUpdate(true);
                return;
            }

            target.localScale = scale;
        }
    }
}

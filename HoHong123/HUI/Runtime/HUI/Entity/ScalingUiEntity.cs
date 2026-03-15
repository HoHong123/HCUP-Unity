using System;
using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;

namespace HUI.Entity {
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


        public void Reset(bool immediate = false) => _ApplyScale(originalScale, immediate);
        public void Scale(bool immediate = false) => _ApplyScale(UseAbsoluteScale ? absoluteScale : target.localScale * scaleFactor, immediate);


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
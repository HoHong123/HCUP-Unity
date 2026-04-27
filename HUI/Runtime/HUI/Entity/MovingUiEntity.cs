using System;
using UnityEngine;
using DG.Tweening;
using HInspector;

namespace HUI.Entity {
    [Serializable]
    public class MovingUiEntity {
        [HTitle("Target")]
        [SerializeField]
        [HOnValueChanged(nameof(_Init))]
        Transform target;

        [HTitle("Option")]
        [SerializeField]
        bool useAnimation = false;
        [HShowIf(nameof(useAnimation)), SerializeField]
        float animationDuration = 0.2f;

        [HTitle("Positions")]
        [Tooltip("MUST consider the pivot relation with parent.")]
        public bool UseAbsolutePosition = false;
        [SerializeField]
        Vector3 originPosition;
        [HShowIf("UseAbsolutePosition")]
        [SerializeField]
        Vector3 absolutePosition = Vector3.zero;
        [HHideIf("UseAbsolutePosition")]
        [SerializeField]
        Vector3 moveAmount = Vector3.zero;


        private void _Init() {
            originPosition = target.localPosition;
        }


        public void Reset(bool immediate = false) => _ApplyMove(originPosition, immediate);
        public void Move(bool immediate = false) => _ApplyMove(UseAbsolutePosition ? absolutePosition : (originPosition + moveAmount), immediate);


        private bool _CanAnimate() {
            if (!useAnimation) return false;
            if (target == null) return false;
            return target.gameObject.activeInHierarchy;
        }

        private void _ApplyMove(Vector3 pos, bool immediate = false) {
            target.DOKill();

            if (_CanAnimate() && !immediate) {
                target.DOLocalMove(pos, animationDuration).SetUpdate(true);
                return;
            }

            target.localPosition = pos;
        }
    }
}

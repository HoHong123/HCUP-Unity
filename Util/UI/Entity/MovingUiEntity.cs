using System;
using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;


namespace Util.UI.Entity {
    [Serializable]
    public class MovingUiEntity {
        [Title("Target")]
        [SerializeField]
        [OnValueChanged(nameof(_Init))]
        Transform target;

        [Title("Option")]
        [SerializeField]
        bool useAnimation = false;
        [ShowIf(nameof(useAnimation)), SerializeField]
        float animationDuration = 0.2f;

        [Title("Positions")]
        [InfoBox("MUST consider the pivot relation with parent.")]
        public bool UseAbsolutePosition = false;
        [SerializeField]
        Vector3 originPosition;
        [ShowIf("UseAbsolutePosition")]
        [SerializeField]
        Vector3 absolutePosition = Vector3.zero;
        [HideIf("UseAbsolutePosition")]
        [SerializeField]
        Vector3 moveAmount = Vector3.zero;


        private void _Init() {
            originPosition = target.localPosition;
        }


        public void Reset() => _Move(originPosition);
        public void Move() => _Move(UseAbsolutePosition ? absolutePosition : (target.localPosition + moveAmount));


        private void _Move(Vector3 pos) {
            if (useAnimation) {
                target.DOKill();
                target.DOLocalMove(pos, animationDuration);
            }
            else {
                target.localPosition = pos;
            }
        }
    }

}
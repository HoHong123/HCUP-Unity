using System;
using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;
using Util.OdinCompat;


namespace Util.UI.Entity {
    public partial class ScalingUiEntity {
#if ODIN_INSPECTOR
        [HeaderOrTitle("Target")]
        [SerializeField]
        [OnValueChanged(nameof(_Init))]
        Transform target;

        [HeaderOrTitle("Option")]
        [SerializeField]
        bool useAnimation = false;
        [ShowIf(nameof(useAnimation)), SerializeField]
        float animationDuration = 0.2f;

        [HeaderOrTitle("Scales")]
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
#endif
    }
}
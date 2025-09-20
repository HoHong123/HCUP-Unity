#if ODIN_INSPECTOR
using UnityEngine;
using Sirenix.OdinInspector;
using Util.OdinCompat;
#endif

namespace Util.UI.Entity {
    public partial class MovingUiEntity {
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

        [HeaderOrTitle("Positions")]
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
#endif
    }
}
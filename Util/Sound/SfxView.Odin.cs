#if ODIN_INSPECTOR
using System;
using Sirenix.OdinInspector;
#endif

namespace Util.Sound {
    public partial class SFXView {
#if ODIN_INSPECTOR
        [HideLabel]
        [HorizontalGroup("Row", Width = 0.7f), LabelWidth(75)]
        [OnValueChanged("_UpdateIdFromEnum")]
        public SFXList Clip;

        [HideLabel]
        [HorizontalGroup("Row", Width = 0.3f), LabelWidth(25)]
        [OnValueChanged("_UpdateEnumFromId")]
        public int Id;

        private void _UpdateIdFromEnum() {
            Id = (int)Clip;
        }

        private void _UpdateEnumFromId() {
            if (Enum.IsDefined(typeof(SFXList), Id))
                Clip = (SFXList)Id;
            else
                Id = (int)Clip;
        }
#endif
    }
}
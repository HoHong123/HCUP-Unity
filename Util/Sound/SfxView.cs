using System;
using Sirenix.OdinInspector;

namespace Util.Sound {
    [Serializable]
    public class SFXView {
        [HideLabel]
        [HorizontalGroup("Row", Width = 0.7f), LabelWidth(75)]
        [OnValueChanged("_UpdateIdFromEnum")]
        public SFXList Clip;

        [HideLabel]
        [HorizontalGroup("Row", Width = 0.3f), LabelWidth(25)]
        [OnValueChanged("_UpdateEnumFromId")]
        public int Id;

        public SFXView() => Init(SFXList.Click);
        public SFXView(SFXList sfx) => Init(sfx);
        public void Init(SFXList sfx) {
            Clip = sfx;
            Id = (int)sfx;
        }


        private void _UpdateIdFromEnum() {
            Id = (int)Clip;
        }

        private void _UpdateEnumFromId() {
            if (Enum.IsDefined(typeof(SFXList), Id))
                Clip = (SFXList)Id;
            else
                Id = (int)Clip;
        }
    }
}
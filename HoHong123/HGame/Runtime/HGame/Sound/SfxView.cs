using System;
using Sirenix.OdinInspector;

namespace Util.Sound {
    [Serializable]
    public class SFXView<T> where T : Enum  {
        [HideLabel]
        [HorizontalGroup("Row", Width = 0.7f), LabelWidth(75)]
        [OnValueChanged("_UpdateIdFromEnum")]
        public T Clip;

        [HideLabel]
        [HorizontalGroup("Row", Width = 0.3f), LabelWidth(25)]
        [OnValueChanged("_UpdateEnumFromId")]
        public int Id;


        public SFXView(T sfx) => Init(sfx);
        public void Init(T sfx) {
            Clip = sfx;
            Id = Convert.ToInt32(sfx);
        }

#if UNITY_EDITOR
        private void _UpdateIdFromEnum() {
            Id = Convert.ToInt32(Clip);
        }

        private void _UpdateEnumFromId() {
            if (Enum.IsDefined(typeof(T), Id)) {
                Clip = (T)Enum.ToObject(typeof(T), Id);
            } else {
                Id = Convert.ToInt32(Clip);
            }
        }
#endif
    }
}
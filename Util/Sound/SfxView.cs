using System;

namespace Util.Sound {
    [Serializable]
    public partial class SFXView {
#if !ODIN_INSPECTOR
        public SFXList Clip;
        public int Id;
#endif

        public SFXView() => Init(SFXList.Click);
        public SFXView(SFXList sfx) => Init(sfx);
        public void Init(SFXList sfx) {
            Clip = sfx;
            Id = (int)sfx;
        }
    }
}
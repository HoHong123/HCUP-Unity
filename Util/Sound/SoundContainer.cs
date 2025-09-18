using System;
using System.Linq;
using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace Util.Sound {
    [Serializable]
    public class SoundContainer {
        [ListDrawerSettings]
        public List<SFXView> Clips = new();


        public SoundContainer(List<SFXList> clips) {
            foreach (var clip in clips) {
                Clips.Add(new SFXView(clip));
            }
        }

        public void Add(SFXList clip) {
            if (Clips.Any(c => c.Clip == clip)) return;
            Clips.Add(new SFXView(clip));
        }
    }
}

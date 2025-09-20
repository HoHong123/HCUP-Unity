using UnityEngine;
using Util.OdinCompat;

namespace Util.Sound {
    public class SoundUnit : MonoBehaviour {
        [HeaderOrTitle("Container")]
        [SerializeField]
#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.InlineProperty, Sirenix.OdinInspector.HideLabel]
#endif
        SoundContainer container;


        private void Awake() {
            SoundManager.Instance.SetSoundUnit(container);
        }

        private void OnDestroy() {
            if (Application.isPlaying && SoundManager.HasInstance)
                SoundManager.Instance.RemoveSoundUnit(container);
        }
    }
}
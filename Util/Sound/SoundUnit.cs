using UnityEngine;
using Sirenix.OdinInspector;


namespace Util.Sound {
    public class SoundUnit : MonoBehaviour {
        [Title("Container")]
        [SerializeField, InlineProperty, HideLabel]
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
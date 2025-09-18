using UnityEngine;

namespace Util.Sound {
    /// <summary>
    /// For UI element to interacte with 'SoundManager' using unity event listener.
    /// </summary>
    public class SoundConnector : MonoBehaviour {
        public void PlayClickSound() => SoundManager.Instance.PlayClickSound();
        public void PlayOneShot(SFXList flag) => SoundManager.Instance.PlayOneShot(flag);
        public void PlayBGM(BGMList flag) => SoundManager.Instance.PlayBGM(flag);
    }
}

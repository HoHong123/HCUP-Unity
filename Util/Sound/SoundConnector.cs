using UnityEngine;
#if !ODIN_INSPECTOR
using Util.Logger;
#endif

namespace Util.Sound {
    /// <summary>
    /// For UI element to interacte with 'SoundManager' using unity event listener.
    /// </summary>
    public class SoundConnector : MonoBehaviour {
#if ODIN_INSPECTOR
        public void PlayClickSound() => SoundManager.Instance.PlayClickSound();
        public void PlayOneShot(SFXList flag) => SoundManager.Instance.PlayOneShot(flag);
        public void PlayBGM(BGMList flag) => SoundManager.Instance.PlayBGM(flag);
#else
        public void PlayClickSound() => HLogger.Warning($"Require 'Odin Inspector'");
        public void PlayOneShot(SFXList flag) => HLogger.Warning($"Require 'Odin Inspector'");
        public void PlayBGM(BGMList flag) => HLogger.Warning($"Require 'Odin Inspector'");
#endif
    }
}

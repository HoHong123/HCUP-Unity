using HGame.Sound.Core;
using HUtil.Inspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HGame.Sample.Sound {
    public class SoundSampleManager : MonoBehaviour {
        [System.Serializable]
        private struct SoundEntry {
            public TMP_InputField Input;
            public Button Btn;
        }

        [HTitle("Test Catalog")]
        [SerializeField]
        SoundCatalogSO testCatalog;
        [SerializeField]
        Button registerBtn;
        [SerializeField]
        Button releaseBtn;

        [HTitle("Test Play")]
        [SerializeField]
        SoundEntry play;

        [HTitle("Test BGM")]
        [SerializeField]
        SoundEntry playBgm;
        [SerializeField]
        SoundEntry stopBgm;


        private void Start() {
            registerBtn.onClick.AddListener(_OnClickRegister);
            releaseBtn.onClick.AddListener(_OnClickRelease);

            play.Btn.onClick.AddListener(_OnClickPlay);

            playBgm.Btn.onClick.AddListener(_OnClickPlayBGM);
            stopBgm.Btn.onClick.AddListener(_OnClickStop);
        }


        private void _OnClickRegister() {
            if (!testCatalog) return;
            Audio.SoundManager.Instance.PrewarmCatalog(testCatalog);
        }

        private void _OnClickRelease() {
            if (!testCatalog) return;
            Audio.SoundManager.Instance.ReleaseCatalog(testCatalog);
        }

        private void _OnClickPlay() {
            string token = play.Input.text;
            if (string.IsNullOrWhiteSpace(token)) return;
            Audio.SoundManager.Instance.Play(token);
        }

        private void _OnClickPlayBGM() {
            string token = play.Input.text;
            if (string.IsNullOrWhiteSpace(token)) return;
            Audio.SoundManager.Instance.PlayBGM(token);
        }

        private void _OnClickStop() {
            Audio.SoundManager.Instance.StopBGM(0);
        }
    }
}

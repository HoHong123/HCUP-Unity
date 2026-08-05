using HAudio.Core;
using HInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HAudio.Sample.Sound {
    public class AudioSampleManager : MonoBehaviour {
        [System.Serializable]
        private struct SoundEntry {
            public TMP_InputField Input;
            public Button Btn;
        }

        [HTitle("Test Catalog")]
        [SerializeField]
        AudioCatalogSO testCatalog;
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
            AudioManager.Instance.PrewarmCatalog(testCatalog);
        }

        private void _OnClickRelease() {
            if (!testCatalog) return;
            AudioManager.Instance.ReleaseCatalog(testCatalog);
        }

        private void _OnClickPlay() {
            string token = play.Input.text;
            if (string.IsNullOrWhiteSpace(token)) return;
            AudioManager.Instance.Play(token);
        }

        private void _OnClickPlayBGM() {
            string token = play.Input.text;
            if (string.IsNullOrWhiteSpace(token)) return;
            AudioManager.Instance.PlayBGM(token);
        }

        private void _OnClickStop() {
            AudioManager.Instance.StopBGM(0);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;
using UnityEngine.UIElements;

namespace Util.Sound {
    public class SoundSfxAgent : MonoBehaviour {
        [Title("Clips")]
        [SerializeField]
        [ListDrawerSettings]
        List<SFXView<SoundSFX>> clips;

        public List<SFXView<SoundSFX>> Clips => clips;


        private void Start() {
            SoundManager.Instance.PrewarmIds(clips);
        }

        private void OnDestroy() {
            if (Application.isPlaying && SoundManager.HasInstance)
                SoundManager.Instance.ReleaseIds(clips);
        }


        public void Play(SoundSFX sfx) {
            if (!SoundManager.HasInstance) return;
            SoundManager.Instance.Play(sfx);
        }

        public void Play3D(SoundSFX sfx, Transform parent) {
            if (!SoundManager.HasInstance) return;
            SoundManager.Instance.Play3D(sfx, parent);
        }

        public void Play3D(SoundSFX sfx, Vector3 position) {
            if (!SoundManager.HasInstance) return;
            SoundManager.Instance.Play3D(sfx, position);
        }
    }
}
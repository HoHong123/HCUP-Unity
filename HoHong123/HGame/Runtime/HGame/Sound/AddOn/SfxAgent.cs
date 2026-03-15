using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Cysharp.Threading.Tasks;
using HUtil.Data;

namespace HGame.Sound.AddOn {
    public class SfxAgent : MonoBehaviour, IDataSubscriber {
        #region Fields
        [Title("Clips")]
        [SerializeField]
        [ListDrawerSettings]
        List<SfxView> preloadUids = new();
        #endregion

        #region Properties
        public IReadOnlyList<SfxView> PreloadUids => preloadUids;
        #endregion

        #region Unity Life Cycle
        private void Start() {
            if (!SoundManager.HasInstance) return;
            SoundManager.Instance.PrewarmIds(preloadUids).Forget();
        }

        private void OnDestroy() {
            if (!Application.isPlaying || !SoundManager.HasInstance) return;
            SoundManager.Instance.ReleaseIds(preloadUids);
        }
        #endregion

        #region Play API
        public void Play(AudioClips clip) => Play((int)clip);
        public void PlayUI(AudioClips clip) => PlayUI((int)clip);
        public void Play3D(AudioClips clip, Transform parent) => Play3D((int)clip, parent);
        public void Play3D(AudioClips clip, Vector3 worldPos) => Play3D((int)clip, worldPos);

        public void Play(int uid) {
            if (!SoundManager.HasInstance) return;
            SoundManager.Instance.Play(uid);
        }

        public void PlayUI(int uid) {
            if (!SoundManager.HasInstance) return;
            SoundManager.Instance.PlayUI(uid);
        }

        public void Play3D(int uid, Transform parent) {
            if (!SoundManager.HasInstance) return;
            SoundManager.Instance.Play3D(uid, parent);
        }

        public void Play3D(int uid, Vector3 worldPos) {
            if (!SoundManager.HasInstance) return;
            SoundManager.Instance.Play3D(uid, worldPos);
        }
        #endregion
    }
}
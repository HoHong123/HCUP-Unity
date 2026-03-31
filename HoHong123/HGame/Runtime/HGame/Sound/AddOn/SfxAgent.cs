using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using HUtil.Data;

namespace HGame.Sound.AddOn {
    public class SfxAgent : MonoBehaviour, IDataSubscriber {
        #region Fields
        [Title("Settings")]
        [SerializeField]
        bool useNewManager = true;

        [Title("Clips")]
        [SerializeField]
        [ListDrawerSettings(DefaultExpandedState = true)]
        List<SfxView> preloadUids = new();
        #endregion

        #region Properties
        public IReadOnlyList<SfxView> PreloadUids => preloadUids;
        #endregion

        #region Unity Life Cycle
        private void Start() {
            if (!_HasTargetManager()) return;
            _PrewarmViews().Forget();
        }

        private void OnDestroy() {
            if (!Application.isPlaying) return;
            if (!_HasTargetManager()) return;
            _ReleaseViews();
        }
        #endregion

        #region Public - Token API
        public void Play(string token) {
            if (!useNewManager || !Audio.SoundManager.HasInstance) return;
            Audio.SoundManager.Instance.Play(token);
        }

        public void PlayUI(string token) {
            if (!useNewManager || !Audio.SoundManager.HasInstance) return;
            Audio.SoundManager.Instance.PlayUI(token);
        }

        public void Play3D(string token, Transform parent) {
            if (!useNewManager || !Audio.SoundManager.HasInstance) return;
            Audio.SoundManager.Instance.Play3D(token, parent);
        }

        public void Play3D(string token, Vector3 worldPos) {
            if (!useNewManager || !Audio.SoundManager.HasInstance) return;
            Audio.SoundManager.Instance.Play3D(token, worldPos);
        }
        #endregion

        #region Legacy Support
        public void Play(AudioClips clip) => Play((int)clip);
        public void PlayUI(AudioClips clip) => PlayUI((int)clip);
        public void Play3D(AudioClips clip, Transform parent) => Play3D((int)clip, parent);
        public void Play3D(AudioClips clip, Vector3 worldPos) => Play3D((int)clip, worldPos);

        public void Play(int uid) {
            if (useNewManager) {
                if (!Audio.SoundManager.HasInstance) return;
                Audio.SoundManager.Instance.Play(uid);
                return;
            }

            if (!SoundManager.HasInstance) return;
            SoundManager.Instance.Play(uid);
        }

        public void PlayUI(int uid) {
            if (useNewManager) {
                if (!Audio.SoundManager.HasInstance) return;
                Audio.SoundManager.Instance.PlayUI(uid);
                return;
            }

            if (!SoundManager.HasInstance) return;
            SoundManager.Instance.PlayUI(uid);
        }

        public void Play3D(int uid, Transform parent) {
            if (useNewManager) {
                if (!Audio.SoundManager.HasInstance) return;
                Audio.SoundManager.Instance.Play3D(uid, parent);
                return;
            }

            if (!SoundManager.HasInstance) return;
            SoundManager.Instance.Play3D(uid, parent);
        }

        public void Play3D(int uid, Vector3 worldPos) {
            if (useNewManager) {
                if (!Audio.SoundManager.HasInstance) return;
                Audio.SoundManager.Instance.Play3D(uid, worldPos);
                return;
            }

            if (!SoundManager.HasInstance) return;
            SoundManager.Instance.Play3D(uid, worldPos);
        }
        #endregion

        #region Private - Manager
        private bool _HasTargetManager() {
            if (useNewManager) return Audio.SoundManager.HasInstance;
            return SoundManager.HasInstance;
        }
        #endregion

        #region Private - Prewarm
        private async UniTaskVoid _PrewarmViews() {
            if (useNewManager) {
                foreach (var view in preloadUids) {
                    if (view == null) continue;
                    await Audio.SoundManager.Instance.PrewarmSfxView(view);
                }
                return;
            }

            await SoundManager.Instance.PrewarmIds(preloadUids);
        }

        private void _ReleaseViews() {
            if (useNewManager) {
                foreach (var view in preloadUids) {
                    if (view == null) continue;
                    Audio.SoundManager.Instance.ReleaseSfxView(view);
                }
                return;
            }

            SoundManager.Instance.ReleaseIds(preloadUids);
        }
        #endregion
    }
}

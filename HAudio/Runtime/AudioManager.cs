using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
using HAudio.Catalog;
using HAudio.Repository;
using HAudio.AddOn;
using HAudio.Core;
using HResource.Data;
using HCore;
using HInspector;
using HDiagnosis.Logger;
using HDiagnosis.HDebug;
#if UNITY_EDITOR && ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * token-first 런타임 매니저 스크립트입니다.
 *
 * 주의사항 ::
 * 1. 재생 전에 token 또는 catalog를 prewarm한 뒤 사용해야 합니다.
 * 2. AssetProvider 가 이 매니저를 소유자로 지문 발급하고, 파괴 시 자동 회수합니다.
 * =========================================================
 */
#endif

namespace HAudio {
    public sealed partial class AudioManager : SingletonBehaviour<AudioManager> {
        #region Const
        const string DEFAULT_CLICK_KEY = "Audio.DefaultClickUid";

        const float MIN_DB = -80f;
        const float MAX_DB = 0f;

        const string MIXER_MASTER = "MasterVolume";
        const string MIXER_SFX = "SFXVolume";
        const string MIXER_UI = "UIVolume";
        const string MIXER_BGM = "BGMVolume";

        const string PREF_MASTER = "Audio.Master";
        const string PREF_SFX = "Audio.SFX";
        const string PREF_UI = "Audio.UI";
        const string PREF_BGM = "Audio.BGM";
        #endregion

        #region Static
        static int privateDefaultClickUid;

        public static int DefaultClickUid => privateDefaultClickUid;

        /// <summary> 전역 기본 클릭음을 uid 로 지정한다. 0 이면 기본 클릭음 없음. </summary>
        public static void SetGlobalClickUid(int uid) {
            privateDefaultClickUid = uid;
            PlayerPrefsHandler.SetInt(DEFAULT_CLICK_KEY, uid);
        }
        #endregion

        #region Fields
        [HTitle("Data Load")]
        [SerializeField]
        AssetLoadMode loadMode = AssetLoadMode.Resources;

        [HTitle("Default Click")]
        [HDropdown(AudioCatalogSO.DROPDOWN_SOURCE_ID)]
        [SerializeField]
        int initialDefaultClickUid;

        AudioCatalogRegistry catalogRegistry;
        IAudioClipRepository clipRepository;

        int inFlightPrewarmCount; // (케이스 리포트 07 RACE-2).
        bool isDestroyed;

        [HTitle("Audio Mixer")]
        [SerializeField]
        AudioMixer audioMix;

        [HTitle("Audio Sources")]
        [SerializeField]
        AudioSource sfxAudio;
        [SerializeField]
        AudioSource bgmAudio;
        [SerializeField]
        AudioSource bgmAltAudio;
        [SerializeField]
        AudioSource uiAudio;
        [SerializeField]
        AudioSpatialPool spatialPool;
        #endregion

        #region Properties
#if UNITY_EDITOR && ODIN_INSPECTOR
        [Title("Preview")]
        [ShowInInspector, Searchable]
        AudioClipManagerSnapshot Preview => CreateSnapshot();
#endif
        #endregion

        #region Unity Life Cycle
        protected override void Awake() {
            base.Awake();
            if (instance != this) return;

            _BuildRepository();
        }

        private void Start() {
            if (instance != this) return;
            _CheckPlayerPrefs();
        }

        protected override void OnDestroy() {
            if (instance != this) return;
            isDestroyed = true;

            // 저장소를 만든 쪽이 폐기까지 책임.
            if (inFlightPrewarmCount < 1) {
                _DisposeRepository();
            }
            else {
                _DisposeRepositoryAfterPrewarmAsync().Forget();
            }

            base.OnDestroy();
        }
        #endregion

        #region Private - Teardown
        // OnDestroy 직후 곧바로 폐기하면, 아직 await 중인 prewarm 이 뒤늦게 캐시에 등록되어 로더 핸들이 영구 잔존.
        private async UniTaskVoid _DisposeRepositoryAfterPrewarmAsync() {
            const int MAX_WAIT_FRAMES = 600;   // 60fps 기준 약 10초

            int waited = 0;
            while (inFlightPrewarmCount > 0 && waited < MAX_WAIT_FRAMES) {
                await UniTask.Yield();
                waited++;
            }

            if (inFlightPrewarmCount > 0) {
                HLogger.Warning(
                    $"[AudioManager] Disposing with {inFlightPrewarmCount} prewarm task(s) still in flight after {MAX_WAIT_FRAMES} frames.");
            }

            _DisposeRepository();
        }

        // 두 경로(동기 즉시 / prewarm 대기 후)가 같은 정리를 쓰도록 분리.
        private void _DisposeRepository() {
            var repository = clipRepository;
            clipRepository = null;
            if (repository == null) return;

            repository.ReleaseAll();
            repository.Dispose();
        }

        // prewarm 진입점 2곳(PrewarmToken / PrewarmCatalog)을 감싸 진행 중 건수를 센다.
        private async UniTask _TrackPrewarmAsync(UniTask inner) {
            inFlightPrewarmCount++;
            try {
                await inner;
            }
            finally {
                inFlightPrewarmCount--;
            }
        }

        private bool _RejectPrewarmIfDestroyed(string apiName) {
            if (!isDestroyed && clipRepository != null) return false;
            HLogger.Warning($"[AudioManager] {apiName} called after destroy. The request is ignored.");
            return true;
        }
        #endregion

        #region Private - Init
        private void _CheckPlayerPrefs() {
            SetMasterVolume(PlayerPrefsHandler.GetFloat(PREF_MASTER, 1f), save: false);
            SetSfxVolume(PlayerPrefsHandler.GetFloat(PREF_SFX, 1f), save: false);
            SetUiVolume(PlayerPrefsHandler.GetFloat(PREF_UI, 1f), save: false);
            SetBgmVolume(PlayerPrefsHandler.GetFloat(PREF_BGM, 1f), save: false);

            int fallbackUid = initialDefaultClickUid;
            if (!PlayerPrefsHandler.HasKey(DEFAULT_CLICK_KEY)) {
                PlayerPrefsHandler.SetInt(DEFAULT_CLICK_KEY, fallbackUid);
                privateDefaultClickUid = fallbackUid;
                return;
            }

            privateDefaultClickUid = PlayerPrefsHandler.GetInt(DEFAULT_CLICK_KEY, fallbackUid);
        }

        private void _BuildRepository() {
            // Assert 는 릴리즈에서 제거되어 무방비가 된다. 필드 누락은 릴리즈에서도 로그로 드러낸다.
            if (audioMix == null) HLogger.Error("[AudioManager] Mixer is null.");
            if (sfxAudio == null) HLogger.Error("[AudioManager] sfxAudio is null.");
            if (bgmAudio == null) HLogger.Error("[AudioManager] bgmAudio is null.");
            if (uiAudio == null) HLogger.Error("[AudioManager] uiAudio is null.");
            if (spatialPool == null) HLogger.Error("[AudioManager] spatialPool is null.");
            catalogRegistry = new AudioCatalogRegistry();
            clipRepository = new AudioClipRepository(loadMode, catalogRegistry, this);
        }
        #endregion

        #region Public - Prewarm
        public UniTask PrewarmToken(int uid) {
            if (_RejectPrewarmIfDestroyed(nameof(PrewarmToken))) return UniTask.CompletedTask;
            return _TrackPrewarmAsync(clipRepository.PrewarmTokenAsync(uid));
        }

        public UniTask PrewarmToken(string token) {
            if (_RejectPrewarmIfDestroyed(nameof(PrewarmToken))) return UniTask.CompletedTask;

            string normalizedToken = _NormalizeToken(token);
#if UNITY_EDITOR
            _TrackPreviewToken(normalizedToken);
#endif
            return _TrackPrewarmAsync(clipRepository.PrewarmTokenAsync(normalizedToken));
        }

        public async UniTask PrewarmSfxView(SfxView view) {
            foreach (var catalog in view.Catalogs) {
                await PrewarmCatalog(catalog);
            }
        }

        public UniTask PrewarmCatalog(AudioCatalogSO catalog) {
            if (_RejectPrewarmIfDestroyed(nameof(PrewarmCatalog))) return UniTask.CompletedTask;

#if UNITY_EDITOR
            _TrackPreviewCatalog(catalog);
#endif
            return _TrackPrewarmAsync(clipRepository.PrewarmCatalogAsync(catalog));
        }

        public async UniTask PrewarmTokens(IEnumerable<string> tokens) {
            List<UniTask> tasks = new List<UniTask>();
            foreach (var token in tokens) {
                tasks.Add(PrewarmToken(token));
            }
            await UniTask.WhenAll(tasks);
        }

        public async UniTask PrewarmCatalogs(IEnumerable<AudioCatalogSO> catalogs) {
            List<UniTask> tasks = new List<UniTask>();
            foreach (var catalog in catalogs) {
                tasks.Add(PrewarmCatalog(catalog));
            }
            await UniTask.WhenAll(tasks);
        }
        #endregion

        #region Public - Release
        public bool ReleaseToken(int uid) {
            if (_RejectReleaseIfDisposed(nameof(ReleaseToken))) return false;
            return clipRepository.Release(uid);
        }

        public bool ReleaseToken(string token) {
            if (_RejectReleaseIfDisposed(nameof(ReleaseToken))) return false;

            string normalizedToken = _NormalizeToken(token);
#if UNITY_EDITOR
            _TrackPreviewToken(normalizedToken);
#endif
            return clipRepository.Release(normalizedToken);
        }

        public void ReleaseSfxView(SfxView view) {
            foreach (var catalog in view.Catalogs) {
                ReleaseCatalog(catalog);
            }
        }

        public void ReleaseCatalog(AudioCatalogSO catalog) {
            if (_RejectReleaseIfDisposed(nameof(ReleaseCatalog))) return;

#if UNITY_EDITOR
            _ReleasePreviewCatalog(catalog);
#endif
            clipRepository.ReleaseCatalog(catalog);
        }

        public void ReleaseAllManaged() {
            if (_RejectReleaseIfDisposed(nameof(ReleaseAllManaged))) return;
            clipRepository.ReleaseAll();
        }

        private bool _RejectReleaseIfDisposed(string apiName) {
            if (clipRepository != null) return false;
            HLogger.Warning(
                $"[AudioManager] {apiName} called after the repository was disposed. Nothing to release.");
            return true;
        }
        #endregion

        #region Public - Play (uid)
        public void PlayUI(int uid) {
            if (!_TryGetLoadedClip(uid, out var clip)) return;
            uiAudio.PlayOneShot(clip);
        }

        public void Play(int uid) {
            if (!_TryGetLoadedClip(uid, out var clip)) return;
            sfxAudio.PlayOneShot(clip);
        }

        public void Play3D(int uid, Transform parent) {
            if (!_TryGetLoadedClip(uid, out var clip)) return;
            spatialPool.PlayAt(clip, parent, sfxAudio.volume);
        }

        public void Play3D(int uid, Vector3 worldPos) {
            if (!_TryGetLoadedClip(uid, out var clip)) return;
            spatialPool.PlayAt(clip, worldPos, sfxAudio.volume);
        }

        public void PlayBGM(int uid, bool ignoreSameClip = true) {
            if (!_TryGetLoadedClip(uid, out var clip)) return;
            if (ignoreSameClip && bgmAudio.isPlaying && bgmAudio.clip == clip) return;

            bgmAudio.clip = clip;
            bgmAudio.Play();
        }
        #endregion

        #region Public - Play (token)
        public void PlayClick() {
            if (privateDefaultClickUid == 0) return;
            PlayUI(privateDefaultClickUid);
        }

        public void PlayUI(string token) {
            if (!_TryGetLoadedClip(token, out var clip)) return;
            uiAudio.PlayOneShot(clip);
        }

        public void Play(string token) {
            if (!_TryGetLoadedClip(token, out var clip)) return;
            sfxAudio.PlayOneShot(clip);
        }

        public void Play3D(string token, Transform parent) {
            if (!_TryGetLoadedClip(token, out var clip)) return;
            spatialPool.PlayAt(clip, parent, sfxAudio.volume);
        }

        public void Play3D(string token, Vector3 worldPos) {
            if (!_TryGetLoadedClip(token, out var clip)) return;
            spatialPool.PlayAt(clip, worldPos, sfxAudio.volume);
        }

        public void PlayBGM(string token, bool ignoreSameClip = true) {
            if (string.IsNullOrWhiteSpace(token)) return;
            if (!_TryGetLoadedClip(token, out var clip)) return;
            if (ignoreSameClip && bgmAudio.isPlaying && bgmAudio.clip == clip) return;

            bgmAudio.clip = clip;
            bgmAudio.Play();
        }

        public async void StopBGM(float fadeOut = 0f) {
            if (fadeOut <= 0f) {
                bgmAudio.Stop();
                return;
            }

            float time = 0f;
            float volume = bgmAudio.volume;

            while (time < fadeOut) {
                time += Time.unscaledDeltaTime;
                float fade = Mathf.Clamp01(time / fadeOut);
                bgmAudio.volume = Mathf.Lerp(volume, 0f, fade);
                await UniTask.Yield();
            }

            bgmAudio.Stop();
            bgmAudio.volume = volume;
        }
        #endregion

        #region Private - Clip
        private bool _TryGetLoadedClip(int uid, out AudioClip clip) {
            clip = null;
            if (clipRepository == null) {
                HLogger.Error("[AudioManager] clipRepository is null.");
                return false;
            }

            if (clipRepository.TryGet(uid, out clip) && clip) return true;
#if UNITY_EDITOR
            HDebug.StackTraceError($"[AudioManager] Clip not loaded yet. Prewarm required. uid={uid}", 10);
#endif

            clip = null;
            return false;
        }

        private bool _TryGetLoadedClip(string token, out AudioClip clip) {
            if (clipRepository == null) {
                HLogger.Error("[AudioManager] clipRepository is null.");
                clip = null;
                return false;
            }
            string normalizedToken = _NormalizeToken(token);
#if UNITY_EDITOR
            _TrackPreviewToken(normalizedToken);
#endif
            if (clipRepository.TryGet(normalizedToken, out clip) && clip) return true;
#if UNITY_EDITOR
            HDebug.StackTraceError($"[AudioManager] Clip not loaded yet. Prewarm required. token={normalizedToken}", 10);
#endif

            clip = null;
            return false;
        }
        #endregion

        #region Public - Volume Control
        public bool IsMasterOn => GetMasterVolume01() > 0f;
        public bool IsSfxOn => GetSfxVolume01() > 0f;
        public bool IsUiOn => GetUiVolume01() > 0f;
        public bool IsBgmOn => GetBgmVolume01() > 0f;

        public float GetMasterVolume01() => _GetLocalMixerVolume01(PREF_MASTER);
        public float GetSfxVolume01() => _GetLocalMixerVolume01(PREF_SFX);
        public float GetUiVolume01() => _GetLocalMixerVolume01(PREF_UI);
        public float GetBgmVolume01() => _GetLocalMixerVolume01(PREF_BGM);

        public void SetMasterVolume(float volume, bool save = true) {
            _SetMixerVolume(MIXER_MASTER, volume);
            if (save) PlayerPrefsHandler.SetFloat(PREF_MASTER, Mathf.Clamp01(volume));
        }

        public void SetSfxVolume(float volume, bool save = true) {
            _SetMixerVolume(MIXER_SFX, volume);
            if (save) PlayerPrefsHandler.SetFloat(PREF_SFX, Mathf.Clamp01(volume));
        }

        public void SetUiVolume(float volume, bool save = true) {
            _SetMixerVolume(MIXER_UI, volume);
            if (save) PlayerPrefsHandler.SetFloat(PREF_UI, Mathf.Clamp01(volume));
        }

        public void SetBgmVolume(float volume, bool save = true) {
            _SetMixerVolume(MIXER_BGM, volume);
            if (save) PlayerPrefsHandler.SetFloat(PREF_BGM, Mathf.Clamp01(volume));
        }
        #endregion

        #region Private - Mixer
        private void _SetMixerVolume(string exposedName, float volume01) {
            float db = _ToDecibel(volume01);
            audioMix.SetFloat(exposedName, db);
        }

        private float _GetLocalMixerVolume01(string prefKey) {
            return Mathf.Clamp01(PlayerPrefsHandler.GetFloat(prefKey, 1f));
        }

        private float _ToDecibel(float volume01) {
            volume01 = Mathf.Clamp01(volume01);
            if (volume01 <= 0.0001f) return MIN_DB;
            float db = Mathf.Log10(volume01) * 20f;
            return Mathf.Clamp(db, MIN_DB, MAX_DB);
        }
        #endregion

        #region Private - Token
        static string _NormalizeToken(string token) {
            if (string.IsNullOrWhiteSpace(token)) return string.Empty;
            return token.Trim();
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 주요 기능 ::
 * 1. token과 catalog 단위의 preload와 release를 제공합니다.
 * 2. SFX, UI, 3D, BGM 재생 경로를 분리해 제공합니다.
 * 3. 오디오 믹서 볼륨과 기본 클릭 토큰을 관리합니다.
 *
 * 사용법 ::
 * 1. 인스펙터에서 Mixer와 AudioSource 참조를 먼저 연결합니다.
 * 2. 사용 전 PrewarmToken 또는 PrewarmCatalog를 호출합니다.
 * 3. 재생은 Play, PlayUI, Play3D, PlayBGM 계열 메서드로 수행합니다.
 *
 * 이벤트 ::
 * 1. Awake에서 repository와 owner를 초기화합니다.
 * 2. OnDestroy에서 in-flight prewarm 완료를 기다린 뒤 owner 해제 + repository 폐기를 수행합니다.
 *    (OnDestroy는 await할 수 없어 _DisposeRepositoryAfterPrewarmAsync로 분리했습니다.)
 * 3. 폐기 이후의 prewarm/release 요청은 경고와 함께 무시됩니다.
 *
 * 기타 ::
 * 1. 기준 키는 string token 단일 체계입니다. int/enum 진입점은 없습니다.
 * 2. 에디터 preview 기능은 별도 partial에서만 동작합니다.
 * =========================================================
 */
#endif

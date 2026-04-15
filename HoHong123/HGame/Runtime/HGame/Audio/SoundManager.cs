using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Assertions;
using HGame.Audio.Catalog;
using HGame.Audio.Repository;
using HGame.Sound.AddOn;
using HGame.Sound.Core;
using HUtil.AssetHandler.Data;
using HUtil.AssetHandler.Subscription;
using HCore;
using HUtil.Data.Load;
using HInspector;
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
 * 2. Awake에서 발급한 ownerId를 기준으로 OnDestroy 시점에 일괄 해제합니다.
 * =========================================================
 */
#endif

namespace HGame.Audio {
    public sealed partial class SoundManager : SingletonBehaviour<SoundManager> {
        #region Const
        const string DEFAULT_CLICK_KEY = "Audio.DefaultClickToken";

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
        static string privateDefaultClickToken = string.Empty;

        public static string DefaultClickToken => privateDefaultClickToken;

        public static void SetGlobalClickToken(string token) {
            privateDefaultClickToken = _NormalizeToken(token);
            PlayerPrefsHandler.SetString(DEFAULT_CLICK_KEY, privateDefaultClickToken);
        }
        #endregion

        #region Fields
        [HTitle("Data Load")]
        [SerializeField]
        AssetLoadMode loadMode = AssetLoadMode.Resources;

        [HTitle("Default Token")]
        [SerializeField]
        string initialDefaultClickToken = string.Empty;

        SoundCatalogRegistry catalogRegistry;
        ISoundClipRepository clipRepository;
        AssetOwnerId ownerId;

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
        SoundSpatialPool spatialPool;
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
            ownerId = AssetOwnerIdGenerator.NewId(this);
            _BuildRepository();
        }

        void Start() {
            if (instance != this) return;
            _CheckPlayerPrefs();
        }

        void OnDestroy() {
            if (instance != this) return;

            clipRepository?.ReleaseOwner(ownerId);
            AssetOwnerIdGenerator.NotifyReleased(ownerId);
        }
        #endregion

        #region Private - Init
        private void _CheckPlayerPrefs() {
            SetMasterVolume(PlayerPrefsHandler.GetFloat(PREF_MASTER, 1f), save: false);
            SetSfxVolume(PlayerPrefsHandler.GetFloat(PREF_SFX, 1f), save: false);
            SetUiVolume(PlayerPrefsHandler.GetFloat(PREF_UI, 1f), save: false);
            SetBgmVolume(PlayerPrefsHandler.GetFloat(PREF_BGM, 1f), save: false);

            string fallbackToken = _NormalizeToken(initialDefaultClickToken);
            if (!PlayerPrefsHandler.HasKey(DEFAULT_CLICK_KEY)) {
                PlayerPrefsHandler.SetString(DEFAULT_CLICK_KEY, fallbackToken);
                privateDefaultClickToken = fallbackToken;
                return;
            }

            privateDefaultClickToken = _NormalizeToken(PlayerPrefsHandler.GetString(DEFAULT_CLICK_KEY, fallbackToken));
        }

        private void _BuildRepository() {
#if UNITY_ASSERTIONS
            Assert.IsNotNull(audioMix, "[Audio.SoundManager] Mixer is null.");
            Assert.IsNotNull(sfxAudio, "[Audio.SoundManager] sfxAudio is null.");
            Assert.IsNotNull(bgmAudio, "[Audio.SoundManager] bgmAudio is null.");
            Assert.IsNotNull(uiAudio, "[Audio.SoundManager] uiAudio is null.");
            Assert.IsNotNull(spatialPool, "[Audio.SoundManager] spatialPool is null.");
#endif
            catalogRegistry = new SoundCatalogRegistry();
            clipRepository = new SoundClipRepository(loadMode, catalogRegistry);
        }
        #endregion

        #region Public - Prewarm
        public UniTask PrewarmToken(string token) {
            string normalizedToken = _NormalizeToken(token);
#if UNITY_EDITOR
            _TrackPreviewToken(normalizedToken);
#endif
            return clipRepository.PrewarmTokenAsync(normalizedToken, ownerId);
        }

        public async UniTask PrewarmSfxView(SfxView view) {
            foreach (var catalog in view.Catalogs) {
                await PrewarmCatalog(catalog);
            }
        }

        public UniTask PrewarmCatalog(SoundCatalogSO catalog) {
#if UNITY_EDITOR
            _TrackPreviewCatalog(catalog);
#endif
            return clipRepository.PrewarmCatalogAsync(catalog, ownerId);
        }

        public async UniTask PrewarmTokens(IEnumerable<string> tokens) {
            List<UniTask> tasks = new List<UniTask>();
            foreach (var token in tokens) {
                tasks.Add(PrewarmToken(token));
            }
            await UniTask.WhenAll(tasks);
        }

        public async UniTask PrewarmCatalogs(IEnumerable<SoundCatalogSO> catalogs) {
            List<UniTask> tasks = new List<UniTask>();
            foreach (var catalog in catalogs) {
                tasks.Add(PrewarmCatalog(catalog));
            }
            await UniTask.WhenAll(tasks);
        }
        #endregion

        #region Public - Release
        public bool ReleaseToken(string token) {
            string normalizedToken = _NormalizeToken(token);
#if UNITY_EDITOR
            _TrackPreviewToken(normalizedToken);
#endif
            return clipRepository.Release(normalizedToken, ownerId);
        }

        public void ReleaseSfxView(SfxView view) {
            foreach (var catalog in view.Catalogs) {
                ReleaseCatalog(catalog);
            }
        }

        public void ReleaseCatalog(SoundCatalogSO catalog) {
#if UNITY_EDITOR
            _ReleasePreviewCatalog(catalog);
#endif
            clipRepository.ReleaseCatalog(catalog, ownerId);
        }

        public void ReleaseAllManaged() {
            clipRepository.ReleaseOwner(ownerId);
        }
        #endregion

        #region Public - Play
        public void PlayClick() {
            if (string.IsNullOrWhiteSpace(privateDefaultClickToken)) return;
            PlayUI(privateDefaultClickToken);
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
        private bool _TryGetLoadedClip(string token, out AudioClip clip) {
            Assert.IsNotNull(clipRepository, "[Audio.SoundManager] clipRepository is null.");
            string normalizedToken = _NormalizeToken(token);
#if UNITY_EDITOR
            _TrackPreviewToken(normalizedToken);
#endif
            if (clipRepository.TryGet(normalizedToken, out clip) && clip) return true;
#if UNITY_EDITOR
            HDebug.StackTraceError($"[Audio.SoundManager] Clip not loaded yet. Prewarm required. token={normalizedToken}", 10);
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
            return Mathf.Clamp01(PlayerPrefsHandler.GetFloat(prefKey));
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
 * 2. OnDestroy에서 owner 기준으로 관리 중인 asset을 해제합니다.
 *
 * 기타 ::
 * 1. 레거시 호출 지원은 Legacy partial에 분리되어 있습니다.
 * 2. 에디터 preview 기능은 별도 partial에서만 동작합니다.
 * =========================================================
 */
#endif

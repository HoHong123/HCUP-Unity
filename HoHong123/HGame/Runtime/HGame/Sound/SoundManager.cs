using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Assertions;
using Sirenix.OdinInspector;
using HGame.Sound.AddOn;
using HGame.Sound.Core;
using HGame.Sound.Load;
using HUtil.Core;
using HUtil.Data.Load;
using HUtil.Inspector;
using HUtil.Logger;
using HUtil.Diagnosis;

namespace HGame.Sound {
    public sealed class SoundManager : SingletonBehaviour<SoundManager> {
        #region Const
        const string DEFAULT_CLICK_KEY = "DEFAULT_CLICK_SOUND";

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
        #region Default Click Option
        static int PRIVATE_DEFAULT_CLICK_UID = 600003;
        public static int DEFAULT_CLICK_UID => PRIVATE_DEFAULT_CLICK_UID;

        public static void SetGlobalClickUid(AudioClips clip) => SetGlobalClickUid((int)clip);
        /// <summary> 프로젝트 전체 버튼 클릭 사운드 변경 </summary>
        public static void SetGlobalClickUid(int uid) {
            PRIVATE_DEFAULT_CLICK_UID = uid;
            PlayerPrefsHandler.SetInt(DEFAULT_CLICK_KEY, PRIVATE_DEFAULT_CLICK_UID);
        }
        #endregion
        #endregion

        #region Fields
        [HTitle("Data Load")]
        [SerializeField]
        DataLoadType load = DataLoadType.Resources;

        AudioClipProvider clipProvider;

        [HTitle("Audio Mixer")]
        [Required]
        [SerializeField]
        AudioMixer audioMix;

        [HTitle("Audio Sources")]
        [Required]
        [SerializeField]
        AudioSource sfxAudio;
        [Required]
        [SerializeField]
        AudioSource bgmAudio;
        [Required]
        [SerializeField]
        AudioSource bgmAltAudio;
        [Required]
        [SerializeField]
        AudioSource uiAudio;
        [Required]
        [SerializeField]
        SoundSpatialPool spatialPool;
        #endregion

        #region Properties
#if UNITY_EDITOR && ODIN_INSPECTOR
        [Title("Preview")]
        [ShowInInspector, Searchable]
        IReadOnlyDictionary<int, AudioClipCache.Item> Preview => clipProvider?.Preview;
#endif
        #endregion

        #region ======== Unity Life Cycle ========
        protected override void Awake() {
            base.Awake();
            _BuildClipProvider();
        }

        private void Start() {
            _CheckPlayerPrefs();
        }
        #endregion

        #region ======== Init ========
        private void _CheckPlayerPrefs() {
            SetMasterVolume(PlayerPrefsHandler.GetFloat(PREF_MASTER, 1), save: false);
            SetSfxVolume(PlayerPrefsHandler.GetFloat(PREF_SFX, 1), save: false);
            SetUiVolume(PlayerPrefsHandler.GetFloat(PREF_UI, 1), save: false);
            SetBgmVolume(PlayerPrefsHandler.GetFloat(PREF_BGM, 1), save: false);

            if (!PlayerPrefsHandler.HasKey(DEFAULT_CLICK_KEY)) {
                PlayerPrefsHandler.SetInt(DEFAULT_CLICK_KEY, PRIVATE_DEFAULT_CLICK_UID);
                return;
            }
            PRIVATE_DEFAULT_CLICK_UID = PlayerPrefsHandler.GetInt(DEFAULT_CLICK_KEY);
        }

        private void _BuildClipProvider() {
#if UNITY_ASSERTIONS
            Assert.IsNotNull(audioMix, "[SoundManager] Mixer is null.");
            Assert.IsNotNull(sfxAudio, "[SoundManager] sfxAudio is null.");
            Assert.IsNotNull(bgmAudio, "[SoundManager] bgmAudio is null.");
            Assert.IsNotNull(uiAudio, "[SoundManager] uiAudio is null.");
            Assert.IsNotNull(spatialPool, "[SoundManager] spatialPool is null.");
#endif
            clipProvider = new AudioClipProvider(load: load);
        }
        #endregion

        #region ======== Registration ========
        #region Prewarm
        public UniTask PrewarmId(int id) => clipProvider.PrewarmIdAsync(id);

        public UniTask PrewarmId(SoundCatalogSO catalog) => clipProvider.PrewarmCatalogAsync(catalog);

        public async UniTask PrewarmIds(IEnumerable<int> ids) {
            List<UniTask> tasks = new();
            foreach (var id in ids)
                tasks.Add(PrewarmId(id));
            await UniTask.WhenAll(tasks);
        }

        public async UniTask PrewarmIds(IEnumerable<SfxView> view) {
            List<UniTask> tasks = new();
            foreach (var v in view)
                tasks.Add(PrewarmIds(v.Catalogs));
            await UniTask.WhenAll(tasks);
        }

        public async UniTask PrewarmIds(IEnumerable<SoundCatalogSO> catalogs) {
            List<UniTask> tasks = new();
            foreach (var cat in catalogs)
                tasks.Add(PrewarmId(cat));
            await UniTask.WhenAll(tasks);
        }
        #endregion

        #region Release
        public void ReleaseId(int id) => clipProvider.ReleaseId(id);

        public void ReleaseIds(IEnumerable<int> ids) {
            foreach (var id in ids)
                ReleaseId(id);
        }

        public void ReleaseIds(IEnumerable<SfxView> view) {
            foreach (var v in view) {
                foreach (var cat in v.Catalogs) {
                    ReleaseIds(cat);
                }
            }
        }

        public void ReleaseIds(SoundCatalogSO catalog) => clipProvider.ReleaseCatalog(catalog);
        #endregion
        #endregion

        #region ======== Play ========
        public void PlayClick() => PlayUI(PRIVATE_DEFAULT_CLICK_UID);

        public void PlayUI(AudioClips clips) => PlayUI((int)clips);
        public void Play(AudioClips clips) => Play((int)clips);
        public void Play3D(AudioClips clips, Transform parent) => Play3D((int)clips, parent);
        public void Play3D(AudioClips clips, Vector3 worldPos) => Play3D((int)clips, worldPos);
        public void PlayBGM(AudioClips clips) => PlayBGM((int)clips);

        public void PlayUI(int id) {
            if (!_TryGetLoadedClip(id, out var clip)) return;
            uiAudio.PlayOneShot(clip);
        }

        public void Play(int id) {
            if (!_TryGetLoadedClip(id, out var clip)) return;
            sfxAudio.PlayOneShot(clip);
        }

        public void Play3D(int id, Transform parent) {
            if (!_TryGetLoadedClip(id, out var clip)) return;
            spatialPool.PlayAt(clip, parent, sfxAudio.volume);
        }

        public void Play3D(int id, Vector3 worldPos) {
            if (!_TryGetLoadedClip(id, out var clip)) return;
            spatialPool.PlayAt(clip, worldPos, sfxAudio.volume);
        }

        public void PlayBGM(int id, bool ignorSameClip = true) {
            if (!_TryGetLoadedClip(id, out var clip)) return;
            if (ignorSameClip && bgmAudio.clip == clip) return;

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


        private bool _TryGetLoadedClip(int id, out AudioClip clip) {
#if UNITY_ASSERTIONS
            Assert.IsNotNull(clipProvider, "[SoundManager] clipProvider is null.");
#endif
            // 실제 사운드 클립을 사용하는 객체는 SoundManager 단일 객체이기에 의존도를 높이지 않고 Peek로 클립 사용.
            if (clipProvider.TryGet(id, out clip) && clip) return true;

#if UNITY_EDITOR
            HDebug.StackTraceError($"[SoundManager] Data not loaded yet. Prewarm required. uid={id}", 10);
#endif
            clip = null;
            return false;
        }
        #endregion

        #region ======== Volume Control ========
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

        private void _SetMixerVolume(string exposedName, float volume01) {
            var db = _ToDecibel(volume01);
            audioMix.SetFloat(exposedName, db);
        }

        private float _GetLocalMixerVolume01(string prefKey) {
            return Mathf.Clamp01(PlayerPrefsHandler.GetFloat(prefKey));
        }

        private float _GetMixerVolume01(string exposedName, string prefKey) {
            if (audioMix.GetFloat(exposedName, out float db)) return _ToLinear01(db);
            return Mathf.Clamp01(PlayerPrefsHandler.GetFloat(prefKey));
        }

        private float _ToDecibel(float volume01) {
            volume01 = Mathf.Clamp01(volume01);
            if (volume01 <= 0.0001f) return MIN_DB;
            float db = Mathf.Log10(volume01) * 20f;
            return Mathf.Clamp(db, MIN_DB, MAX_DB);
        }

        private float _ToLinear01(float db) {
            if (db <= MIN_DB + 0.01f) return 0f;
            float linear = Mathf.Pow(10f, db / 20f);
            return Mathf.Clamp01(linear);
        }
        #endregion

        #region ======== Debug ========
#if UNITY_EDITOR
        public bool TryGetClipDiagnostics(out IAudioClipDiagnostics diagnostics) {
            diagnostics = clipProvider as IAudioClipDiagnostics;
            return diagnostics != null;
        }
#endif
        #endregion
    }
}


#if UNITY_EDITOR
/* - 개발 로그 -
 * ==========================================================
 * @Jason <사운드 리스트 관리>
 * Dictionary를 통해 수많은 사운드 리소스를 필요에 따라 O(1)의 속도로 빠르게 접근할 필요가 있다고 생각하여 작성하였습니다.
 * Serializable Dictionary를 통해 인스펙터 GUI에서 데이터 포맷을 관리하기 쉽게 만드는 시도를 하였습니다.
 * 하지만 현재까지 리서치한 Serializable Dictionary 방법들은 사실상 리스트 2개를 사용한 선형구조를 눈 속임으로 사용하는 것이 었습니다.
 * 하여 충분히 필요에 따라 내부 로직을 추후에 변경하여도 상관이 없습니다.
 * ==========================================================
 * @Jason <종속성 체크>
 * 현재 사운드 클립은 SoundUnit 컨포넌트가 존재하는 경우에만 추가되고 해당 컨포넌트가 파괴되면 연관된 사운드 클립을 제거하는 방식입니다.
 * 이러한 방식은 동일한 사운드 크립을 사용하는 유닛이 중복되어 생성될 경우, 하나의 유닛이 파괴될 때, 공통으로 사용하는 클립이 제거되는 문제가 발생할 수 있습니다. 하여 종속성을 카운트하는 로직을 넣기로 하였습니다.
 * 여러 접근 방식이 있었지만, 딕셔너리 내부에 종속되는 유닛의 수를 카운트 할 수 있는 클래스를 따로 만드는 것이 가장 가독성이 좋을 것이라 생각하여 SoundItem 클래스를 만들었습니다.
 * ==========================================================
 * @Jason 2025.12.18
 * <리소스 로드 시스템 추가>
 * UID 기반 시스템으로 확정 후, 리소스를 로드하는 방식(Resources, Addressable, etc)을 선택하도는 기능 추가 및 사운드 클립 리소스 관리 방식도 대규모 리펙토링하였습니다.
 * Ps. SoundMajorCatelog, SoundCatalogSO, SoundPolicySO 등 참고 필수
 * <등록/제거 기능 추가> 
 * 리소스 로드 방식의 확정으로 추가된 기능에 맞추어 리소스 로드/제거 기능이 리펙토링 되었습니다.
 * ==========================================================
 * @Jason 2025.12.20
 * <사운드 카탈로그 시스템 적용>
 * 필요한 씬에 기본적으로 사용될 사운드 리스트를 미리 정의하는 SO를 핸들링하는 기능을 추가합니다.
 * ==========================================================
 * @Jason 2025.12.28
 * <기본 클릭 사운드 시스템 추가>
 * 모든 버튼에 동일하게 사용될 기본 클릭사운드를 전역 처리해줄 기능을 구현합니다.
 * 사운드 데이터들은 필수지만 백엔드에 저장할 만큼 중요한 데이터는 아니기에 PlayerPrefs로 처리합니다.
 * ==========================================================
 * @Jason 2025.12.30
 * <볼륨 컨트롤>
 * 오디오 믹서 볼륨을 컨트롤하는 기능을 추가합니다.
 * 현재 믹서는 아래와 같이 총 4개의 채널로 구성되어 있습니다.
 * Master
 * ㄴ SFX
 * ㄴ BGM
 * ㄴ UI
 * ==========================================================
 * @Jason 2026.01.19
 * <오디오 시스템 데이터 핸들링 기능 대규모 개편>
 * 신규 데이터 핸들링 시스템(HUtil.Data)과 연동하도록 만들었습니다.
 * SoundClipProvider와 SoundUidClipLoader를 통한 신규 로딩 시스템이 구축되었습니다.
 */
#endif
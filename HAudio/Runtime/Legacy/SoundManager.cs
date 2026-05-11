using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Assertions;
using HAudio.AddOn;
using HAudio.Core;
using HAudio.Load;
using HCore;
using HUtil.Data.Load;
using HInspector;
using HDiagnosis.HDebug;
#if UNITY_EDITOR && ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace HAudio {
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
        static int PRIVATE_DEFAULT_CLICK_UID = (int)AudioClips.Click;
        public static int DEFAULT_CLICK_UID => PRIVATE_DEFAULT_CLICK_UID;

        public static void SetGlobalClickUid(AudioClips clip) => SetGlobalClickUid((int)clip);
        /// <summary> ������Ʈ ��ü ��ư Ŭ�� ���� ���� </summary>
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
        [HRequired]
        [SerializeField]
        AudioMixer audioMix;

        [HTitle("Audio Sources")]
        [HRequired]
        [SerializeField]
        AudioSource sfxAudio;
        [HRequired]
        [SerializeField]
        AudioSource bgmAudio;
        [HRequired]
        [SerializeField]
        AudioSource bgmAltAudio;
        [HRequired]
        [SerializeField]
        AudioSource uiAudio;
        [HRequired]
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
            if (ignorSameClip && bgmAudio.isPlaying && bgmAudio.clip == clip) return;

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
            // ���� ���� Ŭ���� ����ϴ� ��ü�� SoundManager ���� ��ü�̱⿡ �������� ������ �ʰ� Peek�� Ŭ�� ���.
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
/* - ���� �α� -
 * ==========================================================
 * @Jason <���� ����Ʈ ����>
 * Dictionary�� ���� ������ ���� ���ҽ��� �ʿ信 ���� O(1)�� �ӵ��� ������ ������ �ʿ䰡 �ִٰ� �����Ͽ� �ۼ��Ͽ����ϴ�.
 * Serializable Dictionary�� ���� �ν����� GUI���� ������ ������ �����ϱ� ���� ����� �õ��� �Ͽ����ϴ�.
 * ������ ������� ����ġ�� Serializable Dictionary ������� ��ǻ� ����Ʈ 2���� ����� ���������� �� �������� ����ϴ� ���� �����ϴ�.
 * �Ͽ� ����� �ʿ信 ���� ���� ������ ���Ŀ� �����Ͽ��� ����� �����ϴ�.
 * ==========================================================
 * @Jason <���Ӽ� üũ>
 * ���� ���� Ŭ���� SoundUnit ������Ʈ�� �����ϴ� ��쿡�� �߰��ǰ� �ش� ������Ʈ�� �ı��Ǹ� ������ ���� Ŭ���� �����ϴ� ����Դϴ�.
 * �̷��� ����� ������ ���� ũ���� ����ϴ� ������ �ߺ��Ǿ� ������ ���, �ϳ��� ������ �ı��� ��, �������� ����ϴ� Ŭ���� ���ŵǴ� ������ �߻��� �� �ֽ��ϴ�. �Ͽ� ���Ӽ��� ī��Ʈ�ϴ� ������ �ֱ�� �Ͽ����ϴ�.
 * ���� ���� ����� �־�����, ��ųʸ� ���ο� ���ӵǴ� ������ ���� ī��Ʈ �� �� �ִ� Ŭ������ ���� ����� ���� ���� �������� ���� ���̶� �����Ͽ� SoundItem Ŭ������ ��������ϴ�.
 * ==========================================================
 * @Jason 2025.12.18
 * <���ҽ� �ε� �ý��� �߰�>
 * UID ��� �ý������� Ȯ�� ��, ���ҽ��� �ε��ϴ� ���(Resources, Addressable, etc)�� �����ϵ��� ��� �߰� �� ���� Ŭ�� ���ҽ� ���� ��ĵ� ��Ը� �����丵�Ͽ����ϴ�.
 * Ps. SoundMajorCatelog, SoundCatalogSO, SoundPolicySO �� ���� �ʼ�
 * <���/���� ��� �߰�> 
 * ���ҽ� �ε� ����� Ȯ������ �߰��� ��ɿ� ���߾� ���ҽ� �ε�/���� ����� �����丵 �Ǿ����ϴ�.
 * ==========================================================
 * @Jason 2025.12.20
 * <���� īŻ�α� �ý��� ����>
 * �ʿ��� ���� �⺻������ ���� ���� ����Ʈ�� �̸� �����ϴ� SO�� �ڵ鸵�ϴ� ����� �߰��մϴ�.
 * ==========================================================
 * @Jason 2025.12.28
 * <�⺻ Ŭ�� ���� �ý��� �߰�>
 * ��� ��ư�� �����ϰ� ���� �⺻ Ŭ�����带 ���� ó������ ����� �����մϴ�.
 * ���� �����͵��� �ʼ����� �鿣�忡 ������ ��ŭ �߿��� �����ʹ� �ƴϱ⿡ PlayerPrefs�� ó���մϴ�.
 * ==========================================================
 * @Jason 2025.12.30
 * <���� ��Ʈ��>
 * ����� �ͼ� ������ ��Ʈ���ϴ� ����� �߰��մϴ�.
 * ���� �ͼ��� �Ʒ��� ���� �� 4���� ä�η� �����Ǿ� �ֽ��ϴ�.
 * Master
 * �� SFX
 * �� BGM
 * �� UI
 * ==========================================================
 * @Jason 2026.01.19
 * <����� �ý��� ������ �ڵ鸵 ��� ��Ը� ����>
 * �ű� ������ �ڵ鸵 �ý���(HUtil.Data)�� �����ϵ��� ��������ϴ�.
 * SoundClipProvider�� SoundUidClipLoader�� ���� �ű� �ε� �ý����� ����Ǿ����ϴ�.
 */
#endif

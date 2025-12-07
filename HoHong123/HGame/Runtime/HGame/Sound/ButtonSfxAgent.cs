using UnityEngine;
using Sirenix.OdinInspector;
using Util.Logger;
using Util.UI.ButtonUI;

namespace Util.Sound {
    [RequireComponent(typeof(DelegateButton))]
    public class ButtonSfxAgent : MonoBehaviour {
        // 공통 디폴트 클릭 사운드.
        // 프로젝트 전체에서 바꾸고 싶을 수도 있으니 static으로 노출.
        static SoundSFX defaultClip = SoundSFX.Click;

        [Title("Sound Policy")]
        [SerializeField]
        [Tooltip("true면 overrideClip을 사용하고, false면 defaultClip을 사용")]
        bool useOverride = false;
        [SerializeField, ShowIf(nameof(useOverride))]
        SoundSFX overrideClip = SoundSFX.Click;

        DelegateButton btn;


        private void Awake() {
            if (!btn) btn = GetComponent<DelegateButton>();
            if (!btn) {
                HLogger.Error("[UIButtonClickSound] DelegateButton not found.", gameObject);
                return;
            }

            btn.OnPointUp -= _HandleClickSound;
            btn.OnPointUp += _HandleClickSound;
        }

        private void OnDestroy() {
            if (btn != null) {
                btn.OnPointUp -= _HandleClickSound;
            }
        }


        /// <summary> 전역 기본 버튼 사운드를 런타임 중 교체하고 싶다면 호출 </summary>
        public static void SetGlobalDefaultClip(SoundSFX newDefault) {
            defaultClip = newDefault;
        }


        private void _HandleClickSound() {
            if (!SoundManager.HasInstance) return;
            SoundManager.Instance.PlayUI(useOverride ? overrideClip : defaultClip);
        }
    }
}

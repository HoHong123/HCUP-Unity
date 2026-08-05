using System;

namespace HUI.TextUI {
    public static class HTextLocalizer {
        /// <summary>로컬리제이션 ID로 번역된 텍스트를 반환하는 델리게이트</summary>
        public static Func<string, string> GetText;

        /// <summary>언어 변경 시 발생하는 이벤트 (languageCode)</summary>
        public static event Action<string> OnLanguageChanged;

        // Domain Reload 비활성 시 이전 플레이의 델리게이트가 잔존해 파괴된 로컬라이저를 가리키는 것을 방지.
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void _ResetStatics() {
            GetText = null;
            OnLanguageChanged = null;
        }

        /// <summary>
        /// 언어 변경을 알린다. DR2.Core 측에서 호출한다.
        /// </summary>
        public static void RaiseLanguageChanged(string languageCode) {
            OnLanguageChanged?.Invoke(languageCode);
        }
    }
}

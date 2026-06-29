using UnityEngine;
using UnityEngine.UI;

namespace HUI.TextUI {
    /// <summary>
    /// UnityEngine.UI.Text를 상속한 커스텀 텍스트 컴포넌트.
    /// 로컬리제이션 옵션을 통해 런타임에 번역된 텍스트로 교체할 수 있다.
    /// </summary>
    [AddComponentMenu("HCUP/UI/Text/HText")]
    public class HText : Text {
        #region Fields
        [SerializeField, HideInInspector]
        bool useLocalization;
        [SerializeField, HideInInspector]
        string localizationId;
        [SerializeField, HideInInspector]
        bool useOriginalText;
        [SerializeField, HideInInspector]
        OriginalTextMode originalTextMode;
        [SerializeField, HideInInspector]
        bool fitWidth;

        string cachedOriginalText;
        #endregion

        #region Unity Life Cycle
        protected override void Awake() {
            base.Awake();
            cachedOriginalText = text;

            if (useLocalization) {
                HTextLocalizer.OnLanguageChanged += _OnLanguageChanged;
                _ApplyLocalization();
            }
        }

        protected override void OnDestroy() {
            HTextLocalizer.OnLanguageChanged -= _OnLanguageChanged;
            base.OnDestroy();
        }
        #endregion

        #region Private
        private void _OnLanguageChanged(string newLanguage) {
            if (useLocalization) _ApplyLocalization();
        }

        private void _ApplyLocalization() {
            if (string.IsNullOrEmpty(localizationId)) return;
            if (HTextLocalizer.GetText == null) return;

            string localizedText = HTextLocalizer.GetText(localizationId);

            if (useOriginalText) {
                switch (originalTextMode) {
                    case OriginalTextMode.PrependOrigin:
                        text = cachedOriginalText + localizedText;
                        break;
                    case OriginalTextMode.AppendOrigin:
                        text = localizedText + cachedOriginalText;
                        break;
                    case OriginalTextMode.FormatOrigin:
                        text = string.Format(localizedText, cachedOriginalText);
                        break;
                }
            }
            else {
                text = localizedText;
            }

            _FitWidth();
        }

        private void _FitWidth() {
            if (!fitWidth) return;
            float width = preferredWidth;
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }
        #endregion
    }
}

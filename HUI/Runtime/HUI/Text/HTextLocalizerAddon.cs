using HInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HUI.TextUI {
    /// <summary>
    /// 기존 Text 컴포넌트에 애드온으로 부착하여 로컬리제이션 기능을 추가한다.
    /// Text를 상속하지 않으므로 기존 Text의 Copy/Paste Component Values가 그대로 동작한다.
    /// </summary>
    [AddComponentMenu("HCUP/UI/Text/HText Localizer Addon")]
    [RequireComponent(typeof(Text))]
    public class HTextLocalizerAddon : MonoBehaviour {
        #region Fields
        [SerializeField]
        string localizationId;
        [SerializeField]
        bool fitWidth;
        [SerializeField]
        bool useOriginalText;
        [SerializeField, HShowIf("useOriginalText")]
        OriginalTextMode originalTextMode;

        Text textComponent;
        string cachedOriginalText;
        #endregion

        #region Properties
        public string LocalizationId {
            get => localizationId;
            set {
                localizationId = value;
                _ApplyLocalization();
            }
        }
        #endregion

        #region Public
        /// <summary>
        /// 리사이클 셀에서 원본 텍스트와 로컬리제이션 ID를 갱신하고 즉시 적용한다.
        /// cachedOriginalText를 갱신하므로 FormatOrigin 모드에서 올바른 매개변수가 사용된다.
        /// </summary>
        public void Apply(string originalText, string localizeId) {
            cachedOriginalText = originalText;
            localizationId = localizeId;
            _ApplyLocalization();
        }
        #endregion

        #region Unity Life Cycle
        private void Awake() {
            textComponent = GetComponent<Text>();
            cachedOriginalText = textComponent.text;

            HTextLocalizer.OnLanguageChanged += _OnLanguageChanged;
            _ApplyLocalization();
        }

        private void OnDestroy() {
            HTextLocalizer.OnLanguageChanged -= _OnLanguageChanged;
        }
        #endregion

        #region Private
        private void _OnLanguageChanged(string newLanguage) {
            _ApplyLocalization();
        }

        private void _ApplyLocalization() {
            if (textComponent == null) return;
            if (string.IsNullOrEmpty(localizationId)) return;
            if (HTextLocalizer.GetText == null) return;

            string localizedText = HTextLocalizer.GetText(localizationId);

            if (useOriginalText) {
                switch (originalTextMode) {
                    case OriginalTextMode.PrependOrigin:
                        textComponent.text = cachedOriginalText + localizedText;
                        break;
                    case OriginalTextMode.AppendOrigin:
                        textComponent.text = localizedText + cachedOriginalText;
                        break;
                    case OriginalTextMode.FormatOrigin:
                        textComponent.text = string.Format(localizedText, cachedOriginalText);
                        break;
                }
            }
            else {
                textComponent.text = localizedText;
            }

            _FitWidth();
        }

        private void _FitWidth() {
            if (!fitWidth || textComponent == null) return;
            float width = textComponent.preferredWidth;
            textComponent.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }
        #endregion
    }
}

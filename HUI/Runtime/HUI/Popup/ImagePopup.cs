using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using HInspector;
using HResource.Data;
using HResource.Provider;
using HDiagnosis.Logger;

namespace HUI.Popup {
    public class ImagePopup : BasePopupUi {
        #region Fields
        [HTitle("Viewport")]
        [SerializeField]
        RectTransform viewRect;
        [SerializeField]
        RectTransform contentRect;
        [SerializeField]
        RectTransform rawRect;

        [HTitle("Image")]
        [SerializeField]
        RawImage rawImg;

        [HTitle("Button")]
        [SerializeField]
        Button panelBtn;

        // AssetProvider
        // - Resources/Addressable 에셋의 실제 Load/Cache/Validate/Release 를 소유.
        // - OnDestroy의 ReleaseOwner(this)로 이 인스턴스 소유 자산을 일괄 회수한다.
        // - 그것을 잊어도 provider 의 파괴 프로브가 같은 회수를 수행한다.
        // currentMode / currentKey
        // - 직전 로드 요청의 (mode, key). 새 요청 시 이전 자원을 Release
        // - "단 하나의 스프라이트만 유지" 제약을 단순 필드 교체로 보장한다.
        IAssetSource<string, Sprite> resourcesProvider;
        IAssetSource<string, Sprite> addressableProvider;
        AssetLoadMode? currentMode;
        string currentKey;
        #endregion

        #region Events
        public event Action OnClickPanel;
        #endregion


        #region Unity Lifecycle
        protected override void Start() {
            base.Start();
            panelBtn.onClick.AddListener(_HandlePanelClicked);
        }

        protected override void OnDestroy() {
            base.OnDestroy();

            panelBtn.onClick.RemoveAllListeners();

            // 정상 플로우로 자기 몫을 먼저 내려놓는다. 이 호출을 빠뜨려도 파괴 프로브가
            // 같은 회수를 수행하지만, 명시적 반납이 기본 경로다.
            resourcesProvider?.ReleaseOwner(this);
            addressableProvider?.ReleaseOwner(this);
            // 두 provider 모두 이 팝업이 지연 생성한 것이라 폐기 책임도 여기에 있다.
            resourcesProvider?.Dispose();
            addressableProvider?.Dispose();
            resourcesProvider = null;
            addressableProvider = null;
        }
        #endregion

        #region Public - UI Update
        public void SetUi(Sprite spt) => _DisplaySpriteRatio(spt);
        public void SetUi(Texture texture) => _DisplaySpriteRatio(texture);

        public async UniTask SetUiFromResourcesAsync(string fullPath) {
            if (string.IsNullOrEmpty(fullPath)) {
                HLogger.Error("[ImagePopup] fullPath is null or empty.");
                return;
            }
            await _LoadAndApplyAsync(_EnsureResourcesProvider(), fullPath, AssetLoadMode.Resources);
        }

        public async UniTask SetUiFromAddressableAsync(string address) {
            if (string.IsNullOrEmpty(address)) {
                HLogger.Error("[ImagePopup] address is null or empty.");
                return;
            }
            await _LoadAndApplyAsync(_EnsureAddressableProvider(), address, AssetLoadMode.Addressable);
        }
        #endregion

        #region Private - Asset Handling
        private IAssetSource<string, Sprite> _EnsureResourcesProvider() {
            if (resourcesProvider == null) {
                resourcesProvider = AssetProviderFactory.CreateResources<Sprite>(resourcesRootPath: string.Empty);
            }
            return resourcesProvider;
        }

        private IAssetSource<string, Sprite> _EnsureAddressableProvider() {
            if (addressableProvider == null) {
                addressableProvider = AssetProviderFactory.CreateAddressable<Sprite>();
            }
            return addressableProvider;
        }

        private async UniTask _LoadAndApplyAsync(
            IAssetSource<string, Sprite> provider,
            string key,
            AssetLoadMode mode) {

            _ReleasePreviousIfAny();

            var sprite = await provider.GetAsync(this, key, mode, AssetFetchMode.CacheFirst);
            if (sprite == null) {
                HLogger.Error($"[ImagePopup] Failed to load sprite. mode={mode}, key={key}");
                return;
            }
            currentMode = mode;
            currentKey = key;
            _DisplaySpriteRatio(sprite);
        }

        private void _ReleasePreviousIfAny() {
            if (currentKey == null || !currentMode.HasValue) return;

            var provider = currentMode.Value == AssetLoadMode.Resources
                ? resourcesProvider
                : addressableProvider;
            provider?.Release(this, currentKey);

            currentMode = null;
            currentKey = null;
        }
        #endregion

        #region Private - Sprite Display
        private void _DisplaySpriteRatio(Sprite sprite) => _DisplaySpriteRatio(sprite.texture);
        private void _DisplaySpriteRatio(Texture texture) {
            float textureWidth = texture.width;
            float textureHeight = texture.height;
            float viewWidth = viewRect.rect.width;
            float viewHeight = viewRect.rect.height;
            float scaleFactor = viewRect.rect.width / textureWidth;
            float newHeight = texture.height * scaleFactor;

            rawImg.texture = texture;
            rawRect.sizeDelta = new Vector2(viewWidth, newHeight);
            rawRect.anchoredPosition = Vector2.zero;

            contentRect.pivot = new Vector2(0, (newHeight > viewHeight) ? 1 : 0.5f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, Mathf.Max(newHeight, viewHeight));
        }
        #endregion

        #region Private - Event Handlers
        private void _HandlePanelClicked() {
            OnClickPanel?.Invoke();
        }
        #endregion
    }
}

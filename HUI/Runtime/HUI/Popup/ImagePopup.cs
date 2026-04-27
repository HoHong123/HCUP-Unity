using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using HInspector;
using HUtil.AssetHandler.Data;
using HUtil.AssetHandler.Provider;
using HUtil.AssetHandler.Subscription;
using HDiagnosis.Logger;

namespace HUI.Popup {
    public class ImagePopup : BasePopupUi, IAssetOwner {
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
        // - OnDestroy의 ReleaseOwner(ownerId)로 이 인스턴스 소유 자산을 일괄 회수한다.
        // currentMode / currentKey
        // - 직전 로드 요청의 (mode, key). 새 요청 시 이전 자원을 Release
        // - "단 하나의 스프라이트만 유지" 제약을 단순 필드 교체로 보장한다.
        AssetProvider<string, Sprite> resourcesProvider;
        AssetProvider<string, Sprite> addressableProvider;
        AssetLoadMode? currentMode;
        string currentKey;
        #endregion

        #region Events
        public event Action OnClickPanel;
        #endregion

        #region Properties
        AssetOwnerId ownerId;
        public AssetOwnerId OwnerId {
            get {
                if (!ownerId.IsValid) ownerId = AssetOwnerIdGenerator.NewId(this);
                return ownerId;
            }
        }
        #endregion

        #region Unity Lifecycle
        protected override void Start() {
            base.Start();
            panelBtn.onClick.AddListener(_HandlePanelClicked);
        }

        protected override void OnDestroy() {
            panelBtn.onClick.RemoveAllListeners();
            OnClickPanel = null;

            resourcesProvider?.ReleaseOwner(ownerId);
            addressableProvider?.ReleaseOwner(ownerId);
            if (ownerId.IsValid) AssetOwnerIdGenerator.NotifyReleased(ownerId);

            base.OnDestroy();
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
        private AssetProvider<string, Sprite> _EnsureResourcesProvider() {
            if (resourcesProvider == null) {
                resourcesProvider = AssetProviderFactory.CreateResources<Sprite>(resourcesRootPath: string.Empty);
            }
            return resourcesProvider;
        }

        private AssetProvider<string, Sprite> _EnsureAddressableProvider() {
            if (addressableProvider == null) {
                addressableProvider = AssetProviderFactory.CreateAddressable<Sprite>();
            }
            return addressableProvider;
        }

        private async UniTask _LoadAndApplyAsync(
            AssetProvider<string, Sprite> provider,
            string key,
            AssetLoadMode mode) {

            _ReleasePreviousIfAny();

            var sprite = await provider.GetAsync(key, mode, AssetFetchMode.CacheFirst, OwnerId);
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
            provider?.Release(currentKey, ownerId);

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

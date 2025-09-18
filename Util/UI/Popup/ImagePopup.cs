using System;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

namespace Util.UI.Popup {
    public class ImagePopup : BasePopupUi {
        [Title("Viewport")]
        [SerializeField]
        RectTransform viewRect;
        [SerializeField]
        RectTransform contentRect;
        [SerializeField]
        RectTransform rawRect;

        [Title("Image")]
        [SerializeField]
        RawImage rawImg;

        [Title("Button")]
        [SerializeField]
        Button panelBtn;

        public event Action OnClickPanel;


        protected override void Start() {
            base.Start();
            panelBtn.onClick.AddListener(() => OnClickPanel?.Invoke());
            closeBtn.onClick.AddListener(() => Destroy(panel));
        }


        public void SetUi(Sprite spt) => _DisplaySpriteRatio(spt);
        public void SetUi(Texture texture) => _DisplaySpriteRatio(texture);
        public void SetUi(string resourcePath) => SetUi(Resources.Load<Sprite>(resourcePath));


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
    }
}
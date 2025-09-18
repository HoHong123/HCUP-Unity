using System;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using DG.Tweening;

namespace Util.UI.Entity {
    [Serializable]
    public class ColorUiEntity {
        [Title("Option")]
        [SerializeField]
        bool changeSprite = false;
        [HideIf(nameof(changeSprite)), SerializeField]
        bool useAnimation = false;
        [ShowIf("@!this.changeSprite && this.useAnimation"), SerializeField]
        float animationDuration = 0.2f;

        [Title("Color")]
        [OnValueChanged(nameof(_Init))]
        [HideIf(nameof(changeSprite)), SerializeField]
        MaskableGraphic graphic;
        [HideIf(nameof(changeSprite)), SerializeField]
        Color originColor;
        [HideIf(nameof(changeSprite)), SerializeField]
        Color targetColor;

        [Title("Sprite")]
        [OnValueChanged(nameof(_Init))]
        [ShowIf(nameof(changeSprite)), SerializeField]
        Image image;
        [ShowIf(nameof(changeSprite)), SerializeField]
        Sprite originSprite;
        [ShowIf(nameof(changeSprite)), SerializeField]
        Sprite targetSprite;


        private void _Init() {
            if (graphic == null && image == null) return;

            if (changeSprite) {
                if (image == null) return;
                graphic = image;
                originColor = image.color;
                originSprite = image.sprite;
            }
            else {
                if (graphic != null && graphic is Image) {
                    image = graphic as Image;
                    originSprite = image.sprite;
                }
                else {
                    originSprite = null;
                    targetSprite = null;
                }
                originColor = graphic.color;
            }
        }


        public void SetColor(Color Original, Color Target) {
            originColor = Original;
            targetColor = Target;
        }

        public void Reset() {
            if (changeSprite)
                image.sprite = originSprite;
            else
                _Dye(originColor);
        }

        public void Dye() {
            if (changeSprite)
                image.sprite = targetSprite;
            else
                _Dye(targetColor);
        }


        private void _Dye(Color color) {
            if (useAnimation) {
                graphic.DOKill();
                graphic.DOColor(color, animationDuration);
            }
            else {
                graphic.color = color;
            }
        }
    }
}
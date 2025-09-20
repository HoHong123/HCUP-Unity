using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
#if !ODIN_INSPECTOR
using System;
using Util.OdinCompat;
#endif

namespace Util.UI.Entity {
    public partial class ColorUiEntity {
#if !ODIN_INSPECTOR
        [HeaderOrTitle("Option")]
        [SerializeField]
        bool changeSprite = false;
        [SerializeField]
        bool useAnimation = false;
        [SerializeField]
        float animationDuration = 0.2f;

        [HeaderOrTitle("Color")]
        [SerializeField]
        MaskableGraphic graphic;
        [SerializeField]
        Color originColor;
        [SerializeField]
        Color targetColor;

        [HeaderOrTitle("Sprite")]
        [SerializeField]
        Image image;
        [SerializeField]
        Sprite originSprite;
        [SerializeField]
        Sprite targetSprite;
#endif

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
                //graphic.DOColor(color, animationDuration);
            }
            else {
                graphic.color = color;
            }
        }
    }
}
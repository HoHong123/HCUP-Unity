#if UNITY_EDITOR
/* =========================================================
 * 이 스크립트는 UI Graphic 또는 Image를 대상으로 색상 및 스프라이트 변경 상태를
 * 관리하는 엔티티 클래스입니다.
 *
 * 주의사항 ::
 * 1. changeSprite가 활성화되면 Sprite 변경 모드로 동작하며, image 참조가 반드시 유효해야 합니다.
 * 2. changeSprite가 비활성화되면 Color 변경 모드로 동작하며, graphic 참조가 반드시 유효해야 합니다.
 * 3. useAnimation은 Color 변경 모드에서만 동작하며, 비활성 GameObject에는 Tween 애니메이션을 적용하지 않습니다.
 * 4. useDynamicPressTint는 originColor를 기준으로 targetColor를 계산하므로 originColor 초기화 상태가 중요합니다.
 * 5. Image와 Graphic을 혼용하는 구조이므로 참조 대상 변경 시 _Init() 결과가 의도와 일치하는지 확인해야 합니다.
 * =========================================================
 */
#endif

using System;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using DG.Tweening;

namespace HUI.Entity {
    [Serializable]
    public class ColorUiEntity {
        #region Options
        /// Option to change the color change target to a color or sprite.
        #region Change Sprite Option
        [Title("Option")]
        [SerializeField]
        bool changeSprite = false;
        #endregion

        /// Additional coloring animation options using the Dotween package.
        #region Use Animation Option
        [HideIf(nameof(changeSprite))]
        [SerializeField]
        bool useAnimation = false;
        [ShowIf("@!this.changeSprite && this.useAnimation")]
        [SerializeField]
        float animationDuration = 0.2f;
        #endregion

        // Feature option to add a tint effect of a given color rather than a fixed color.
        #region Tint Option
        [Title("Dynamic Press Tint")]
        [SerializeField]
#if UNITY_EDITOR
        [OnValueChanged(nameof(_RefreshTargetColorInEditor))]
#endif
        bool useDynamicPressTint = false;

        [ShowIf(nameof(useDynamicPressTint))]
        [SerializeField]
#if UNITY_EDITOR
        [OnValueChanged(nameof(_RefreshTargetColorInEditor))]
#endif
        ColorTintMode pressTintMode = ColorTintMode.Darker;

        [ShowIf(nameof(useDynamicPressTint))]
        [SerializeField]
        [Range(0f, 0.5f)]
#if UNITY_EDITOR
        [OnValueChanged(nameof(_RefreshTargetColorInEditor))]
#endif
        float pressValueDelta = 0.12f;
        #endregion
        #endregion

        #region Entities
        #region Color Entity
        [Title("Color")]
        [OnValueChanged(nameof(_Init))]
        [HideIf(nameof(changeSprite)), SerializeField]
        MaskableGraphic graphic;
        [HideIf(nameof(changeSprite)), SerializeField]
#if UNITY_EDITOR
        [OnValueChanged(nameof(_RefreshTargetColorInEditor))]
#endif
        Color originColor;
        [HideIf(nameof(changeSprite)), SerializeField]
        Color targetColor;
        #endregion

        #region Sprite Entity
        [Title("Sprite")]
        [OnValueChanged(nameof(_Init))]
        [ShowIf(nameof(changeSprite)), SerializeField]
        Image image;
        [ShowIf(nameof(changeSprite)), SerializeField]
        Sprite originSprite;
        [ShowIf(nameof(changeSprite)), SerializeField]
        Sprite targetSprite;
        #endregion
        #endregion

        #region Properties
        public MaskableGraphic Graphic => graphic;
        #endregion

        #region Init
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
        #endregion

        #region Coloring
        public void SetColor(Color original, bool immediate = false) {
            var target = useDynamicPressTint ? _ComputePressColor(original) : original;
            SetColor(original, target, immediate);
        }

        public void SetColor(Color original, Color target, bool immediate = false) {
            originColor = original;
            targetColor = target;
        }

        public void Reset(bool immediate = false) {
            if (changeSprite) {
                image.sprite = originSprite;
            }
            else {
                _Dye(originColor, immediate);
            }
        }

        public void Dye(bool immediate = false) {
            if (changeSprite) {
                image.sprite = targetSprite;
            }
            else {
                _Dye(targetColor, immediate);
            }
        }


        private bool _CanAnimate() {
            if (!useAnimation) return false;
            if (graphic == null) return false;
            return graphic.gameObject.activeInHierarchy;
        }

        private void _Dye(Color color, bool immediate = false) {
            if (_CanAnimate() && !immediate) {
                graphic.DOKill();
                graphic.DOColor(color, animationDuration);
                return;
            }

            graphic.color = color;
        }

        private Color _ComputePressColor(Color baseColor) {
            var mode = pressTintMode;
            if (mode == ColorTintMode.Auto) {
                float lum = 0.2126f * baseColor.r + 0.7152f * baseColor.g + 0.0722f * baseColor.b;
                mode = lum >= 0.5f ? ColorTintMode.Darker : ColorTintMode.Lighter;
            }

            Color.RGBToHSV(baseColor, out float h, out float s, out float v);
            float delta = pressValueDelta * (mode == ColorTintMode.Darker ? -1f : 1f);
            v = Mathf.Clamp01(v + delta);

            var target = Color.HSVToRGB(h, s, v);
            target.a = baseColor.a;
            return target;
        }

#if UNITY_EDITOR
        private void _RefreshTargetColorInEditor() {
            if (changeSprite) return;

            targetColor = useDynamicPressTint
                ? _ComputePressColor(originColor)
                : originColor;

            if (graphic != null) UnityEditor.EditorUtility.SetDirty(graphic);
        }
#endif
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH 2026.03.10
 * 
 * 주요 기능 ::
 * 1. UI 대상의 원본 색상(originColor)과 목표 색상(targetColor)을 분리하여 관리합니다.
 * 2. 필요 시 스프라이트(originSprite / targetSprite) 전환 방식으로도 동작할 수 있습니다.
 * 3. useAnimation 활성화 시 DOTween을 사용하여 색상 전환 애니메이션을 재생합니다.
 * 4. useDynamicPressTint 활성화 시 originColor를 기준으로 자동 tint 색상을 계산합니다.
 * 5. Reset()으로 원본 상태 복원, Dye()로 목표 상태 적용을 수행합니다.
 *
 * 사용법 ::
 * 1. Color 변경 모드에서는 graphic을 연결하고 changeSprite를 비활성화합니다.
 * 2. Sprite 변경 모드에서는 image를 연결하고 changeSprite를 활성화합니다.
 * 3. SetColor(original) 또는 SetColor(original, target)으로 원본/목표 색상을 설정합니다.
 * 4. Dye() 호출 시 목표 상태를 적용하고, Reset() 호출 시 원본 상태로 복구합니다.
 * 5. 버튼 눌림 연출처럼 동적 tint가 필요하면 useDynamicPressTint를 활성화하고 pressTintMode, pressValueDelta를 조정합니다.
 *
 * 기타 ::
 * 1. Color 변경 모드에서 graphic이 Image인 경우 image 참조도 함께 캐싱합니다.
 * 2. Sprite 변경 모드에서는 graphic = image로 동기화하여 공통 접근을 유지합니다.
 * 3. _RefreshTargetColorInEditor()는 에디터에서 targetColor를 즉시 갱신하기 위한 보조 함수입니다.
 * 4. SetColor(Color original, Color target, bool immediate = false)의 immediate 매개변수는 현재 내부에서 사용되지 않습니다.
 * =========================================================
 */
#endif
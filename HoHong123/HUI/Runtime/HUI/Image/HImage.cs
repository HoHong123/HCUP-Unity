#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Unity UI Image를 확장한 커스텀 이미지 컴포넌트입니다.
 *
 * 주요 기능 ::
 * 1. Sprite Pivot 자동 정렬 기능
 *  - Sprite의 Pivot 값을 기준으로 RectTransform 위치를 자동 보정합니다.
 *  - 다양한 Pivot을 가진 스프라이트를 동일한 기준 위치에 정렬할 수 있습니다.
 * 2. Base Position Bake 기능
 *  - 현재 RectTransform 위치를 기준 위치(Base Position)로 저장합니다.
 *  - 이후 Pivot 정렬 시 기준 위치를 유지한 상태로 Offset이 적용됩니다.
 * 3. Alpha Hit Test 기능
 *  - Sprite의 알파값을 기준으로 Raycast 판정을 수행합니다.
 *  - 투명 영역 클릭을 방지할 수 있습니다.
 *
 * 주요 사용 목적 ::
 * 1. Pivot이 다른 스프라이트 교체 시 UI 위치가 흔들리는 문제 해결
 * 2. UI 이미지의 투명 영역 클릭 방지
 * 3. Sprite 교체 기반 UI (아이콘, 장비 이미지 등) 정렬 유지
 *
 * 동작 방식 ::
 * OnEnable / OnValidate 시
 *  - Pivot Align 옵션이 활성화되어 있으면 자동 정렬 수행
 *  - Alpha Hit Test 옵션이 활성화되어 있으면 Raycast Threshold 적용
 *
 * 특징 ::
 * - Unity Image를 상속한 확장 UI 컴포넌트
 * - Sprite Pivot 기준 자동 위치 보정
 * - Alpha 기반 Raycast 처리 지원
 * =========================================================
 */
#endif

using HUtil.Inspector;
using HUtil.Logger;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace HUI.ImageUI {
    public class HImage : Image {
        #region Members (IMGUI)
        [HTitle("Pivot Align")]
        [SerializeField]
        bool alignOnSpriteChanged = false;
        [SerializeField]
        bool useCustomBasePosition = true;
        [SerializeField, HideInInspector]
        Vector2 baseAnchoredPosition;
        [SerializeField, HideInInspector]
        bool hasBase;

        [HTitle("Raycast (Alpha Hit Test)")]
        [SerializeField]
        bool useAlphaHitTest;
        [SerializeField, Range(0f, 1f)]
        float alphaHitThreshold = 0.2f;
        #endregion

        #region Protected - Unity Life Cycle
        protected override void OnEnable() {
            base.OnEnable();
            if (alignOnSpriteChanged) AlignToSpritePivot();
            if (useAlphaHitTest) _ApplyAlphaHitTest();
        }

#if UNITY_EDITOR
        protected override void OnValidate() {
            base.OnValidate();
            if (alignOnSpriteChanged) AlignToSpritePivot();
            if (useAlphaHitTest) _ApplyAlphaHitTest();
        }
#endif
        #endregion

        #region Public - Bake Sprite Offset
        public void BakeBasePosition() {
            baseAnchoredPosition = rectTransform.anchoredPosition;
            hasBase = true;
        }

        public void AlignToSpritePivot() {
            if (sprite == null) return;
            if (!hasBase || !useCustomBasePosition) {
                baseAnchoredPosition = rectTransform.anchoredPosition;
                hasBase = true;
            }

            Vector2 offset = _CalcOffsetPx(sprite, rectTransform.rect.size, rectTransform.pivot);
            rectTransform.anchoredPosition = baseAnchoredPosition + offset;
        }
        #endregion

        #region Private - Sprite Offset
        private static Vector2 _CalcOffsetPx(Sprite sprite, Vector2 rectSize, Vector2 refPivot01) {
            Assert.IsNotNull(sprite);

            Rect sr = sprite.rect;
            float width = sr.width;
            float height = sr.height;

            if (width <= 0f || height <= 0f) return Vector2.zero;

            // sprite.pivot: 픽셀 좌표(좌하단 기준)
            Vector2 pivot01 = new Vector2(sprite.pivot.x / width, sprite.pivot.y / height);

            // referencePivot(예: 중앙 0.5,0.5)에 pivot을 맞추기 위한 델타
            Vector2 delta01 = refPivot01 - pivot01;

            // RectTransform 크기 기준 픽셀 오프셋
            return new Vector2(delta01.x * rectSize.x, delta01.y * rectSize.y);
        }
        #endregion

        #region Private - Alpha Hit
        public void ApplyAlphaHitTest() {
            _ApplyAlphaHitTest();
        }

        private void _ApplyAlphaHitTest() {
            alphaHitTestMinimumThreshold = useAlphaHitTest ? Mathf.Clamp01(alphaHitThreshold) : 0f;
#if UNITY_EDITOR
            if (!useAlphaHitTest) return;
            if (sprite == null) return;

            var tex = sprite.texture;
            if (tex == null) return;
            if (!tex.isReadable) {
                HLogger.Warning(
                    $"[HImage] Alpha Hit Test가 활성화되어 있으나, Texture Read/Write가 꺼져있습니다. (Sprite: {sprite.name})",
                    gameObject);
            }
#endif
        }
        #endregion
    }
}

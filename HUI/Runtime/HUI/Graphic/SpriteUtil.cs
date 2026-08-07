using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace HUI.Graphic {

    public static class SpriteUtil {
        /// <summary>
        /// Image의 스프라이트 pivot이 rectTransform의 referencePivot 위치에 오도록 anchoredPosition을 보정한다.
        /// - referencePivot: RectTransform 내부 기준점 (0~1). 보통 (0.5,0.5) 또는 (0,0) 등.
        /// - 주의: LayoutGroup이 붙어 있으면 매 프레임 재적용되거나 깨질 수 있음.
        /// </summary>
        public static void AlignPivot(this Image image, Vector2 referencePivot) {
            Assert.IsNotNull(image);
            Assert.IsNotNull(image.rectTransform);

            var sprite = image.sprite;
            if (sprite == null) return;

            RectTransform rt = image.rectTransform;

            // 스프라이트 크기(픽셀)
            float width = sprite.rect.width;
            float height = sprite.rect.height;

            if (width <= 0f || height <= 0f) return;

            // 스프라이트 pivot (픽셀, 좌하단 기준)
            Vector2 pivotPx = sprite.pivot;

            // pivot을 0~1 정규화
            Vector2 pivot01 = new Vector2(pivotPx.x / width, pivotPx.y / height);

            // RectTransform 내부의 referencePivot 위치에서, sprite pivot이 떨어진 만큼(정규화)
            Vector2 delta01 = referencePivot - pivot01;

            // RectTransform 픽셀 크기 기준으로 offset 계산
            Vector2 rectSize = rt.rect.size;
            Vector2 offset = new Vector2(delta01.x * rectSize.x, delta01.y * rectSize.y);

            rt.anchoredPosition += offset;
        }
    }
}

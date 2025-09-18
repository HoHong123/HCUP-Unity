#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 벡터 추가 기능 모음 스크립트입니다.
 * =========================================================
 */
#endif

using UnityEngine;

namespace Util.Formattable {
    public static class VectorUtil {
        public static Vector2 GetRandomPositionWithin(this RectTransform rectTransform, Vector2 padding = default) {
            Vector2 size = rectTransform.rect.size;
            Vector2 pivot = rectTransform.pivot;

            float minX = -size.x * pivot.x + padding.x;
            float maxX = size.x * (1 - pivot.x) - padding.x;
            float minY = -size.y * pivot.y + padding.y;
            float maxY = size.y * (1 - pivot.y) - padding.y;

            float randomX = Random.Range(minX, maxX);
            float randomY = Random.Range(minY, maxY);

            return new Vector2(randomX, randomY);
        }


        public static Vector2 GetCanvasPosition(this Transform _target, Camera _camera) {
            return _camera.WorldToScreenPoint(_target.position);
        }
    }
}
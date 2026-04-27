#if UNITY_EDITOR
/* =========================================================
 * Vector 관련 유틸리티 함수 모음입니다.
 *
 * 목적 ::
 * UI 위치 계산 및 방향 벡터 계산을 간단하게 하기 위함입니다.
 * =========================================================
 */
#endif

using UnityEngine;


namespace HData.Mathx {
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

        public static Vector2 DegreeToDirection(this float deg) {
            float rad = deg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH 2026.03.10
 *
 * 주요 기능 ::
 * GetRandomPositionWithin
 *  + RectTransform 내부 랜덤 위치 반환
 * GetCanvasPosition
 *  + World → Screen 좌표 변환
 * DegreeToDirection
 *  + 각도 → 방향 벡터
 *
 * 사용법 ::
 * float angle = 90f;
 * Vector2 dir = angle.DegreeToDirection();
 *
 * 기타 ::
 * UI / Gameplay 계산 유틸리티입니다.
 * =========================================================
 */
#endif
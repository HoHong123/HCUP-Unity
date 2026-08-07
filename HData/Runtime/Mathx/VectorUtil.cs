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

        // 이전 이름 GetCanvasPosition 은 스크린 좌표(WorldToScreenPoint)를 반환하면서
        // "Canvas 좌표" 를 반환하는 것처럼 오독됐다. 호출처 0건 확인 후 이름을 실제 동작에 맞춘다.
        public static Vector2 GetScreenPosition(this Transform target, Camera camera) {
            return camera.WorldToScreenPoint(target.position);
        }

        public static Vector2 DegreeToDirection(this float deg) {
            float rad = deg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }
    }
}

#if UNITY_EDITOR
/* =========================================================
 * Dev Log
 * =========================================================
 * 2026-08-07 (수정) :: GetCanvasPosition → GetScreenPosition 개명
 * =========================================================
 * 변경 ::
 * - `GetCanvasPosition(Transform, Camera)` → `GetScreenPosition(Transform, Camera)`.
 *   본문(Camera.WorldToScreenPoint)은 그대로 유지, 이름만 실제 동작에 맞춤.
 *
 * 이유 ::
 * 함수명은 "Canvas 좌표 변환" 을 암시했지만 실제로는 스크린 좌표를 반환해 이름과 동작이
 * 불일치했다. 패키지 전역 grep 결과 호출처 0건(문서 언급만 존재)이라 구현 유지 + 개명으로
 * 정정. Canvas 좌표(RectTransformUtility.ScreenPointToLocalPointInRectangle 등)가
 * 필요하면 별도 API 로 새로 추가할 것.
 * =========================================================
 * @Jason - PKH 2026.03.10
 *
 * 주요 기능 ::
 * GetRandomPositionWithin
 *  + RectTransform 내부 랜덤 위치 반환
 * GetScreenPosition
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
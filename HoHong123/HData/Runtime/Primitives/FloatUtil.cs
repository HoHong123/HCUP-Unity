#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Float 연산 관련 유틸리티 함수 모음입니다.
 * =========================================================
 */
#endif

using UnityEngine;

namespace HUtil.Primitives {
    public static class FloatUtil {
        public static float MidAngleDegree(this float a, float b) {
            float mid = a + Mathf.DeltaAngle(a, b) * 0.5f;
            return Mathf.Repeat(mid + 180f, 360f) - 180f;
        }
    }
}
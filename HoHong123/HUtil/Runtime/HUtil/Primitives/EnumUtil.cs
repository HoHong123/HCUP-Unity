#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Enum 관련 유틸리티 함수 모음입니다.
 * =========================================================
 */
#endif

using System;
using System.Collections.Generic;

namespace HUtil.Primitives {
    public static class EnumUtil {
        public static IEnumerable<T> GetValues<T>() where T : Enum {
            return (T[])Enum.GetValues(typeof(T));
        }
    }
}
#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Transform 추가 기능을 선언하는 스크립트입니다.
 * =========================================================
 */
#endif

using UnityEngine;

namespace HUtil.Core {
    public static class TransformExtension {
        public static void DestroyAllChildren(this Transform parent) {
            if (parent == null) return;
            for (int k = parent.childCount - 1; k >= 0; k--) {
                Object.Destroy(parent.GetChild(k).gameObject);
            }
        }
    }
}

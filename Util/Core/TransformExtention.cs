#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Transform 추가 기능을 선언하는 스크립트입니다.
 * =========================================================
 */
#endif

using UnityEngine;

public static class TransformExtension {
    public static void DestroyAllChildren(this Transform parent) {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--) {
            Object.Destroy(parent.GetChild(i).gameObject);
        }
    }
}

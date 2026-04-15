#if UNITY_EDITOR
/* =========================================================
 * Transform에 대한 확장 기능을 제공하는 유틸리티 클래스입니다.
 * Transform 하위 오브젝트를 일괄 관리하는 기능을 제공합니다.
 * =========================================================
 */
#endif

using UnityEngine;

namespace HUtil.Core {
    public static class TransformExtension {
        public static void DestroyAllChildren(this Transform parent) {
            if (parent == null) return;
            for (int k = parent.childCount - 1; k >= 0; k--) {
#if UNITY_EDITOR
                Object.DestroyImmediate(parent.GetChild(k).gameObject);
#else
                Object.Destroy(parent.GetChild(k).gameObject);
#endif
            }
        }
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH 2026.03.10
 *
 * 주요 기능 ::
 * 1. DestroyAllChildren
 *    + Transform의 모든 자식 GameObject 제거
 *
 * 사용법 ::
 * 1. Transform 객체에서 Extension Method 형태로 호출합니다.
 * 예시 ::
 * parentTransform.DestroyAllChildren();
 *
 * 기타 ::
 * 1. Editor 환경에서는 DestroyImmediate를 사용합니다.
 * 2. Runtime 환경에서는 Destroy를 사용합니다.
 * =========================================================
 */
#endif
#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 2D 환경에서 특정 오브젝트를 따라다니는 카메라 이펙트 스크립트입니다.
 * =========================================================
 */
#endif

using UnityEngine;
using Sirenix.OdinInspector;

namespace HGame._2D.Cam {
    [DisallowMultipleComponent]
    public class CameraFollow : MonoBehaviour {
        [Title("Camear")]
        [SerializeField]
        Camera cam;
        [SerializeField, Range(0f, 1f)]
        float smooth = 0.15f;
        [SerializeField]
        float zPos = -10f;

        [Title("Target")]
        [SerializeField]
        Transform target;
        [SerializeField, Required]
        Transform originalTarget;

        [Title("Bounds")]
        [SerializeField]
        MapBoundType boundType;
        [SerializeField, ShowIf("boundType", MapBoundType.WorldBox)]
        BoxCollider2D worldBoundsB2D;
        [SerializeField, ShowIf("boundType", MapBoundType.Absolute)]
        Rect absolutBound;

#if UNITY_EDITOR
        [Title("Debug")]
        [SerializeField]
        protected bool useDebug = true;
        [SerializeField, ShowIf("useDebug")]
        protected Color debugColor = Color.cyan;
#endif

        bool hasRect = false;
        Rect worldRect = default;
        Vector3 velocity = default;


        private void Awake() {
            _RefreshWorldRect();
        }

        private void LateUpdate() {
            if (!cam || !hasRect) return;

            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;

            float minX = worldRect.xMin + halfW;
            float maxX = worldRect.xMax - halfW;
            float minY = worldRect.yMin + halfH;
            float maxY = worldRect.yMax - halfH;

            Vector3 desired = target.position;
            float newX = Mathf.Clamp(desired.x, minX, maxX);
            float newY = Mathf.Clamp(desired.y, minY, maxY);

            Vector3 dest = new Vector3(newX, newY, zPos);
            cam.transform.position = (smooth <= 0f)  ? dest : Vector3.SmoothDamp(cam.transform.position, dest, ref velocity, smooth);
        }


        public void ResetTarget() => target = originalTarget;
        public void SetPosition(Vector3 position) => target.position = position;
        public void SetPosition(Transform target) => this.target = target;


        private void _RefreshWorldRect() {
            hasRect = true;
            switch (boundType) {
            case MapBoundType.WorldBox:
                if (worldBoundsB2D) {
                    var b = worldBoundsB2D.bounds;
                    worldRect = new Rect(b.min, b.size);
                }
                break;
            case MapBoundType.Absolute:
                if (absolutBound.size != Vector2.zero) {
                    worldRect = absolutBound;
                }
                break;
            default:
                hasRect = false;
                break;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected() {
            if (!useDebug) return;
            switch (boundType) {
            case MapBoundType.WorldBox:
                if (worldBoundsB2D) {
                    var b = worldBoundsB2D.bounds;
                    Gizmos.color = debugColor;
                    Gizmos.DrawWireCube(b.center, b.size);
                }
                break;
            case MapBoundType.Absolute:
                if (absolutBound.size != Vector2.zero) {
                    Gizmos.color = debugColor;
                    Gizmos.DrawWireCube((Vector3)absolutBound.center, (Vector3)absolutBound.size);
                }
                break;
            default:
                hasRect = false;
                break;
            }
        }
#endif
    }
}

#if UNITY_EDITOR
/* Dev Log
 * @Jason - PKH 18. 09. 25
 * 1. 디버깅 시스템 업데이트
 * + 디버그 활성화 기능 추가
 * + 디버그 컬러 설정 가능
 */
#endif
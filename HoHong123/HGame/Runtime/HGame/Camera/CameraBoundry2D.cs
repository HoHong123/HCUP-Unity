using UnityEngine;
using Sirenix.OdinInspector;

namespace HGame.Cam {
    [DisallowMultipleComponent]
    public class CameraBoundry2D : BaseCameraBoundry {
        #region Fields
        [Title("2D Camera")]
        [SerializeField]
        float zPos = -10f;
        
        [Title("Bounds")]
        [SerializeField]
        MapBoundType boundType;
        [SerializeField][ShowIf("boundType", MapBoundType.WorldBox)]
        BoxCollider2D worldBoundsB2D;
        [SerializeField][ShowIf("boundType", MapBoundType.Absolute)]
        Rect absolutBound;

        bool hasRect;
        Rect worldRect;
        Vector3 tmpVelocity;
        #endregion

        #region Init
        protected override void _OnAwake() {
            _RefreshWorldRect();
        }

        protected override void _OnValidate() {
            _RefreshWorldRect();
        }

        #endregion

        #region Unity Lifecycle
        protected override void _UpdateCamera(ref Vector3 velocity) {
            if (!Camera || !hasRect || !Target) return;
            if (!Camera.orthographic) return;

            float halfH = Camera.orthographicSize;
            float halfW = halfH * Camera.aspect;

            float minX = worldRect.xMin + halfW;
            float maxX = worldRect.xMax - halfW;
            float minY = worldRect.yMin + halfH;
            float maxY = worldRect.yMax - halfH;

            Vector3 desired = Target.position;
            float newX = Mathf.Clamp(desired.x, minX, maxX);
            float newY = Mathf.Clamp(desired.y, minY, maxY);

            Vector3 dest = new Vector3(newX, newY, zPos);

            if (Smooth <= 0f) {
                Camera.transform.position = dest;
            }
            else {
                Camera.transform.position =
                    Vector3.SmoothDamp(Camera.transform.position, dest, ref velocity, Smooth);
            }
        }
        #endregion

        #region Refresh
        private void _RefreshWorldRect() {
            hasRect = false;

            switch (boundType) {
            case MapBoundType.WorldBox:
                if (!worldBoundsB2D) return;
                Bounds bounds = worldBoundsB2D.bounds;
                worldRect = new Rect(bounds.min.x, bounds.min.y, bounds.size.x, bounds.size.y);
                hasRect = true;
                break;

            case MapBoundType.Absolute:
                if (absolutBound.size != Vector2.zero) {
                    worldRect = absolutBound;
                    hasRect = true;
                }
                break;

            default:
                hasRect = false;
                break;
            }
        }
        #endregion

        #region Debug
#if UNITY_EDITOR
        private void OnDrawGizmosSelected() {
            _RefreshWorldRect();
            if (!hasRect)
                return;

            switch (boundType) {
            case MapBoundType.WorldBox:
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(worldRect.center, worldRect.size);
                break;

            case MapBoundType.Absolute:
                if (absolutBound.size != Vector2.zero) {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireCube((Vector3)absolutBound.center, (Vector3)absolutBound.size);
                }
                break;

            default:
                break;
            }
        }
#endif
        #endregion
    }
}

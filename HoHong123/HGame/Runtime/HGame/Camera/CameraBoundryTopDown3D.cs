using UnityEngine;
using Sirenix.OdinInspector;

namespace HGame.Cam {
    [DisallowMultipleComponent]
    public class CameraBoundryTopDown3D : BaseCameraBoundry {
        #region Fields
        [Title("TopDown 3D Camera")]
        [SerializeField]
        float cameraHeight = 10f;

        [Title("Bounds")]
        [SerializeField]
        MapBoundType boundType;

        [SerializeField]
        [ShowIf("boundType", MapBoundType.WorldBox)]
        BoxCollider worldBounds;

        [SerializeField]
        [ShowIf("boundType", MapBoundType.Absolute)]
        Vector3 absoluteCenterXZ;

        [SerializeField]
        [ShowIf("boundType", MapBoundType.Absolute)]
        Vector2 absoluteSizeXZ;

        bool hasBounds;
        Bounds worldBounds3D;
        #endregion

        #region Properties
        public float CameraHeight {
            get => cameraHeight;
            set => cameraHeight = Mathf.Max(0f, value);
        }
        #endregion

        #region Init
        protected override void _OnAwake() {
            _RefreshWorldBounds();
        }

        protected override void _OnValidate() {
            _RefreshWorldBounds();
        }
        #endregion

        #region Unity Lifecycle
        protected override void _UpdateCamera(ref Vector3 velocity) {
            if (!Camera || !hasBounds || !Target) return;
            if (!Camera.orthographic) return; 

            float halfH = Camera.orthographicSize;
            float halfW = halfH * Camera.aspect;

            float minX = worldBounds3D.min.x + halfW;
            float maxX = worldBounds3D.max.x - halfW;
            float minZ = worldBounds3D.min.z + halfH;
            float maxZ = worldBounds3D.max.z - halfH;

            Vector3 desired = Target.position;
            float newX = Mathf.Clamp(desired.x, minX, maxX);
            float newZ = Mathf.Clamp(desired.z, minZ, maxZ);

            Vector3 dest = new Vector3(newX, cameraHeight, newZ);

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
        private void _RefreshWorldBounds() {
            hasBounds = false;
            switch (boundType) {
            case MapBoundType.WorldBox:
                if (!worldBounds) return;
                worldBounds3D = worldBounds.bounds;
                hasBounds = true;
                break;
            case MapBoundType.Absolute:
                if (absoluteSizeXZ == Vector2.zero) return;

                Vector3 center = new Vector3(absoluteCenterXZ.x, cameraHeight, absoluteCenterXZ.y);
                Vector3 size = new Vector3(absoluteSizeXZ.x, cameraHeight * 2f, absoluteSizeXZ.y);
                worldBounds3D = new Bounds(center, size);
                hasBounds = true;
                break;
            default:
                hasBounds = false;
                break;
            }
        }
        #endregion

        #region Debug
#if UNITY_EDITOR
        private void OnDrawGizmosSelected() {
            _RefreshWorldBounds();
            if (!hasBounds) return;

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(worldBounds3D.center, worldBounds3D.size);

            if (Camera && Camera.orthographic) {
                float halfH = Camera.orthographicSize;
                float halfW = halfH * Camera.aspect;

                Vector3 min = worldBounds3D.min;
                Vector3 max = worldBounds3D.max;

                Vector3 center = new Vector3(
                    (min.x + max.x) * 0.5f,
                    cameraHeight,
                    (min.z + max.z) * 0.5f
                );

                Vector3 size = new Vector3(
                    Mathf.Max(0f, worldBounds3D.size.x - halfW * 2f),
                    0.1f,
                    Mathf.Max(0f, worldBounds3D.size.z - halfH * 2f)
                );

                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(center, size);
            }
        }
#endif
        #endregion
    }
}

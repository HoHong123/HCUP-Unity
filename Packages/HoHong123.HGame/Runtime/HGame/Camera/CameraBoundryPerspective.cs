using UnityEngine;
using Sirenix.OdinInspector;

namespace HGame.Cam {
    [DisallowMultipleComponent]
    public class CameraBoundryPerspective : BaseCameraBoundry {
        #region Fields
        [Title("TPS Camera")]
        [SerializeField]
        Vector3 followOffset = new Vector3(0f, 2f, -4f);

        [Title("Bounds")]
        [SerializeField]
        BoxCollider worldBounds;

        bool hasBounds;
        Bounds bounds3D;
        #endregion

        #region Properties
        public Vector3 FollowOffset {
            get => followOffset;
            set => followOffset = value;
        }
        #endregion

        #region Init
        protected override void _OnAwake() {
            _RefreshBounds();
        }

        protected override void _OnValidate() {
            _RefreshBounds();
        }
        #endregion

        #region Unity Lifecycle
        protected override void _UpdateCamera(ref Vector3 velocity) {
            if (!Camera || !Target || !hasBounds) return;
            if (Camera.orthographic) return;

            Vector3 desired = Target.position + Target.rotation * followOffset;
            Vector3 clamped = _ClampPosition(desired);

            if (Smooth <= 0f) {
                Camera.transform.position = clamped;
            }
            else {
                Camera.transform.position =
                    Vector3.SmoothDamp(Camera.transform.position, clamped, ref velocity, Smooth);
            }

            Camera.transform.LookAt(Target.position);
        }

        #endregion

        #region Bound
        private Vector3 _ClampPosition(Vector3 p) {
            Vector3 min = bounds3D.min;
            Vector3 max = bounds3D.max;

            float x = Mathf.Clamp(p.x, min.x, max.x);
            float y = Mathf.Clamp(p.y, min.y, max.y);
            float z = Mathf.Clamp(p.z, min.z, max.z);

            return new Vector3(x, y, z);
        }

        private void _RefreshBounds() {
            hasBounds = false;
            if (!worldBounds) return;

            bounds3D = worldBounds.bounds;
            hasBounds = true;
        }
        #endregion

        #region Debug
#if UNITY_EDITOR
        void OnDrawGizmosSelected() {
            _RefreshBounds();
            if (!hasBounds) return;

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(bounds3D.center, bounds3D.size);

            if (Camera) {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(Camera.transform.position, 0.2f);
            }
        }
#endif
        #endregion
    }
}

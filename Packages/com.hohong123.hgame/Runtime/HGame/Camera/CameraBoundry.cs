using UnityEngine;
using Sirenix.OdinInspector;

// Legacy Script
namespace HGame.Cam {
    [DisallowMultipleComponent]
    public class CameraBoundry : MonoBehaviour {
        [Title("Camear")]
        [SerializeField]
        Camera cam;
        [SerializeField][Range(0f, 1f)]
        float smooth = 0.15f;
        [SerializeField]
        float zPos = -10f;

        [Title("Target")]
        [SerializeField]
        Transform target;
        [SerializeField][Required]
        Transform originalTarget;

        [Title("Bounds")]
        [SerializeField]
        MapBoundType boundType;
        [SerializeField][ShowIf("boundType", MapBoundType.WorldBox)]
        BoxCollider worldBound;
        [SerializeField][ShowIf("boundType", MapBoundType.Absolute)]
        Rect absolutBound;


        bool hasRect = false;
        Bounds worldBound3D = default;
        Vector3 velocity = default;


        private void Awake() {
            _RefreshWorldRect();
        }

        private void LateUpdate() {
            if (!cam || !hasRect || !target) return;

            Vector3 desired = target.position;
            Vector3 clamped = worldBound3D.ClosestPoint(desired);
            Vector3 dest = new Vector3(clamped.x, clamped.y, clamped.z);
            cam.transform.position = (smooth <= 0f)
                ? dest
                : Vector3.SmoothDamp(cam.transform.position, dest, ref velocity, smooth);
        }


        public void ResetTarget() => target = originalTarget;
        public void SetPosition(Transform target) => this.target = target;
        public void SetPosition(Vector3 position) {
            if (!target) target = originalTarget;
            target.position = position;
        }


        private void _RefreshWorldRect() {
            hasRect = true;
            switch (boundType) {
            case MapBoundType.WorldBox:
                if (worldBound) {
                    worldBound3D = worldBound.bounds;
                }
                break;
            case MapBoundType.Absolute:
                hasRect = false;
                break;
            default:
                hasRect = false;
                break;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected() {
            switch (boundType) {
            case MapBoundType.WorldBox:
                if (worldBound) {
                    var b = worldBound.bounds;
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireCube(b.center, b.size);
                }
                break;
            case MapBoundType.Absolute:
                if (absolutBound.size != Vector2.zero) {
                    Gizmos.color = Color.cyan;
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
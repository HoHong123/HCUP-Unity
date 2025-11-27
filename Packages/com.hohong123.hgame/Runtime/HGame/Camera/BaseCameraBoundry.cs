using UnityEngine;
using Sirenix.OdinInspector;

namespace HGame.Cam {
    [DisallowMultipleComponent]
    public abstract class BaseCameraBoundry : MonoBehaviour {
        #region Fields
        [Title("Camera")]
        [SerializeField]
        Camera cam;
        [SerializeField]
        [Range(0f, 1f)]
        float smooth = 0.15f;

        [Title("Target")]
        [SerializeField]
        Transform target;
        [SerializeField]
        Transform originalTarget;

        Vector3 velocity;
        #endregion

        #region Properties
        public Camera Camera => cam;
        public Transform Target => target;

        public float Smooth {
            get => smooth;
            set => smooth = Mathf.Max(0f, value);
        }
        #endregion

        #region 2-5. Getter / Setter
        public void ResetTarget() => target = originalTarget;
        public void SetPosition(Transform target) => this.target = target;
        public virtual void SetPosition(Vector3 position) {
            if (!target) target = originalTarget;
            if (!target) return;
            target.position = position;
        }
        #endregion

        #region Unity Lifecycle
        protected virtual void Awake() {
            if (!cam) cam = Camera.main;
            if (!target && originalTarget) target = originalTarget;
            _OnAwake();
        }

        protected virtual void OnValidate() {
            if (!cam) cam = Camera.main;
            _OnValidate();
        }

        protected virtual void LateUpdate() {
            if (!cam || !target) return;
            _UpdateCamera(ref velocity);
        }
        #endregion

        #region Inheritance
        protected virtual void _OnAwake() { }
        protected virtual void _OnValidate() { }
        protected abstract void _UpdateCamera(ref Vector3 velocity);
        #endregion
    }
}

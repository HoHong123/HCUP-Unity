using UnityEngine;
using HInspector;

namespace HGame.Cam {
    public class CameraManager : HCore.SingletonBehaviour<CameraManager> {
        [HTitle("Camera Follow")]
        [SerializeField]
        BaseCameraBoundry follow;

        [HTitle("Camera Effect")]
        // Add effect modules

        public void ResetFollow() => follow.ResetTarget();
        public void SetFollowTarget(Vector3 target) => follow.SetPosition(target);
        public void SetFollowTarget(Transform target) => follow.SetPosition(target);
    }
}

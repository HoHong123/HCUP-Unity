using UnityEngine;
using Sirenix.OdinInspector;

namespace HGame.Cam {
    public class CameraManager : HUtil.Core.SingletonBehaviour<CameraManager> {
        [Title("Camera Follow")]
        [SerializeField]
        BaseCameraBoundry follow;

        [Title("Camera Effect")]
        // Add effect modules

        public void ResetFollow() => follow.ResetTarget();
        public void SetFollowTarget(Vector3 target) => follow.SetPosition(target);
        public void SetFollowTarget(Transform target) => follow.SetPosition(target);
    }
}
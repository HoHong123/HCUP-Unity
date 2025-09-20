#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 2D 환경 카메라 매니지먼트 스크립트.
 * =========================================================
 */
#endif

using UnityEngine;
using Util.Core;
using Util.OdinCompat;

namespace HGame._2D.Cam {
    public class CameraManager : SingletonBehaviour<CameraManager> {
        [HeaderOrTitle("Camera Follow")]
        [SerializeField]
        CameraFollow follow;

        [HeaderOrTitle("Camera Effect")]
        // Add effect modules

        public void ResetFollow() => follow.ResetTarget();
        public void SetFollowTarget(Vector3 target) => follow.SetPosition(target);
        public void SetFollowTarget(Transform target) => follow.SetPosition(target);
    }
}


#if UNITY_EDITOR
/* Dev Log
 * @Jason - PKH
 * 카메라 추적 시스템 사용
 * TODO :: 추가 시스템 도입 예정 (흔들림, 전환, 이펙트 등)
 * =====================================
 */
#endif
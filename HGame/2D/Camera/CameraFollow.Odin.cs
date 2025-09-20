#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 2D 환경에서 특정 오브젝트를 따라다니는 카메라 이펙트 스크립트입니다.
 * =========================================================
 */
#endif

#if ODIN_INSPECTOR
using HGame._2D.Map;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.OdinCompat;
#endif

namespace HGame._2D.Cam {
    public partial class CameraFollow : MonoBehaviour {
#if ODIN_INSPECTOR
        [HeaderOrTitle("Camear")]
        [SerializeField]
        Camera cam;
        [SerializeField, Range(0f, 1f)]
        float smooth = 0.15f;
        [SerializeField]
        float zPos = -10f;

        [HeaderOrTitle("Target")]
        [SerializeField]
        Transform target;
        [SerializeField, Required]
        Transform originalTarget;

        [HeaderOrTitle("Bounds")]
        [SerializeField]
        MapBoundType boundType;
        [SerializeField, ShowIf("boundType", MapBoundType.WorldBox)]
        BoxCollider2D worldBoundsB2D;
        [SerializeField, ShowIf("boundType", MapBoundType.Absolute)]
        Rect absolutBound;

#if UNITY_EDITOR
        [HeaderOrTitle("Debug")]
        [SerializeField]
        protected bool useDebug = true;
        [SerializeField, ShowIf("useDebug")]
        protected Color debugColor = Color.cyan;
#endif
#endif
    }
}
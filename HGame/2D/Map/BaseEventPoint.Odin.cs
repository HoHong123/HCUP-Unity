#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 2D 환경에서 특정 오브젝트와 충돌 이벤트를 담당하는 스크립트를 구현했습니다.
 * 
 * ** 사용법 **
 * 1. 상속으로 제네릭 타입에 충돌 후 가져올 컴포넌트를 지정합니다.
 * 2. 물리 충돌이 발생하면 충돌 오브젝트의 태그를 비교합니다.
 * + 태그 사용유무 조절가능 ('useTag' 설정)
 * 3. 충돌 후 선정한 컴포넌트를 가져오고 대리자를 통해 연결된 모든 오브젝트에 전달합니다.
 * 4. 에디터상 이벤트 포인트의 기즈모를 활성화할 수 있습니다.
 * =========================================================
 */
#endif

#if ODIN_INSPECTOR
using UnityEngine;
using Sirenix.OdinInspector;
using Util.OdinCompat;
#endif

namespace HGame._2D.Map {
    public abstract partial class BaseEventPoint<T> : MonoBehaviour where T : MonoBehaviour {
#if ODIN_INSPECTOR
        [HeaderOrTitle("Filter")]
        [SerializeField]
        protected bool useTag = true;
        [SerializeField, ShowIf("useTag")]
        protected string targetTag;

        [HeaderOrTitle("Collider")]
        [SerializeField, Required]
        protected Collider2D eventCollider;

#if UNITY_EDITOR
        [HeaderOrTitle("Debug")]
        [SerializeField]
        protected bool useDebug = true;
        [SerializeField, ShowIf("useDebug")]
        protected Color debugColor = Color.red;
#endif
#endif
    }
}

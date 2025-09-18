using System;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

namespace Util.UI.Drop {
    [Serializable]
    public class BaseDropUnit : MonoBehaviour, IDropUnit {
        [Title("Information")]
        [SerializeField]
        protected int uid = -1;
        [SerializeField]
        protected Toggle unitTg;

        public int UID => uid;
        public Toggle Toggle => unitTg;
        public Action<int> OnSelect { get; protected set; }
    }
}


#if UNITY_EDITOR
/* Dev Log
 * @Jason - PKH
 * KOR ::
 * 유닛의 경우, 토글 사용이 필수적이기에 'IDropUnit' 인터페이스에 토글 프로퍼티가 선언되어있습니다.
 * 'BaseDropUnit' 클래스는 유닛으로 바로 사용될 수 있는 클래스로써 선언하긴 했지만, 제가 개인적으로 만든 클래스라 오딘인스펙터를 사용합니다.
 * 환경과 상황에 따라 'BaseDropUnit' 클래스가 아닌 자신만의 클래스 선언을 추천합니다.
 * ENG ::
 * For units, the use of toggle is essential, so the toggle property is declared in the 'IDropUnit' interface.
 * The 'BaseDropUnit' class is declared as a class that can be used directly as a unit, but since it is a class I personally created, I use the Odin inspector.
 * Depending on the environment and situation, I recommend declaring your own class instead of the 'BaseDropUnit' class.
 */
#endif
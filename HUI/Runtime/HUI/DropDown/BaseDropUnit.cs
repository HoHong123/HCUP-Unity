#if UNITY_EDITOR
/* =========================================================
 * 이 스크립트는 드롭다운 항목 유닛의 기본 베이스 클래스입니다.
 * 드롭다운에서 공통적으로 필요한 UID, Toggle, 선택 콜백 참조를 보관하며,
 * IDropUnit을 구현하는 가장 단순한 기본 구조를 제공합니다.
 *
 * 주의사항 ::
 * 1. 이 스크립트는 Toggle 기반 드롭다운 유닛 구조를 전제로 합니다.
 * 2. unitTg는 정상 동작을 위해 반드시 연결되어 있어야 합니다.
 * 3. 이 클래스는 바로 사용할 수 있는 기본형이지만, 실제 프로젝트에서는 전용 파생 클래스로 확장하는 것을 권장합니다.
 * 4. IDropUnit 규격이 변경되면 이 클래스도 함께 수정되어야 합니다.
 * =========================================================
 */
#endif

using System;
using UnityEngine;
using UnityEngine.UI;
using HInspector;

namespace HUI.Dropdown {
    [Serializable]
    public class BaseDropUnit : MonoBehaviour, IDropUnit {
        [HTitle("Information")]
        [SerializeField]
        protected int uid = -1;
        [SerializeField]
        protected Toggle unitTg;

        public int UID => uid;
        public Toggle Toggle => unitTg;

        public event Action<int> OnSelect;

        public void RunSelectEvent() {
            OnSelect?.Invoke(uid);
        }
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
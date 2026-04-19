/* =========================================================
 * @Jason - PKH
 * HInspector의 CustomEditor 파이프라인을 활성화하기 위한 MonoBehaviour 베이스 클래스입니다.
 *
 * 역할 ::
 * Unity의 CustomEditor 선택 규칙(더 구체적인 타겟이 우선)을 이용하여,
 * MonoBehaviour를 전역 타겟팅하는 타 에디터(Odin 등)보다 우리 HInspectorEditor가
 * 우선 선택되도록 opt-in 지점을 제공합니다.
 *
 * 사용법 ::
 * MonoBehaviour 대신 이 클래스를 상속받으세요.
 * HHorizontalGroup / HVerticalGroup / 향후 HButton / HShowInInspector 등
 * CustomEditor 기반 기능을 사용하려면 이 베이스가 필수입니다.
 *
 * 주의사항 ::
 * PropertyDrawer 기반 어트리뷰트(HTitle, HReadOnly, HShowIf 등)는
 * 이 베이스 상속 여부와 무관하게 일반 MonoBehaviour에서도 동작합니다.
 * =========================================================
 */

using UnityEngine;

namespace HInspector {
    public abstract class HInspectorBehaviour : MonoBehaviour { }
}

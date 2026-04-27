/* =========================================================
 * @Jason - PKH
 * Odin이 설치된 환경에서 해당 타입을 Odin 대신 HInspectorEditor로 직접 렌더하게 하는 opt-in 진입점입니다.
 * Unity의 CustomEditor 선택 규칙(더 구체적 타겟 우선)을 이용해 Odin보다 HMonoBehaviourInspector가 선택됩니다.
 *
 * 대부분의 경우 이 상속은 불필요합니다 ::
 * Odin 설치 시 - 브릿지(HInspectorToOdinBridge)가 HInspector 속성을 Odin 속성으로 매핑하므로
 *                일반 MonoBehaviour 상속만으로도 HTitle / HButton / HShowIf 등이 동작합니다.
 * Odin 미설치 시 - 전역 fallback(HGlobalMonoBehaviourInspector)이 일반 MonoBehaviour도 HInspectorEditor로 처리합니다.
 *
 * 이 상속이 실제로 필요한 케이스 ::
 * Odin 설치 환경에서 특정 타입을 브릿지 경유가 아닌 HInspectorEditor 파이프라인으로 직접 렌더하고 싶을 때.
 * 이 경우 Odin 전용 속성(TableList, InlineEditor 등)은 해당 타입에서 동작하지 않습니다.
 * =========================================================
 */

using UnityEngine;

namespace HInspector {
    public abstract class HInspectorBehaviour : MonoBehaviour { }
}

/* =========================================================
 * @Jason - PKH
 * Odin이 설치된 환경에서 해당 타입을 Odin 대신 HInspectorEditor로 직접 렌더하게 하는 opt-in 진입점입니다.
 * Unity의 CustomEditor 선택 규칙(더 구체적 타겟 우선)을 이용해 Odin보다 HScriptableObjectInspector가 선택됩니다.
 *
 * 대부분의 경우 이 상속은 불필요합니다 ::
 * Odin 설치 시 - 브릿지(HInspectorToOdinBridge)가 HInspector 속성을 Odin 속성으로 매핑하므로
 *                일반 ScriptableObject 상속만으로도 HInspector 속성이 동작합니다.
 * Odin 미설치 시 - 전역 fallback(HGlobalScriptableObjectInspector)이 일반 ScriptableObject도 HInspectorEditor로 처리합니다.
 *
 * 이 상속이 실제로 필요한 케이스 ::
 * Odin 설치 환경에서 특정 ScriptableObject를 브릿지 경유가 아닌 HInspectorEditor로 직접 렌더하고 싶을 때.
 * 이 경우 Odin 전용 속성은 해당 타입에서 동작하지 않습니다.
 * =========================================================
 */

using UnityEngine;

namespace HInspector {
    public abstract class HInspectorScriptableObject : ScriptableObject { }
}

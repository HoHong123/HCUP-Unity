#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * HInspectorScriptableObject 타겟용 HInspector 쉘 클래스입니다.
 *
 * 역할 ::
 * 추상 베이스인 HInspectorEditor를 HInspectorScriptableObject 계열에 등록하는 지점입니다.
 * ScriptableObject 전역 타겟이 아닌 HInspectorScriptableObject를 타겟팅하여 Odin 등
 * 타 전역 에디터보다 우선 선택되도록 합니다.
 *
 * 주의사항 ::
 * 사용자는 ScriptableObject 대신 HInspectorScriptableObject를 상속받아야 이 에디터가 적용됩니다.
 * 로직은 모두 HInspectorEditor에 있고 이 클래스는 타겟 등록만 담당합니다.
 * =========================================================
 */
#endif

#if UNITY_EDITOR
using UnityEditor;

namespace HInspector.Editor {
    [CustomEditor(typeof(HInspectorScriptableObject), true)]
    [CanEditMultipleObjects]
    public sealed class HScriptableObjectInspector : HInspectorEditor { }
}
#endif

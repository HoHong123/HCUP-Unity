#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * HInspectorBehaviour 타겟용 HInspector 쉘 클래스입니다.
 *
 * 역할 ::
 * 추상 베이스인 HInspectorEditor를 HInspectorBehaviour 계열에 등록하는 지점을 제공합니다.
 * MonoBehaviour 전역 타겟이 아닌 HInspectorBehaviour를 타겟팅하여 Odin 등 타 전역
 * 에디터보다 우선 선택되도록 합니다 (Unity CustomEditor 규칙: 더 구체적 타겟 우선).
 *
 * 주의사항 ::
 * 사용자는 MonoBehaviour 대신 HInspectorBehaviour를 상속받아야 이 에디터가 적용됩니다.
 * 로직은 모두 HInspectorEditor에 있고 이 클래스는 타겟 등록만 담당합니다.
 * =========================================================
 */
#endif

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace HInspector.Editor {
    [CustomEditor(typeof(HInspectorBehaviour), true)]
    [CanEditMultipleObjects]
    public sealed class HMonoBehaviourInspector : HInspectorEditor { }

#if !ODIN_INSPECTOR
    // Odin 미설치 환경에서만 활성. Odin이 있으면 defineConstraints에 의해 이 쉘이 컴파일되지 않아 Odin과 경쟁하지 않는다.
    // MonoBehaviour 전역 + isFallback으로 등록하여, H-속성이 하나라도 있는 일반 MonoBehaviour에는 HInspectorEditor가 적용되고,
    // H-속성이 없는 타입은 HInspectorEditor 내부 가드(_HasAnyHInspectorAttribute)가 DrawDefaultInspector로 자동 폴백한다.
    [CustomEditor(typeof(MonoBehaviour), true, isFallback = true)]
    [CanEditMultipleObjects]
    public sealed class HGlobalMonoBehaviourInspector : HInspectorEditor { }
#endif
}
#endif

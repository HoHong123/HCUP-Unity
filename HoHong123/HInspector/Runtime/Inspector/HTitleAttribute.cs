#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Inspector에 섹션 Title을 추가하는 Attribute입니다.
 * 선언 위치에 Title 텍스트와 구분선을 독립 레이아웃 아이템으로 그립니다.
 *
 * 사용 예 ::
 * [HTitle("Character Stats")]
 * public int hp;
 *
 * 결과 ::
 * Character Stats
 * ─────────────────
 * hp
 *
 * 특징 ::
 * Unity의 [Space] / [Header]처럼 "선언 위치에 그려지는 독립 슬롯"으로 동작합니다.
 * 부착된 필드가 HBoxGroup / HHorizontalGroup / HVerticalGroup 등에 속하더라도
 * 타이틀은 그룹 경계 밖에 렌더됩니다. 그룹이 열린 상태에서 HTitle을 만나면
 * 현재 그룹을 닫고 타이틀을 그린 뒤, 필드의 그룹을 새로 열어 진행합니다.
 *
 * 주의사항 ::
 * HInspectorEditor(CustomEditor)가 처리하므로, HInspectorBehaviour 또는
 * HInspectorScriptableObject를 상속받은 타겟에서만 시각적으로 그려집니다.
 * 일반 MonoBehaviour / ScriptableObject에서는 어트리뷰트가 무시됩니다.
 * 로직이 없는 순수 UI 장식용 Attribute이기에 Conditional이 붙습니다.
 * =========================================================
 */
#endif

namespace HInspector {
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    public class HTitleAttribute : HInspectorAttribute {
        public string Title { get; }
        public HTitleAttribute(string title, int order = -50)
            : base(order) {
            Title = title;
        }
    }
}
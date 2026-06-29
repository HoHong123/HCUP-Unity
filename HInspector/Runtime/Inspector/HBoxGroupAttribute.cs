#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Inspector에서 필드들을 하나의 박스 GUI 안에 시각적으로 묶는 Attribute입니다.
 * 동일한 GroupName을 가진 필드들은 박스 프레임 안에 수직으로 정렬되며,
 * GroupName이 박스 상단에 헤더로 표시됩니다.
 *
 * 사용 예 ::
 * [HBoxGroup("Stats")]
 * public int hp;
 * [HBoxGroup("Stats")]
 * public int atk;
 *
 * 결과 ::
 * ┌─ Stats ───┐
 * │ HP  [______]    │
 * │ Atk [______]    │
 * └───────┘
 *
 * 주의사항 ::
 * 내부 필드들을 수평으로 나열하려면 HHorizontalGroupAttribute를 사용하세요.
 * HBoxGroup과 HHorizontalGroup은 직교하는 독립 개념입니다.
 * =========================================================
 */
#endif

namespace HInspector {
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class HBoxGroupAttribute : HInspectorAttribute {
        public string GroupName { get; }

        public HBoxGroupAttribute(string groupName, int order = -40)
            : base(order) {
            GroupName = groupName;
        }
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.06.29 Runtime attribute 빌드 가드 제거
 *
 * # 수정
 * - namespace + 클래스 전체를 감싸던 #if UNITY_EDITOR 가드 제거
 * - 빌드 포함 필요한 Runtime attribute가 Addressables 플레이어 빌드에서 CS0246 유발했던 문제 수정
 *
 * =============================================================================
 */
#endif

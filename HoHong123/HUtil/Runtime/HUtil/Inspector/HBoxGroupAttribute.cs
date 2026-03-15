#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Inspector에서 필드를 같은 그룹으로 묶기 위한 Attribute입니다.
 * 동일한 GroupName을 가진 필드를 하나의 Horizontal Box 그룹으로 정렬합니다.
 *
 * 사용 예 ::
 * [HBoxGroup("Stats")]
 * public int hp;
 * [HBoxGroup("Stats")]
 * public int atk;
 * =========================================================
 */
#endif
#if UNITY_EDITOR
namespace HUtil.Inspector {
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class HBoxGroupAttribute : HInspectorAttribute {
        public string GroupName { get; }

        public HBoxGroupAttribute(string groupName, int order = -40)
            : base(order) {
            GroupName = groupName;
        }
    }
}
#endif
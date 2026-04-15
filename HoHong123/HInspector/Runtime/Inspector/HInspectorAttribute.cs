#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * HUtil Inspector 시스템의 모든 Attribute의 기본 클래스입니다.
 *
 * 특징 ::
 * Unity PropertyAttribute 기반 확장
 *
 * 기능 ::
 * Order 값을 통해 Attribute 실행 순서를 제어합니다.
 * =========================================================
 */
#endif

namespace HUtil.Inspector {
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public abstract class HInspectorAttribute : UnityEngine.PropertyAttribute {
        public int Order { get; }
        protected HInspectorAttribute(int order = 0) => Order = order;
    }
}

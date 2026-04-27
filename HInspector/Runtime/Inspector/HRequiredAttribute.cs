#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Inspector 필드가 비어있을 때 경고를 표시하는 Attribute입니다.
 *
 * 지원 타입 ::
 * ObjectReference (null 체크)
 * String (빈 문자열 체크)
 *
 * 사용 예 ::
 * [HRequired]
 * public GameObject target;
 *
 * [HRequired("플레이어 프리팹을 할당해주세요")]
 * public GameObject playerPrefab;
 *
 * 결과 ::
 * 값이 비어있으면 필드 아래에 경고 박스 표시
 * =========================================================
 */
#endif

namespace HInspector {
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class HRequiredAttribute : HInspectorAttribute {
        public string Message { get; }

        public HRequiredAttribute(int order = 600) : base(order) {
            Message = null;
        }

        public HRequiredAttribute(string message, int order = 600) : base(order) {
            Message = message;
        }
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Inspector에 섹션 Title을 추가하는 Attribute입니다.
 * 필드 위에 Title 텍스트와 구분선을 생성합니다.
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
 * 주의사항 ::
 * 로직이 없는 순수 UI 장식용 Attribute이기에 Conditional이 붙습니다.
 * =========================================================
 */
#endif

namespace HUtil.Inspector {
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
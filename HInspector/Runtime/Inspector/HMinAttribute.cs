#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Inspector 필드의 최소값을 제한하는 Attribute입니다.
 *
 * 적용 타입 ::
 * int, float, Vector2
 *
 * 사용 예 ::
 * [HMin(0)]
 * public int hp;
 * =========================================================
 */
#endif

namespace HInspector {
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class HMinAttribute : HInspectorAttribute {
        public float Min { get; }

        public HMinAttribute(float min, int order = 100)
            : base(order) {
            Min = min;
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
 * - 헤더 주석만 가드 유지, 클래스 본문은 빌드에 포함되도록 수정
 *
 * =============================================================================
 */
#endif
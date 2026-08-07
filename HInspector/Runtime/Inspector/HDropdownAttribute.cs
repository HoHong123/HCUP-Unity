#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 정수 필드를 "등록된 목록"에서 고르게 만드는 인스펙터 속성입니다.
 *
 * 특징 ::
 * 목록의 출처를 이 어셈블리가 알지 못합니다. 소스 ID(문자열)만 들고 있고, 실제 항목은
 * 각 도메인이 HDropdownSourceRegistry 에 등록합니다. 그래서 HInspector 는 HAudio 같은
 * 상위 도메인을 참조하지 않고도 그 도메인의 목록을 그릴 수 있습니다.
 *
 * 사용 예 ::
 * [HDropdown("HAudio.Clips")]
 * [SerializeField] int overrideClickUid;      // 0 = 선택 없음
 *
 * 주의사항 ::
 * 1. int 필드 전용입니다. 다른 타입에는 기본 필드로 폴백합니다.
 * 2. HInspectorAttribute 파생이므로 [HShowIf] 등 다른 HInspector 속성과 같은 필드에
 *    함께 붙일 수 있습니다 (드로어가 하나로 합성되기 때문).
 * 3. 소스가 등록되지 않았거나 값이 목록에 없으면 "Missing (값)" 으로 붉게 표시합니다 —
 *    조용히 넘어가면 참조가 끊긴 것을 아무도 모른다.
 * =========================================================
 */
#endif

namespace HInspector {
    public sealed class HDropdownAttribute : HInspectorAttribute {
        /// <summary> 항목 공급자를 찾을 키. 도메인마다 고유해야 한다 (예: "HAudio.Clips"). </summary>
        public string SourceId { get; }

        /// <summary> true 면 0 을 "(None)" 으로 표시하고 선택지에 포함한다. </summary>
        public bool AllowNone { get; }

        public HDropdownAttribute(string sourceId, bool allowNone = true, int order = 0) : base(order) {
            SourceId = sourceId;
            AllowNone = allowNone;
        }
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.08.06 신규 생성
 *
 * # 목적
 * - 직렬화 필드에 enum 을 쓸 수 없는 계층(HCUP 라이브러리)에서도 드롭다운 저작을 제공.
 *
 * # 설계 결정
 * - enum 은 컴파일 타임 도구다. 직렬화 필드의 값은 .prefab/.unity YAML 에 들어가므로
 *   컴파일러가 검증하지 않는다 — enum 으로 선언해도 카탈로그에서 항목이 사라질 때
 *   컴파일 오류가 나지 않는다. 그래서 직렬화 필드에는 enum 이 아니라
 *   "제한된 편집 UI + 참조 검증" 이 필요하고, 이 속성이 그 앞단이다.
 * - 목록 출처를 문자열 ID 로 간접화해 의존 방향(도메인 → HInspector)을 지켰다.
 * - HInspectorAttribute 파생을 택했다. 독립 PropertyAttribute 로 두면 Unity 가 필드당
 *   드로어를 하나만 적용하므로 같은 필드의 [HShowIf] 등과 공존하지 못한다.
 *   (HSpritePreviewAttribute 가 독립 파생인 이유는 그쪽이 조합 대상이 아니기 때문이다)
 *
 * =============================================================================
 */
#endif

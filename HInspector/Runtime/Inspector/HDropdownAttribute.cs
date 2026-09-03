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
 * [SerializeField] int overrideClickUid;      // 0 = 선택 없음, 검색창 항상 표시
 *
 * [HDropdown("HAudio.Clips", searchThreshold: 20)]
 * [SerializeField] int rareClickUid;          // 항목이 20개를 넘을 때만 검색창 표시
 *
 * 주의사항 ::
 * 1. int 필드 전용입니다. 다른 타입에는 기본 필드로 폴백합니다.
 * 2. HInspectorAttribute 파생이므로 [HShowIf] 등 다른 HInspector 속성과 같은 필드에
 *    함께 붙일 수 있습니다 (드로어가 하나로 합성되기 때문).
 * 3. 소스가 등록되지 않았거나 값이 목록에 없으면 "Missing (값)" 으로 붉게 표시합니다.
 *    조용히 넘어가면 참조가 끊긴 것을 아무도 모릅니다.
 * 4. SearchThreshold 는 현재 Odin 렌더러에서만 반영됩니다. 비-Odin 경로(HDropdownField)는
 *    UnityEditor 의 AdvancedDropdown 을 쓰는데, 그 타입이 검색 관련 멤버를 전혀 공개하지
 *    않아 검색창을 띄울 수 없습니다 (검색 상태는 internal AdvancedDropdownWindow 소유).
 *    비-Odin 경로에 검색을 넣으려면 AdvancedDropdown 을 버리고 자체 팝업을 구현해야 합니다.
 * =========================================================
 */
#endif

namespace HInspector {
    public sealed class HDropdownAttribute : HInspectorAttribute {
        /// <summary> 항목 공급자를 찾을 키. 도메인마다 고유해야 한다 (예: "HAudio.Clips"). </summary>
        public string SourceId { get; }

        /// <summary> true 면 0 을 "(None)" 으로 표시하고 선택지에 포함한다. </summary>
        public bool AllowNone { get; }

        /// <summary>
        /// 검색창을 켜기 시작하는 항목 개수. 항목 수가 이 값 이하이면 검색창을 숨긴다.
        /// 0(기본)이면 개수와 무관하게 항상 켠다. "(None)" 도 항목 수에 포함한다.
        /// </summary>
        public int SearchThreshold { get; }

        public HDropdownAttribute(string sourceId, bool allowNone = true, int searchThreshold = 0, int order = 0) : base(order) {
            SourceId = sourceId;
            AllowNone = allowNone;
            // 음수는 임계값으로 뜻이 없다. 0(항상 켬)으로 접어 Odin 에 이상값을 넘기지 않는다.
            SearchThreshold = searchThreshold < 0 ? 0 : searchThreshold;
        }
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * 2026-09-03 (수정) :: AdvancedDropdown 검색 관련 서술 정정 (메타데이터 실측)
 *
 * # 변경
 * - XML 주석에서 Odin 언급을 제거했다. 임계값의 의미는 이 파일이 정의한다.
 * - 주의사항 4 를 실측 결과로 교체했다. 비-Odin 경로는 검색창을 띄울 수 없다.
 *
 * # 이유
 * - 이 파일의 이전 서술과 그 앞 서술이 둘 다 틀렸다. 실측 없이 단정한 결과다.
 *   (1) "AdvancedDropdown 이 검색창을 항상 그린다" 틀림. 사용자가 IMGUI 에서 없음을 확인했다.
 *   (2) "isSearchFieldDisabled 로 제어 가능" 틀림. 그 멤버는 AdvancedDropdown 에 없다.
 * - 실측 방법과 결과 : UnityEditor.CoreModule.dll 의 메타데이터를 직접 읽었다.
 *   AdvancedDropdown(public) 이 서브클래스에 여는 멤버는 minimumSize(protected),
 *   BuildRoot(protected abstract), ItemSelected(protected virtual) 뿐이다.
 *   maximumSize / SetFilter / m_Gui / m_WindowInstance 는 전부 internal 이고,
 *   검색 상태(searchable, isSearchFieldDisabled)는 internal 타입
 *   AdvancedDropdownWindow 가 갖는다. 외부 어셈블리에서 상속도 호출도 불가하다.
 *
 * # 결과
 * - SearchThreshold 는 Odin 렌더러에서만 반영된다. 이건 어트리뷰트의 결함이 아니라
 *   비-Odin 렌더러의 미구현이다. 계약의 정의는 이 파일에 있다.
 *
 * # 주의
 * - 비-Odin 경로에 검색을 넣으려면 AdvancedDropdown 을 PopupWindowContent 기반
 *   자체 팝업으로 교체해야 한다. 계층(라벨의 '/') 재구현이 따라온다. 별도 작업이다.
 * - 리플렉션으로 internal 멤버를 건드리는 우회는 택하지 않는다. 라이브러리 코드가
 *   에디터 버전마다 깨진다.
 *
 * =============================================================================
 * 2026-09-03 (수정) :: SearchThreshold 태그 신설
 *
 * # 변경
 * - SearchThreshold(int) 프로퍼티와 생성자 인자 searchThreshold 추가. 기본값 0.
 * - 음수 입력은 생성자에서 0 으로 접는다.
 *
 * # 이유
 * - Odin 설치 환경에서 [HDropdown] 은 HInspectorToOdinBridge 를 거쳐 Odin
 *   ValueDropdown 으로 그려진다. 브릿지가 검색 설정을 넘기지 않아 Odin 기본값
 *   NumberOfItemsBeforeEnablingSearch = 10 이 적용되고, 항목이 10개 이하인 소스는
 *   검색창이 아예 뜨지 않았다. 저작 시점에 이를 제어할 수단이 없었다.
 * - 기본값을 Odin 과 같은 10 이 아니라 0 으로 잡았다. 목록은 등록소가 런타임에
 *   공급하므로 저작 시점에 개수를 알 수 없고, 비-Odin 경로(AdvancedDropdown)는
 *   검색창을 항상 그린다. 0 이어야 두 렌더 경로의 체감이 같아진다.
 *
 * # 결과
 * - 기존 호출부는 인자를 늘리지 않아도 검색창이 항상 살아있다.
 *   생성자 인자를 order 앞에 끼웠지만 3번째 인자를 위치로 넘기는 호출부가 없어
 *   기존 코드의 의미는 바뀌지 않는다 (호출처 2곳 전수 확인).
 *
 * # 주의
 * - 비-Odin 경로에서는 이 값이 무시된다. AdvancedDropdown 의 검색창을 끄는 공개
 *   API 가 없어서 SearchThreshold 를 크게 줘도 검색창은 남는다.
 *
 * =============================================================================
 * @Jason - PKH 2026.08.06 신규 생성
 *
 * # 목적
 * - 직렬화 필드에 enum 을 쓸 수 없는 계층(HCUP 라이브러리)에서도 드롭다운 저작을 제공.
 *
 * # 설계 결정
 * - enum 은 컴파일 타임 도구다. 직렬화 필드의 값은 .prefab/.unity YAML 에 들어가므로
 *   컴파일러가 검증하지 않는다 - enum 으로 선언해도 카탈로그에서 항목이 사라질 때
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

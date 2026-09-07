#if UNITY_EDITOR
/* =========================================================
 * 이 스크립트는 커스텀 드롭다운 UI의 공통 베이스 클래스입니다.
 * 네이티브 드롭다운 대신 기존 UI 레이아웃 시스템과 결합 가능한 구조를 제공하며,
 * 데이터와 유닛을 제네릭으로 받아 드롭다운 항목 생성 및 선택 처리를 확장할 수 있도록 설계되었습니다.
 *
 * 주의사항 ::
 * 1. 이 스크립트는 Toggle 및 RectTransform 컴포넌트가 반드시 필요합니다.
 * 2. TData는 IDropData를 구현하고 기본 생성자가 가능해야 합니다.
 * 3. TUnit은 MonoBehaviour 및 IDropUnit을 구현해야 합니다.
 * 4. 실제 유닛 초기화와 선택 후 처리 로직은 파생 클래스에서 반드시 구현해야 합니다.
 * 5. unitPrefab은 TUnit 컴포넌트를 포함하거나, 런타임에 AddComponent 가능한 구조여야 합니다.
 * 6. 검색은 searchInput 슬롯이 연결된 경우에만 동작합니다. 비어 있으면 검색 경로 자체가 꺼집니다.
 * 7. searchInput을 연결하려면 파생 클래스가 GetSearchKey를 반드시 override해야 합니다.
 *    기본 구현은 빈 문자열을 돌려주므로 override 없이 검색어를 입력하면 모든 항목이 숨습니다.
 * =========================================================
 */
#endif

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HInspector;

namespace HUI.Dropdown {
    [RequireComponent(typeof(Toggle))]
    [RequireComponent(typeof(RectTransform))]
    public abstract class BaseDropDown<TData, TUnit> : MonoBehaviour, IBasicPanel
        where TData : IDropData, new()
        where TUnit : MonoBehaviour, IDropUnit {
        [HTitle("Data")]
        [SerializeField]
        protected List<TData> datas = new();

        [HTitle("Setting")]
        [SerializeField, HOnValueChanged("SetTablePivot")]
        [Tooltip("Preset position setting. (Not mandatory)")]
        protected DirectionType direction = DirectionType.Down;

        [HTitle("Dropdown")]
        [SerializeField]
        protected Toggle dropTg;
        [SerializeField]
        protected RectTransform rect;

        [HTitle("Table")]
        [SerializeField]
        protected ToggleGroup tableTgg;
        [SerializeField]
        protected GameObject table;
        [SerializeField]
        protected RectTransform tableRect;
        [SerializeField]
        protected Transform unitParent;
        [SerializeField]
        protected Vector2 tableOffset;

        [HTitle("Unit")]
        [SerializeField]
        protected GameObject unitPrefab;
        [SerializeField]
        protected List<TUnit> units = new();

        [HTitle("Search")]
        [SerializeField]
        [Tooltip("Item filter input. Leave it empty to disable search. (Not mandatory)")]
        protected TMP_InputField searchInput;

        public event Action<int> OnItemSelected;

        int value = 0;
        public int Value { 
            get => value;
            set {
                this.value = value;
                OnItemSelected?.Invoke(value);
            }
        }


        protected virtual void Start() {
            if (rect == null) rect = GetComponent<RectTransform>();
            if (tableRect == null) tableRect = table.GetComponent<RectTransform>();

            dropTg.onValueChanged.AddListener(SetActive);
            OnItemSelected += SelectByIndex;
            if (searchInput != null) searchInput.onValueChanged.AddListener(_OnSearchChanged);
            SetActive(false);

            if (datas.Count == 0) return;

            CreateUnits();
            InitUnits();
        }


        public virtual void Open() => table.SetActive(true);
        public virtual void Close() => table.SetActive(false);
        public virtual void SetActive(bool isOn) {
            if (isOn) {
                ClearSearch();
                Open();
            }
            else {
                Close();
            }
        }

        public void OnSelect(int index) {
            Value = index;
            Close();
        }


        protected virtual void CreateUnits() {
            if (datas.Count == 0) return;

            bool haveUnit = unitPrefab.TryGetComponent(typeof(TUnit), out var comp);
            for (int k = 0; k < datas.Count; k++) {
                var index = k;
                var data = datas[index];
                var go = Instantiate(unitPrefab, unitParent);
                go.SetActive(true);
                if (!haveUnit) go.AddComponent(typeof(TUnit));
                units.Add(go.GetComponent<TUnit>());
            }

            // Disable game object, If the object is an actual game object in scene.
            if (unitPrefab.scene.IsValid()) unitPrefab.SetActive(false);
            Close();
        }


        protected void SetTablePivot() {
            if (rect == null || tableRect == null)
                return;

            Vector2 pivot = Vector2.zero;
            Vector2 offset = Vector2.zero;

            switch (direction) {
            case DirectionType.Left:
                pivot = new Vector2(1, 0.5f);
                offset = new Vector2((rect.rect.width * -0.5f) - tableOffset.x, 0);
                break;
            case DirectionType.Right:
                pivot = new Vector2(0, 0.5f);
                offset = new Vector2((rect.rect.width * 0.5f) + tableOffset.x, 0);
                break;
            case DirectionType.Up:
                pivot = new Vector2(0.5f, 0);
                offset = new Vector2(0, (rect.rect.height * 0.5f) + tableOffset.y);
                break;
            case DirectionType.Down:
                pivot = new Vector2(0.5f, 1);
                offset = new Vector2(0, (rect.rect.height * -0.5f) - tableOffset.y);
                break;
            case DirectionType.LeftTop:
                pivot = new Vector2(1, 0);
                offset = new Vector2((rect.rect.width * -0.5f) - tableOffset.x, (rect.rect.height * -0.5f) - tableOffset.y);
                break;
            case DirectionType.LeftBottom:
                pivot = new Vector2(1, 1);
                offset = new Vector2((rect.rect.width * -0.5f) - tableOffset.x, (rect.rect.height * 0.5f) + tableOffset.y);
                break;
            case DirectionType.RightTop:
                pivot = new Vector2(0, 0);
                offset = new Vector2((rect.rect.width * 0.5f) - tableOffset.x, (rect.rect.height * -0.5f) - tableOffset.y);
                break;
            case DirectionType.RightBottom:
                pivot = new Vector2(0, 1);
                offset = new Vector2((rect.rect.width * 0.5f) - tableOffset.x, (rect.rect.height * 0.5f) + tableOffset.y);
                break;
            case DirectionType.Center:
            default:
                pivot = new Vector2(0.5f, 0.5f);
                offset = Vector2.zero;
                break;
            }

            tableRect.pivot = pivot;
            tableRect.anchoredPosition = offset;
        }


        /// <summary>
        /// Hides every unit whose search key does not contain the query.
        /// A blank query restores all units.
        /// </summary>
        protected void ApplyFilter(string query) {
            // 항목마다 Trim 하지 않는다 - 키 입력 한 번에 한 번만 다듬어 할당을 줄인다.
            string keyword = string.IsNullOrWhiteSpace(query) ? string.Empty : query.Trim();

            // CreateUnits가 units를 Clear하지 않으므로 인스펙터 선입력 시 units.Count > datas.Count가 될 수 있다.
            int count = Mathf.Min(datas.Count, units.Count);
            for (int k = 0; k < count; k++) {
                var unit = units[k];
                if (unit == null) continue;

                unit.gameObject.SetActive(_IsMatch(GetSearchKey(datas[k]), keyword));
            }
        }

        /// <summary>Clears the query so that the next Open shows every unit.</summary>
        protected void ClearSearch() {
            if (searchInput == null) return;

            searchInput.text = string.Empty;
            ApplyFilter(string.Empty);
        }


        void _OnSearchChanged(string query) => ApplyFilter(query);

        static bool _IsMatch(string key, string keyword) {
            if (string.IsNullOrEmpty(keyword)) return true;
            if (string.IsNullOrEmpty(key)) return false;

            return key.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }


        /// <summary>
        /// After create unit game object by calling 'CreateUnits' function.
        /// Init all units using 'TData' in 'InitUnits' function.
        /// </summary>
        protected abstract void InitUnits();
        /// <summary>
        /// What would happen after selecting 
        /// </summary>
        /// <param name="index"></param>
        protected abstract void SelectByIndex(int index);
        /// <summary>
        /// Search key of 'TData'. The query is matched against this string.
        /// Default is empty, which means this dropdown does not support search.
        /// Override it whenever 'searchInput' is connected.
        /// </summary>
        protected virtual string GetSearchKey(TData data) => string.Empty;
    }
}


#if UNITY_EDITOR
/* Dev Log
 * =============================================================================
 * 2026-09-03 (수정) :: 검색(필터) 파이프라인을 베이스로 승격
 *
 * # 변경
 * - searchInput(TMP_InputField) 슬롯, ApplyFilter / ClearSearch, _OnSearchChanged / _IsMatch 추가.
 * - 파생 훅 GetSearchKey(TData)를 virtual로 신설. 기본 구현은 string.Empty.
 * - SetActive(true)에서 ClearSearch를 먼저 호출하도록 분기에 중괄호 도입.
 *
 * # 이유
 * - IDropData가 빈 마커 인터페이스라 베이스는 항목의 검색 문자열을 꺼낼 수단이 없다.
 *   abstract로 추가하면 외부 파생 클래스가 전부 깨지므로 virtual + 빈 문자열 기본값을 택했다.
 * - 초기화를 Close()에 두지 않은 이유 : HDropDown.Close()는 base.Close()를 호출하지 않고
 *   완전히 덮어쓴다. 토글 경로가 반드시 지나는 유일한 공통 관문이 SetActive다.
 * - 재생성이 아니라 gameObject.SetActive 토글로 필터한 이유 : 선택 상태(ToggleGroup)가
 *   유지되고 GC 부담이 0이며 재배치는 LayoutGroup이 대신한다.
 *
 * # 결과
 * - searchInput이 비면 리스너를 달지 않아 검색 경로 자체가 꺼진다. 기존 프리팹은 무영향.
 * - 매칭은 IndexOf(query, OrdinalIgnoreCase) >= 0 부분일치. HData StringUtil.FilterText와 같은 관용구다.
 *
 * # 주의
 * - searchInput을 연결했는데 GetSearchKey를 override하지 않으면 검색어 입력 시 모든 항목이 숨는다.
 *   의도된 동작이다(빈 검색 키 = 검색 불가 항목). 조용히 통과시키지 않고 즉시 드러낸다.
 * - Open()을 SetActive 없이 직접 호출하면 이전 검색어가 남는다. 토글 경로는 항상 SetActive를 지난다.
 * - ApplyFilter는 Mathf.Min(datas.Count, units.Count)로 순회한다. CreateUnits가 units를
 *   Clear하지 않아 인스펙터 선입력 시 개수가 어긋날 수 있다.
 *
 * =============================================================================
 * @Jason - PKH
 * 네이티브 드롭다운 런타임에 신규 캔버스 레이아웃에 생성되기에 기존 레이아웃 시스템이 무시됩니다.
 * =================================================================================
 * @Jason - PKH 23. 07. 2025
 * KOR ::
 * 코드의 유연성과 유지보수를 고려하여 리펙토링 진행.
 * 파생 클래스는 드롭다운에 사용될 데이터와 유닛 생성을 의무적으로 하도록 유도하였습니다.
 * 필요시 조건에 맞는 외부 클래스를 사용이 가능하여 확장성을 보장하고
 * 데이터 저장용도의 너무 간소한 클래스/구조체를 물리적 코드파일 생성하는 것을 내부 클래스/구조체 생성으로 방지합니다.
 * ENG ::
 * Refactoring was performed considering the flexibility and maintainability of the code.
 * In the derived class, the data and unit to be used for the dropdown were made mandatory to be created.
 * When necessary, an external class that meets the conditions can be used to ensure extensibility,
 * The creation of a physical code file for a class/structure that is too simple for just to store some datas can be prevented by creating an inner class/structure.
 * =========================================================
 * @Jason - PKH 2026.03.10
 *
 * 주요 기능 ::
 * 1. 드롭다운 항목 데이터를 기반으로 유닛 오브젝트를 생성합니다.
 * 2. Toggle 상태에 따라 드롭다운 테이블의 활성/비활성을 제어합니다.
 * 3. DirectionType과 Offset 값을 사용하여 테이블 Pivot 및 위치를 조정합니다.
 * 4. 항목 선택 시 Value를 갱신하고 OnItemSelected 이벤트를 호출합니다.
 * 5. 파생 클래스가 InitUnits(), SelectByIndex(int)를 통해 세부 동작을 구현하도록 강제합니다.
 *
 * 사용법 ::
 * 1. BaseDropDown을 상속받는 파생 클래스를 작성합니다.
 * 2. TData는 드롭다운 데이터 구조, TUnit은 드롭다운 항목 UI 스크립트로 지정합니다.
 * 3. Inspector에서 dropTg, table, tableRect, unitParent, unitPrefab을 연결합니다.
 * 4. Start 시 CreateUnits()로 항목 오브젝트를 생성한 뒤 InitUnits()에서 데이터와 유닛을 매핑합니다.
 * 5. 항목 선택 시 OnSelect(index)를 호출하면 Value 변경과 함께 드롭다운이 닫힙니다.
 *
 * 기타 ::
 * 1. 네이티브 Dropdown이 기존 레이아웃 시스템을 무시하는 문제를 대체하기 위한 커스텀 구조입니다.
 * 2. unitPrefab에 TUnit이 없으면 런타임에 AddComponent로 추가합니다.
 * 3. scene 상의 실제 prefab 오브젝트일 경우 CreateUnits() 이후 원본 unitPrefab은 비활성화합니다.
 * =========================================================
 */
#endif
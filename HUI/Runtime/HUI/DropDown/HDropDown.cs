#if UNITY_EDITOR
/* =========================================================
 * 이 스크립트는 기본 커스텀 드롭다운 구현체입니다.
 * 문자열과 아이콘 정보를 가지는 드롭다운 데이터를 표시하며, 선택된 항목을
 * 상단 Label / Icon UI에 반영하고 Arrow 애니메이션과 함께 열림/닫힘 상태를 제어합니다.
 *
 * 주의사항 ::
 * 1. 이 스크립트는 BaseDropDown의 구체 구현체이며, table, tableTgg, unitPrefab, dropTg, label, icon, arrow가 정상 연결되어 있어야 합니다.
 * 2. HUnit은 Toggle 기반 선택 구조를 사용하므로 unitTg가 반드시 유효해야 합니다.
 * 3. 데이터 개수와 생성된 units 개수는 항상 일치해야 합니다.
 * 4. 초기 선택은 첫 번째 데이터(index 0)를 기준으로 동작하므로 datas가 비어 있지 않아야 정상 초기화됩니다.
 * 5. Arrow 회전 연출은 DOTween에 의존합니다.
 * 6. 검색을 쓰려면 인스펙터에서 searchInput을 연결합니다. 검색 키는 HData.Name입니다.
 * =========================================================
 */
#endif

using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using HInspector;

namespace HUI.Dropdown {
    public class HDropDown : BaseDropDown<HDropDown.HData, HDropDown.HUnit> {
        #region Inner Class
        [Serializable]
        public class HData : IDropData {
            public string Name;
            public Sprite Icon;
        }
        public class HUnit : BaseDropUnit {
            [HTitle("UI")]
            [SerializeField]
            TMP_Text text;
            [SerializeField]
            Image icon;


            public void Init(int uid, string name, Sprite icon, ToggleGroup group, Action<int> onSelected) {
                this.uid = uid;
                this.text.text = name;
                this.icon.sprite = icon;
                this.OnSelect += onSelected;

                unitTg.group = group;
                unitTg.onValueChanged.RemoveAllListeners();
                unitTg.onValueChanged.AddListener((isOn) => { if (isOn) RunSelectEvent(); });
            }
        }
        #endregion

        [HTitle("UI")]
        [SerializeField]
        Image icon;
        [SerializeField]
        TMP_Text label;
        [SerializeField]
        RectTransform arrow;


        public override void Open() {
            table.SetActive(true);
            arrow.DOKill();
            arrow.DOLocalRotate(new(180, 0), 0.2f);
        }
        public override void Close() {
            dropTg.isOn = false;
            table.SetActive(false);
            arrow.DOKill();
            arrow.DOLocalRotate(new(0, 0), 0.2f);
        }


        protected override void InitUnits() {
            for (int k = 0; k < datas.Count; k++) {
                var data = datas[k];
                var unit = units[k];
                var index = k;
                unit.Init(index, data.Name, data.Icon, tableTgg, OnSelect);
                unit.Toggle.isOn = k == 0 ? true : false;
            }

            SelectByIndex(0);
        }

        protected override void SelectByIndex(int index) {
            var data = datas[index];
            label.text = data.Name;
            icon.sprite = data.Icon;
        }

        protected override string GetSearchKey(HData data) => data.Name;
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * 2026-09-03 (수정) :: 검색 키 훅 override
 *
 * # 변경
 * - GetSearchKey(HData) 를 override 해 HData.Name 을 검색 키로 노출.
 *
 * # 이유
 * - 검색 파이프라인(입력 필드 / 필터 / 유닛 표시 토글)은 BaseDropDown 이 전부 소유한다.
 *   파생 클래스가 할 일은 "이 데이터의 검색 대상 문자열이 무엇인가" 한 줄로 답하는 것뿐이다.
 * - HData 는 Name 과 Icon 을 갖는다. Icon 은 검색 대상이 될 수 없으므로 Name 이 유일한 답이다.
 *
 * # 결과
 * - 인스펙터에서 searchInput 을 연결하면 그 즉시 이름 부분일치 검색이 동작한다.
 *   연결하지 않으면 베이스가 리스너를 달지 않아 기존 동작과 완전히 동일하다.
 *
 * # 주의
 * - Samples~/Dropdown/HDropdown.prefab 에는 searchInput 이 없다. 검색을 쓰려면
 *   프리팹에 TMP_InputField 를 추가하고 슬롯에 연결해야 한다.
 * =============================================================================
 */
#endif

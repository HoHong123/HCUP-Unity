using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

namespace Util.UI.Drop {
    [RequireComponent(typeof(Toggle))]
    [RequireComponent(typeof(RectTransform))]
    public abstract class BaseDropDown<TData, TUnit> : MonoBehaviour, IBasicPanel
        where TData : IDropData, new()
        where TUnit : MonoBehaviour, IDropUnit {
        [Title("Data")]
        [SerializeField]
        protected List<TData> datas = new();

        [Title("Setting")]
        [SerializeField, OnValueChanged("SetTablePivot")]
        [Tooltip("Preset position setting. (Not mandatory)")]
        protected DirectionType direction = DirectionType.Down;

        [Title("DropDown")]
        [SerializeField]
        protected Toggle dropTg;
        [SerializeField]
        protected RectTransform rect;

        [Title("Table")]
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

        [Title("Unit")]
        [SerializeField]
        protected GameObject unitPrefab;
        [SerializeField]
        protected List<TUnit> units = new();

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
            SetActive(false);

            if (datas.Count == 0) return;

            CreateUnits();
            InitUnits();
        }


        public virtual void Open() => table.SetActive(true);
        public virtual void Close() => table.SetActive(false);
        public virtual void SetActive(bool isOn) {
            if (isOn)
                Open();
            else
                Close();
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
        /// After create unit game object by calling 'CreateUnits' function.
        /// Init all units using 'TData' in 'InitUnits' function.
        /// </summary>
        protected abstract void InitUnits();
        /// <summary>
        /// What would happen after selecting 
        /// </summary>
        /// <param name="index"></param>
        protected abstract void SelectByIndex(int index);
    }
}


#if UNITY_EDITOR
/* Dev Log
 * @Jason - PKH
 * 네이티브 드롭다운 로직이 기존 레이아웃 시스템을 무시하도록 설계되어있어 커스텀 클래스를 만듭니다.
 * I create a custom 'DropDown' class to replace the native dropdown that designed to override the existing layout system.
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
 */
#endif
using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;

namespace Util.UI.Drop {
    public class HDropDown : BaseDropDown<HDropDown.HData, HDropDown.HUnit> {
        #region Inner Class
        public class HData : IDropData {
            public string Name;
            public Sprite Icon;
        }
        public class HUnit : BaseDropUnit {
            [Title("UI")]
            [SerializeField]
            TMP_Text text;
            [SerializeField]
            Image icon;


            public void Init(int uid, string name, Sprite icon, ToggleGroup group, Action<int> onSelected) {
                this.uid = uid;
                this.text.text = name;
                this.icon.sprite = icon;
                this.OnSelect = onSelected;

                unitTg.group = group;
                unitTg.onValueChanged.RemoveAllListeners();
                unitTg.onValueChanged.AddListener((isOn) => { if (isOn) OnSelect?.Invoke(uid); });
            }
        }
        #endregion

        [Title("UI")]
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
    }
}
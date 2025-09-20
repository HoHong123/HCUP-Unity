#if ODIN_INSPECTOR
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Util.UI.Panel;
using Util.OdinCompat;
using UnityEngine.UI;
#endif

namespace Util.UI.Drop {
    public abstract partial class BaseDropDown<TData, TUnit> : MonoBehaviour, IBasicPanel
        where TData : IDropData, new()
        where TUnit : MonoBehaviour, IDropUnit {
#if ODIN_INSPECTOR
        [HeaderOrTitle("Data")]
        [ListDrawerSettings]
        [SerializeField]
        protected List<TData> datas = new();

        [HeaderOrTitle("Setting")]
        [OnValueChanged("SetTablePivot")]
        [SerializeField]
        [Tooltip("Preset position setting. (Not mandatory)")]
        protected DirectionType direction = DirectionType.Down;

        [HeaderOrTitle("Data")]
        [SerializeField]
        protected Toggle dropTg;
        [SerializeField]
        protected RectTransform rect;

        [HeaderOrTitle("Data")]
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

        [HeaderOrTitle("Data")]
        [SerializeField]
        protected GameObject unitPrefab;
        [SerializeField]
        protected List<TUnit> units = new();
#endif
    }
}


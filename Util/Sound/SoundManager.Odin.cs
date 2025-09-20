#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
using System.Collections.Generic;
#endif
using UnityEngine;
using Util.Core;
using Util.OdinCompat;

namespace Util.Sound {
    public partial class SoundManager : SingletonBehaviour<SoundManager> {
#if ODIN_INSPECTOR
        [HeaderOrTitle("Sound Data Allocation")]
        [DictionaryDrawerSettings(KeyLabel = "Audio Code", ValueLabel = "Audio Clip")]
        [SerializeField]
        Dictionary<int, SoundItem> soundDic = new();
#endif
    }
}

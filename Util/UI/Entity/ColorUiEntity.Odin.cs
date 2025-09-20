using System;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using Util.OdinCompat;

namespace Util.UI.Entity {
    [Serializable]
    public partial class ColorUiEntity {
#if ODIN_INSPECTOR
        [HeaderOrTitle("Option")]
        [SerializeField]
        bool changeSprite = false;
        [HideIf(nameof(changeSprite)), SerializeField]
        bool useAnimation = false;
        [ShowIf("@!this.changeSprite && this.useAnimation"), SerializeField]
        float animationDuration = 0.2f;

        [HeaderOrTitle("Color")]
        [OnValueChanged(nameof(_Init))]
        [HideIf(nameof(changeSprite))]
        [SerializeField]
        MaskableGraphic graphic;
        [HideIf(nameof(changeSprite))]
        [SerializeField]
        Color originColor;
        [HideIf(nameof(changeSprite))]
        [SerializeField]
        Color targetColor;

        [HeaderOrTitle("Sprite")]
        [OnValueChanged(nameof(_Init))]
        [ShowIf(nameof(changeSprite))]
        [SerializeField]
        Image image;
        [ShowIf(nameof(changeSprite)), SerializeField]
        Sprite originSprite;
        [ShowIf(nameof(changeSprite)), SerializeField]
        Sprite targetSprite;
#endif
    }
}
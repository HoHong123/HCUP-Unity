using UnityEngine;
using Sirenix.OdinInspector;
using HGame.Character;

namespace HGame.Map {
    [DisallowMultipleComponent]
    public class MinimapTrackable : MonoBehaviour {
        [Title("Config")]
        [SerializeField, ReadOnly]
        BaseCharacterConfig config;

        [Title("Collider")]
        [SerializeField]
        Transform target;

        [Title("Collider")]
        [SerializeField]
        Collider2D charCollider;

        [Title("Icon")]
        [SerializeField]
        bool useIcon = false;
        [SerializeField]
        bool scaleByCollider = false;
        [SerializeField]
        float iconSizeMin = 6f;
        [SerializeField]
        float iconSizeMax = 18f;

        [Title("Visibility")]
        [SerializeField]
        bool showWhenOutOfBounds = false;

        public bool UseIcon => useIcon;
        public bool ScaleByCollider => scaleByCollider;
        public bool ShowWhenOutOfBounds => showWhenOutOfBounds;
        public float IconSizeMin => iconSizeMin;
        public float IconSizeMax => iconSizeMax;
        public Sprite Icon => config.Icon;
        public Transform Target => target;
        public Collider2D Collider => charCollider;

        public void Init(BaseCharacterConfig config) {
            this.config = config;
            if (!target) target = transform;
        }
    }
}

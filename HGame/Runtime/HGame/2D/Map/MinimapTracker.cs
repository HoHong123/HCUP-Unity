using UnityEngine;
using HGame.Character;
using HInspector;

namespace HGame.Map {
    [DisallowMultipleComponent]
    public class MinimapTrackable : MonoBehaviour {
        [HTitle("Config")]
        [SerializeField, HReadOnly]
        BaseCharacterConfig config;

        [HTitle("Collider")]
        [SerializeField]
        Transform target;

        [HTitle("Collider")]
        [SerializeField]
        Collider2D charCollider;

        [HTitle("Icon")]
        [SerializeField]
        bool useIcon = false;
        [SerializeField]
        bool scaleByCollider = false;
        [SerializeField]
        float iconSizeMin = 6f;
        [SerializeField]
        float iconSizeMax = 18f;

        [HTitle("Visibility")]
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

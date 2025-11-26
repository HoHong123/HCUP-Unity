using UnityEngine;
using Sirenix.OdinInspector;

namespace HGame.Skill {
    public abstract class BaseSkillSO : SerializedScriptableObject {
        [Title("Meta")]
        [SerializeField]
        int uid;

        [Title("Display")]
        [SerializeField]
        string displayName;
        [SerializeField]
        Sprite icon;
        [SerializeField]
        Sprite uiIcon;
        [SerializeField, TextArea]
        string description;

        [Title("Stack")]
        [SerializeField]
        int maxStacks = 10;
        [SerializeField]
        RarityStackGrant grant = new();


        public int UID => uid;
        public string DisplayName => displayName;
        public int MaxStacks => Mathf.Max(1, maxStacks);
        public string Description => description;
        public Sprite Icon => icon;
        public Sprite UiIcon => uiIcon;

        public virtual bool CanOffer(SkillStats stats, int currentStacks) => currentStacks < MaxStacks;
        public abstract void ApplyWithRarity(SkillStats stats, SkillRarity rarity, ref int currentStacks);
        public virtual bool TryGetFixedRarity(out SkillRarity rarity) {
            rarity = default;
            return false;
        }

        protected int GrantFor(SkillRarity rarity) => grant.Get(rarity);
        protected bool TryAddStacks(ref int current, int add, int max) {
            if (current >= max) return false;
            int before = current;
            current = Mathf.Min(max, current + Mathf.Max(0, add));
            return current > before;
        }
    }
}

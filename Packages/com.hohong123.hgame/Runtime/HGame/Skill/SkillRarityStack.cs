using UnityEngine;

namespace HGame.Skill {
    [System.Serializable]
    public struct RarityStackGrant {
        [SerializeField, Min(0)]
        int normal;
        [SerializeField, Min(0)]
        int common;
        [SerializeField, Min(0)]
        int rare;
        [SerializeField, Min(0)]
        int epic;

        public int Get(SkillRarity rarity) => rarity switch {
            SkillRarity.Normal => normal,
            SkillRarity.Common => common,
            SkillRarity.Rare => rare,
            _ => epic
        };
    }
}

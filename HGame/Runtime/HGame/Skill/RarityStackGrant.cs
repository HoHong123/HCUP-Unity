using UnityEngine;
using HInspector;

namespace HGame.Skill {
    [System.Serializable]
    public struct RarityStackGrant {
        [HMin(0)]
        [SerializeField]
        int normal;
        [HMin(0)]
        [SerializeField]
        int common;
        [HMin(0)]
        [SerializeField]
        int rare;
        [HMin(0)]
        [SerializeField]
        int epic;

        public int Get(SkillRarity rarity) => rarity switch {
            SkillRarity.Normal => normal,
            SkillRarity.Common => common,
            SkillRarity.Rare => rare,
            _ => epic
        };
    }
}

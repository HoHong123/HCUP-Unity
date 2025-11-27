using UnityEngine;

namespace HGame.Skill.Sample {
    [CreateAssetMenu(fileName = "ExplosiveUnlock", menuName = "Game/Skill/Arrow/Explosive Unlock", order = 0)]
    public class SkillExplosiveUnlockSO : BaseSkillSO {
        [SerializeField]
        SkillRarity fixedRarity = SkillRarity.Rare;

        public override bool CanOffer(SkillStats stats, int stacks) => !stats.EnableExplosive;

        public override bool TryGetFixedRarity(out SkillRarity rarity) {
            rarity = fixedRarity;
            return true;
        }

        public override void ApplyWithRarity(SkillStats stats, SkillRarity rarity, ref int current) {
            if (!stats.EnableExplosive) {
                stats.UnlockExplosive();
                current = 1;
            }
        }
    }
}